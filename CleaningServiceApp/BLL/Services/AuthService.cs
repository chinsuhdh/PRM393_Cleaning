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
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Cleaning.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AppDbContext context,
            IConfiguration configuration,
            IEmailService emailService,
            ISmsService smsService,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _smsService = smsService;
            _logger = logger;
        }

        public async Task<bool> RegisterAsync(RegisterRequestDto request)
        {
            var exists = await _context.Accounts.AnyAsync(a => a.Email == request.Email || a.PhoneNumber == request.PhoneNumber);
            if (exists) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                if (!Enum.TryParse<UserRole>(request.Role, true, out var parsedRole))
                {
                    parsedRole = UserRole.Client;
                }

                var newAccount = new Account
                {
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    PasswordHash = passwordHash,
                    PasswordSalt = "BCRYPT_EMBEDDED",
                    Role = parsedRole,
                    Status = AccountStatus.PendingVerification,
                    IsEmailVerified = false,
                    IsPhoneVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Accounts.Add(newAccount);
                await _context.SaveChangesAsync();

                var newProfile = new Profile
                {
                    Id = newAccount.Id,
                    FullName = request.FullName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Profiles.Add(newProfile);

                if (parsedRole == UserRole.Worker)
                {
                    var workerProfile = new WorkerProfile
                    {
                        UserId = newAccount.Id,
                        AverageRating = 5.00m,
                        OnlineStatus = WorkerOnlineStatus.Offline,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.WorkerProfiles.Add(workerProfile);
                }

                var otpCode = GenerateSecureOtp();
                _context.VerificationCodes.Add(new VerificationCode
                {
                    AccountId = newAccount.Id,
                    CodeHash = otpCode,
                    Purpose = VerificationPurpose.EmailVerification,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                string emailBody = $@"
                    <h2>Xác thực tài khoản CleanAI</h2>
                    <p>Chào {request.FullName},</p>
                    <p>Mã OTP xác thực tài khoản của bạn là: <strong style='font-size: 24px;'>{otpCode}</strong></p>
                    <p>Mã này sẽ hết hạn trong 15 phút.</p>";

                await _emailService.SendEmailAsync(request.Email, "Mã xác thực tài khoản - CleanAI", emailBody);

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình RegisterAsync cho email {Email}", request.Email);
                return false;
            }
        }

        public async Task<bool> VerifyAccountAsync(VerifyAccountDto request)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == request.Email);
            if (account == null) return false;

            if (account.Status == AccountStatus.Active) return true;

            var otpRecord = await _context.VerificationCodes
                .Where(o => o.AccountId == account.Id && o.Purpose == VerificationPurpose.EmailVerification && o.CodeHash == request.OtpCode && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null || otpRecord.ExpiresAt < DateTime.UtcNow) return false;

            otpRecord.IsUsed = true;
            account.Status = AccountStatus.Active;
            account.IsEmailVerified = true;
            account.UpdatedAt = DateTime.UtcNow;

            var oldOtps = await _context.VerificationCodes.Where(o => o.AccountId == account.Id && !o.IsUsed).ToListAsync();
            foreach (var otp in oldOtps) otp.IsUsed = true;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request, string? ipAddress, string? userAgent)
        {
            var account = await _context.Accounts
                .Include(a => a.Profile)
                .FirstOrDefaultAsync(a => a.Email == request.EmailOrPhone || a.PhoneNumber == request.EmailOrPhone);

            if (account == null || !BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
            {
                return null;
            }

            if (account.Status != AccountStatus.Active)
            {
                throw new Exception("Tài khoản chưa được xác thực hoặc đã bị khóa.");
            }

            var accessToken = GenerateJwtToken(account);
            var rawRefreshToken = GenerateRefreshToken();
            string tokenHash = ComputeSha256Hash(rawRefreshToken);

            _context.RefreshTokens.Add(new RefreshToken
            {
                AccountId = account.Id,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(Convert.ToDouble(_configuration["JwtConfig:RefreshTokenExpirationDays"])),
                CreatedByIp = ipAddress,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = rawRefreshToken,
                Role = account.Role.ToString(),
                ProfileId = account.Id,
                FullName = account.Profile?.FullName ?? "Unknown"
            };
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(string oldRefreshToken, string? ipAddress)
        {
            string oldTokenHash = ComputeSha256Hash(oldRefreshToken);

            var tokenRecord = await _context.RefreshTokens
                .Include(t => t.Account)
                .ThenInclude(a => a.Profile)
                .FirstOrDefaultAsync(t => t.TokenHash == oldTokenHash);

            if (tokenRecord == null || tokenRecord.IsRevoked || tokenRecord.ExpiresAt < DateTime.UtcNow)
                return null;

            tokenRecord.IsRevoked = true;
            tokenRecord.RevokedAt = DateTime.UtcNow;
            tokenRecord.RevokedByIp = ipAddress;

            var account = tokenRecord.Account;
            var newAccessToken = GenerateJwtToken(account);
            var newRawRefreshToken = GenerateRefreshToken();
            string newTokenHash = ComputeSha256Hash(newRawRefreshToken);

            tokenRecord.ReplacedByTokenHash = newTokenHash;

            _context.RefreshTokens.Add(new RefreshToken
            {
                AccountId = account.Id,
                TokenHash = newTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(Convert.ToDouble(_configuration["JwtConfig:RefreshTokenExpirationDays"])),
                CreatedByIp = ipAddress,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRawRefreshToken,
                Role = account.Role.ToString(),
                ProfileId = account.Id,
                FullName = account.Profile?.FullName ?? "Unknown"
            };
        }

        public async Task<bool> LogoutAsync(string refreshToken, string? ipAddress)
        {
            string tokenHash = ComputeSha256Hash(refreshToken);

            var tokenRecord = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
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

            var otpCode = GenerateSecureOtp();

            _context.VerificationCodes.Add(new VerificationCode
            {
                AccountId = account.Id,
                CodeHash = otpCode,
                Purpose = VerificationPurpose.PasswordReset,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            string emailBody = $@"
                <h2>Yêu cầu đặt lại mật khẩu</h2>
                <p>Mã OTP để đặt lại mật khẩu của bạn là: <strong style='font-size: 24px;'>{otpCode}</strong></p>
                <p>Mã này sẽ hết hạn trong 5 phút.</p>";

            await _emailService.SendEmailAsync(request.Email, "Mã OTP khôi phục mật khẩu - CleanAI", emailBody);

            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto request)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == request.Email);
            if (account == null) return false;

            var otpRecord = await _context.VerificationCodes
                .Where(o => o.AccountId == account.Id && o.Purpose == VerificationPurpose.PasswordReset && o.CodeHash == request.OtpCode && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null || otpRecord.ExpiresAt < DateTime.UtcNow) return false;

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            account.PasswordSalt = "BCRYPT_EMBEDDED";
            account.UpdatedAt = DateTime.UtcNow;

            otpRecord.IsUsed = true;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto request)
        {
            var account = await _context.Accounts.FindAsync(userId);
            if (account == null) return false;

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, account.PasswordHash))
                return false;

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SendPhoneVerificationOtpAsync(Guid userId)
        {
            var account = await _context.Accounts.FindAsync(userId);
            if (account == null || string.IsNullOrEmpty(account.PhoneNumber)) return false;

            var otpCode = GenerateSecureOtp();
            _context.VerificationCodes.Add(new VerificationCode
            {
                AccountId = account.Id,
                CodeHash = otpCode,
                Purpose = VerificationPurpose.PhoneVerification,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await _smsService.SendSmsAsync(account.PhoneNumber, $"[CleanAI] Ma OTP xac thuc so dien thoai cua ban la {otpCode}. Co hieu luc trong 5 phut.");
            return true;
        }

        public async Task<bool> VerifyPhoneAsync(VerifyPhoneDto request)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.PhoneNumber == request.PhoneNumber);
            if (account == null) return false;

            var otpRecord = await _context.VerificationCodes
                .Where(o => o.AccountId == account.Id && o.Purpose == VerificationPurpose.PhoneVerification && o.CodeHash == request.OtpCode && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null || otpRecord.ExpiresAt < DateTime.UtcNow) return false;

            otpRecord.IsUsed = true;
            account.IsPhoneVerified = true;
            account.UpdatedAt = DateTime.UtcNow;

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

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private static string ComputeSha256Hash(string input)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
        }

        private static string GenerateSecureOtp()
        {
            return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        }

        public async Task<string?> ReauthenticateAsync(Guid userId, string password)
        {
            var account = await _context.Accounts.FindAsync(userId);
            if (account == null || !BCrypt.Net.BCrypt.Verify(password, account.PasswordHash))
            {
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JwtConfig:Secret"]!);

            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
        new Claim("TokenType", "Reauth") 
    };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(5),
                Issuer = _configuration["JwtConfig:Issuer"],
                Audience = _configuration["JwtConfig:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}