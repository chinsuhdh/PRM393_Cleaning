using System.Text;
using Cleaning.BLL.Interfaces;
using Cleaning.BLL.Services;
// [THÊM MỚI] Khai báo namespace chứa EmailConfiguration
using Cleaning.BLL.DTOs;
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

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

            dataSourceBuilder.MapEnum<UserRole>("user_role");
            dataSourceBuilder.MapEnum<AccountStatus>("account_status");
            dataSourceBuilder.MapEnum<ServiceUnitType>("service_unit_type");
            dataSourceBuilder.MapEnum<BookingStatus>("booking_status");
            dataSourceBuilder.MapEnum<PaymentMethod>("payment_method");
            dataSourceBuilder.MapEnum<PaymentStatus>("payment_status");
            dataSourceBuilder.MapEnum<AiSenderType>("ai_sender_type");
            dataSourceBuilder.MapEnum<LogLevelType>("log_level_type");
            dataSourceBuilder.MapEnum<DeployStatusType>("deploy_status_type");

            var dataSource = dataSourceBuilder.Build();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(dataSource));

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
            // [THÊM MỚI] BINDING CẤU HÌNH TỪ APPSETTINGS
            // ==========================================
            builder.Services.Configure<EmailConfiguration>(builder.Configuration.GetSection("EmailConfiguration"));

            // ==========================================
            // ĐĂNG KÝ DEPENDENCY INJECTION
            // ==========================================

            // [THÊM MỚI] Đăng ký Email Service
            builder.Services.AddScoped<IEmailService, EmailService>();

            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IProfileService, ProfileService>();
            builder.Services.AddScoped<IUserAddressService, UserAddressService>();
            builder.Services.AddScoped<IWorkerService, WorkerService>();
            builder.Services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
            builder.Services.AddScoped<IBookingService, BookingService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<IReviewService, ReviewService>();
            builder.Services.AddScoped<IAdminService, AdminService>();

            builder.Services.AddHttpClient<IAiService, AiService>();

            // ==========================================
            // 1. KHỞI TẠO BIẾN app 
            // ==========================================
            var app = builder.Build();

            // ==========================================
            // 3. CẤU HÌNH PIPELINE REQUEST
            // ==========================================
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}