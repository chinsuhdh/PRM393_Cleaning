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
        private readonly IEmailService _emailService; // [THÊM MỚI] Khai báo EmailService

        // [THÊM MỚI] Inject IEmailService vào constructor
        public AuthService(AppDbContext context, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        // ==========================================
        // 1. ĐĂNG KÝ (REGISTER) - KÈM TẠO OTP XÁC THỰC
        // ==========================================
        public async Task<bool> RegisterAsync(RegisterRequestDto request)
        {
            // Kiểm tra tồn tại
            var exists = await _context.Accounts.AnyAsync(a => a.Email == request.Email || a.PhoneNumber == request.PhoneNumber);
            if (exists) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Băm mật khẩu bằng BCrypt
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                var newAccount = new Account
                {
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    PasswordHash = passwordHash,
                    Role = UserRole.Client,
                    Status = AccountStatus.PendingVerification, // Cài trạng thái chờ xác thực
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Accounts.Add(newAccount);
                await _context.SaveChangesAsync();

                var newProfile = new Profile
                {
                    Id = newAccount.Id,
                    FullName = request.FullName,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Profiles.Add(newProfile);
                await _context.SaveChangesAsync();

                // --- LOGIC TẠO OTP XÁC THỰC TÀI KHOẢN ---
                var otpCode = new Random().Next(100000, 999999).ToString();
                _context.OtpVerifications.Add(new OtpVerification
                {
                    AccountId = newAccount.Id,
                    OtpCode = otpCode,
                    Purpose = "verify_account",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                // [THÊM MỚI] Gọi IEmailService để gửi mail thật thay vì Console.WriteLine
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

        // ==========================================
        // 2. XÁC THỰC TÀI KHOẢN (VERIFY ACCOUNT)
        // ==========================================
        public async Task<bool> VerifyAccountAsync(VerifyAccountDto request)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == request.Email);
            if (account == null) return false;

            if (account.Status == AccountStatus.Active) return true;

            var otpRecord = await _context.OtpVerifications
                .Where(o => o.AccountId == account.Id && o.Purpose == "verify_account" && o.OtpCode == request.OtpCode && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null || otpRecord.ExpiresAt < DateTime.UtcNow) return false;

            otpRecord.IsUsed = true;
            account.Status = AccountStatus.Active;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // ==========================================
        // 3. ĐĂNG NHẬP (LOGIN)
        // ==========================================
        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request, string? ipAddress, string? userAgent)
        {
            var account = await _context.Accounts
                .Include(a => a.Profile)
                .FirstOrDefaultAsync(a => a.Email == request.EmailOrPhone || a.PhoneNumber == request.EmailOrPhone);

            if (account == null || !BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
            {
                if (account != null)
                {
                    _context.LoginHistories.Add(new LoginHistory
                    {
                        AccountId = account.Id,
                        IpAddress = ipAddress,
                        UserAgent = userAgent,
                        IsSuccess = false,
                        FailReason = "Wrong password",
                        LoginTime = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }
                return null;
            }

            if (account.Status != AccountStatus.Active)
            {
                throw new Exception("Tài khoản chưa được xác thực hoặc đã bị khóa.");
            }

            var accessToken = GenerateJwtToken(account);
            var refreshToken = GenerateRefreshToken();

            _context.RefreshTokens.Add(new RefreshToken
            {
                AccountId = account.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(Convert.ToDouble(_configuration["JwtConfig:RefreshTokenExpirationDays"])),
                CreatedByIp = ipAddress,
                CreatedAt = DateTime.UtcNow
            });

            _context.LoginHistories.Add(new LoginHistory
            {
                AccountId = account.Id,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccess = true,
                LoginTime = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Role = account.Role.ToString()!,
                ProfileId = account.Id,
                FullName = account.Profile?.FullName ?? "Unknown"
            };
        }

        // ==========================================
        // 4. LÀM MỚI TOKEN (REFRESH TOKEN)
        // ==========================================
        public async Task<AuthResponseDto?> RefreshTokenAsync(string oldRefreshToken, string? ipAddress)
        {
            var tokenRecord = await _context.RefreshTokens
                .Include(t => t.Account)
                .ThenInclude(a => a.Profile)
                .FirstOrDefaultAsync(t => t.Token == oldRefreshToken);

            if (tokenRecord == null || tokenRecord.IsRevoked || tokenRecord.ExpiresAt < DateTime.UtcNow)
                return null;

            tokenRecord.IsRevoked = true;
            tokenRecord.RevokedAt = DateTime.UtcNow;
            tokenRecord.RevokedByIp = ipAddress;

            var account = tokenRecord.Account;
            var newAccessToken = GenerateJwtToken(account);
            var newRefreshTokenString = GenerateRefreshToken();

            tokenRecord.ReplacedByToken = newRefreshTokenString;

            _context.RefreshTokens.Add(new RefreshToken
            {
                AccountId = account.Id,
                Token = newRefreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(Convert.ToDouble(_configuration["JwtConfig:RefreshTokenExpirationDays"])),
                CreatedByIp = ipAddress,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshTokenString,
                Role = account.Role.ToString()!,
                ProfileId = account.Id,
                FullName = account.Profile?.FullName ?? "Unknown"
            };
        }

        // ==========================================
        // 5. ĐĂNG XUẤT (LOGOUT)
        // ==========================================
        public async Task<bool> LogoutAsync(string refreshToken, string? ipAddress)
        {
            var tokenRecord = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
            if (tokenRecord == null) return false;

            tokenRecord.IsRevoked = true;
            tokenRecord.RevokedAt = DateTime.UtcNow;
            tokenRecord.RevokedByIp = ipAddress;

            await _context.SaveChangesAsync();
            return true;
        }

        // ==========================================
        // 6. QUÊN MẬT KHẨU (FORGOT PASSWORD)
        // ==========================================
        public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == request.Email);
            if (account == null) return false;

            var otpCode = new Random().Next(100000, 999999).ToString();

            _context.OtpVerifications.Add(new OtpVerification
            {
                AccountId = account.Id,
                OtpCode = otpCode,
                Purpose = "reset_password",
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // [THÊM MỚI] Gọi IEmailService
            string emailBody = $@"
                <h2>Yêu cầu đặt lại mật khẩu</h2>
                <p>Mã OTP để đặt lại mật khẩu của bạn là: <strong style='font-size: 24px;'>{otpCode}</strong></p>
                <p>Mã này sẽ hết hạn sau 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>";

            await _emailService.SendEmailAsync(request.Email, "Mã OTP khôi phục mật khẩu - CleanAI", emailBody);

            return true;
        }

        // ==========================================
        // 7. ĐẶT LẠI MẬT KHẨU (RESET PASSWORD)
        // ==========================================
        public async Task<bool> ResetPasswordAsync(ResetPasswordDto request)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == request.Email);
            if (account == null) return false;

            var otpRecord = await _context.OtpVerifications
                .Where(o => o.AccountId == account.Id && o.Purpose == "reset_password" && o.OtpCode == request.OtpCode && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null || otpRecord.ExpiresAt < DateTime.UtcNow) return false;

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            account.UpdatedAt = DateTime.UtcNow;
            otpRecord.IsUsed = true;

            await _context.SaveChangesAsync();
            return true;
        }

        // ==========================================
        // PRIVATE HELPERS
        // ==========================================
        private string GenerateJwtToken(Account account)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JwtConfig:Secret"]!);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, account.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, account.Role.ToString()!)
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

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}