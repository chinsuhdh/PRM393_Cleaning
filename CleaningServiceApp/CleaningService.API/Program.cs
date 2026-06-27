using System.Text;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.BLL.Services;
using Cleaning.DAL.Data;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;
using Cleaning.DAL.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;

namespace CleaningService.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ==========================================
            // 1. CẤU HÌNH DATABASE & ENUMS POSTGRESQL
            // ==========================================
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

            // Map TẤT CẢ các Enum để đồng bộ với PostgreSQL Schema
            dataSourceBuilder.MapEnum<UserRole>("user_role");
            dataSourceBuilder.MapEnum<AccountStatus>("account_status");
            dataSourceBuilder.MapEnum<VerificationPurpose>("verification_purpose");
            dataSourceBuilder.MapEnum<PropertyType>("property_type");
            dataSourceBuilder.MapEnum<ServiceUnitType>("service_unit_type");
            dataSourceBuilder.MapEnum<BookingType>("booking_type");
            dataSourceBuilder.MapEnum<BookingStatus>("booking_status");
            dataSourceBuilder.MapEnum<WorkerOnlineStatus>("worker_online_status");
            dataSourceBuilder.MapEnum<AvailabilityStatus>("availability_status");
            dataSourceBuilder.MapEnum<PaymentMethod>("payment_method");
            dataSourceBuilder.MapEnum<PaymentStatus>("payment_status");
            dataSourceBuilder.MapEnum<PayoutStatus>("payout_status");
            dataSourceBuilder.MapEnum<RescheduleStatus>("reschedule_status");
            dataSourceBuilder.MapEnum<AiSenderType>("ai_sender_type");
            dataSourceBuilder.MapEnum<PhotoType>("photo_type");
            dataSourceBuilder.MapEnum<CleanlinessLevel>("cleanliness_level");
            dataSourceBuilder.MapEnum<NotificationType>("notification_type");

            var dataSource = dataSourceBuilder.Build();

            // Đăng ký AppDbContext với DI
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(dataSource));

            // ==========================================
            // 2. CẤU HÌNH AUTHENTICATION & JWT
            // ==========================================
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["JwtConfig:Issuer"],
                        ValidAudience = builder.Configuration["JwtConfig:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["JwtConfig:Secret"]!))
                    };
                });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // ==========================================
            // 3. CẤU HÌNH SWAGGER (CÓ BEARER TOKEN)
            // ==========================================
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Cleaning Service API", Version = "v1" });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Vui lòng nhập 'Bearer [khoảng_trắng] [chuỗi_token_của_bạn]'. Ví dụ: Bearer eyJhbGciOi...",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // ==========================================
            // 4. BINDING CẤU HÌNH TỪ APPSETTINGS
            // ==========================================
            builder.Services.Configure<EmailConfiguration>(builder.Configuration.GetSection("EmailConfiguration"));

            // ==========================================
            // 5. ĐĂNG KÝ DEPENDENCY INJECTION (DI)
            // ==========================================

            // Core & Repositories
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Business Services
            builder.Services.AddScoped<IEmailService, EmailService>();

            // [ĐÃ THÊM] Đăng ký ISmsService để sửa lỗi DI Crash
            builder.Services.AddScoped<ISmsService, SmsService>();

            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IProfileService, ProfileService>();
            builder.Services.AddScoped<IUserAddressService, UserAddressService>();

            // Chỉ định rõ namespace để tránh xung đột với Entity WorkerService trong DAL
            builder.Services.AddScoped<IWorkerService, Cleaning.BLL.Services.WorkerService>();

            builder.Services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
            builder.Services.AddScoped<IBookingService, BookingService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<IReviewService, ReviewService>();
            builder.Services.AddScoped<IAdminService, AdminService>();

            // AI Service sử dụng HttpClient
            builder.Services.AddHttpClient<IAiService, AiService>();

            // ==========================================
            // 6. KHỞI TẠO APP VÀ PIPELINE
            // ==========================================
            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            // [ĐÃ THÊM] Cho phép truy cập file tĩnh (Avatar upload)
            app.UseStaticFiles();

            // Lưu ý: UseAuthentication phải nằm TRƯỚC UseAuthorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}