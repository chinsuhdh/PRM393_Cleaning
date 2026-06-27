using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Data;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Cleaning.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthService(AppDbContext context, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<bool> RegisterAsync(RegisterRequestDto request)
        {
            var exists = await _context.Accounts.AnyAsync(a => a.Email == request.Email || a.PhoneNumber == request.PhoneNumber);
            if (exists) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var passwordSalt = GenerateToken(32);
                var newAccount = new Account
                {
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password + passwordSalt),
                    PasswordSalt = passwordSalt,
                    Role = UserRole.Client,
                    Status = AccountStatus.PendingVerification,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Accounts.Add(newAccount);
                await _context.SaveChangesAsync();

                _context.Profiles.Add(new Profile
                {
                    Id = newAccount.Id,
                    FullName = request.FullName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
                _context.VerificationCodes.Add(new VerificationCode
                {
                    AccountId = newAccount.Id,
                    CodeHash = BCrypt.Net.BCrypt.HashPassword(otpCode),
                    Purpose = VerificationPurpose.EmailVerification,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                string emailBody = $@"
                    <h2>Xác thực tài khoản CleanAI</h2>
                    <p>Chào {request.FullName},</p>
                    <p>Mã OTP xác thực tài khoản của bạn là: <strong style='font-size: 24px;'>{otpCode}</strong></p>
                    <p>Mã này sẽ hết hạn sau 15 phút.</p>";

                await _emailService.SendEmailAsync(request.Email, "Mã xác thực tài khoản - CleanAI", emailBody);

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> VerifyAccountAsync(VerifyAccountDto request)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == request.Email);
            if (account == null) return false;
            if (account.Status == AccountStatus.Active) return true;

            var verificationRecords = await _context.VerificationCodes
                .Where(o => o.AccountId == account.Id && o.Purpose == VerificationPurpose.EmailVerification && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var verificationRecord = verificationRecords
                .FirstOrDefault(o => BCrypt.Net.BCrypt.Verify(request.OtpCode, o.CodeHash));

            if (verificationRecord == null || verificationRecord.ExpiresAt < DateTime.UtcNow) return false;

            verificationRecord.IsUsed = true;
            account.Status = AccountStatus.Active;
            account.IsEmailVerified = true;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request, string? ipAddress, string? userAgent)
        {
            var account = await _context.Accounts
                .Include(a => a.Profile)
                .FirstOrDefaultAsync(a => a.Email == request.EmailOrPhone || a.PhoneNumber == request.EmailOrPhone);

            if (account == null || account.PasswordHash == null || account.PasswordSalt == null ||
                !BCrypt.Net.BCrypt.Verify(request.Password + account.PasswordSalt, account.PasswordHash))
            {
                return null;
            }

            if (account.Status != AccountStatus.Active)
            {
                throw new Exception("Tài khoản chưa được xác thực hoặc đã bị khóa.");
            }

            var accessToken = GenerateJwtToken(account);
            var refreshToken = GenerateToken(64);

            _context.RefreshTokens.Add(new RefreshToken
            {
                AccountId = account.Id,
                TokenHash = HashToken(refreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(Convert.ToDouble(_configuration["JwtConfig:RefreshTokenExpirationDays"])),
                CreatedByIp = ipAddress,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return ToAuthResponse(account, accessToken, refreshToken);
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(string oldRefreshToken, string? ipAddress)
        {
            var oldRefreshTokenHash = HashToken(oldRefreshToken);
            var tokenRecord = await _context.RefreshTokens
                .Include(t => t.Account)
                .ThenInclude(a => a.Profile)
                .FirstOrDefaultAsync(t => t.TokenHash == oldRefreshTokenHash);

            if (tokenRecord == null || tokenRecord.IsRevoked || tokenRecord.ExpiresAt < DateTime.UtcNow)
                return null;

            tokenRecord.IsRevoked = true;
            tokenRecord.RevokedAt = DateTime.UtcNow;
            tokenRecord.RevokedByIp = ipAddress;

            var account = tokenRecord.Account;
            var newAccessToken = GenerateJwtToken(account);
            var newRefreshToken = GenerateToken(64);
            var newRefreshTokenHash = HashToken(newRefreshToken);

            tokenRecord.ReplacedByTokenHash = newRefreshTokenHash;

            _context.RefreshTokens.Add(new RefreshToken
            {
                AccountId = account.Id,
                TokenHash = newRefreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(Convert.ToDouble(_configuration["JwtConfig:RefreshTokenExpirationDays"])),
                CreatedByIp = ipAddress,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return ToAuthResponse(account, newAccessToken, newRefreshToken);
        }

        public async Task<bool> LogoutAsync(string refreshToken, string? ipAddress)
        {
            var refreshTokenHash = HashToken(refreshToken);
            var tokenRecord = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == refreshTokenHash);
            if (tokenRecord == null) return false;

            tokenRecord.IsRevoked = true;
            tokenRecord.RevokedAt = DateTime.UtcNow;
            tokenRecord.RevokedByIp = ipAddress;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == request.Email);
            if (account == null) return false;

            var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            _context.VerificationCodes.Add(new VerificationCode
            {
                AccountId = account.Id,
                CodeHash = BCrypt.Net.BCrypt.HashPassword(otpCode),
                Purpose = VerificationPurpose.PasswordReset,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            string emailBody = $@"
                <h2>Yêu cầu đặt lại mật khẩu</h2>
                <p>Mã OTP để đặt lại mật khẩu của bạn là: <strong style='font-size: 24px;'>{otpCode}</strong></p>
                <p>Mã này sẽ hết hạn sau 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>";

            await _emailService.SendEmailAsync(request.Email, "Mã OTP khôi phục mật khẩu - CleanAI", emailBody);

            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto request)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == request.Email);
            if (account == null) return false;

            var verificationRecords = await _context.VerificationCodes
                .Where(o => o.AccountId == account.Id && o.Purpose == VerificationPurpose.PasswordReset && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var verificationRecord = verificationRecords
                .FirstOrDefault(o => BCrypt.Net.BCrypt.Verify(request.OtpCode, o.CodeHash));

            if (verificationRecord == null || verificationRecord.ExpiresAt < DateTime.UtcNow) return false;

            var passwordSalt = GenerateToken(32);
            account.PasswordSalt = passwordSalt;
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword + passwordSalt);
            account.UpdatedAt = DateTime.UtcNow;
            verificationRecord.IsUsed = true;

            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerateJwtToken(Account account)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JwtConfig:Secret"]!);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, account.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, account.Role.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JwtConfig:AccessTokenExpirationMinutes"])),
                Issuer = _configuration["JwtConfig:Issuer"],
                Audience = _configuration["JwtConfig:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static AuthResponseDto ToAuthResponse(Account account, string accessToken, string refreshToken)
        {
            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Role = account.Role.ToString(),
                ProfileId = account.Id,
                FullName = account.Profile?.FullName ?? "Unknown"
            };
        }

        private static string GenerateToken(int byteCount)
        {
            var randomNumber = new byte[byteCount];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}
