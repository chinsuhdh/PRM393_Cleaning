using System.Text;
using System.Text.Json.Serialization;
using AutoMapper;
using Cleaning.BLL.Common;
using Cleaning.BLL.Features.Admin;
using Cleaning.BLL.Features.Ai;
using Cleaning.BLL.Features.Auth;
using Cleaning.BLL.Features.Bookings;
using Cleaning.BLL.Features.Chat;
using Cleaning.BLL.Features.Payments;
using Cleaning.BLL.Features.Profiles;
using Cleaning.BLL.Features.Reviews;
using Cleaning.BLL.Features.ServiceCatalog;
using Cleaning.BLL.Features.UserAddresses;
using Cleaning.BLL.Features.Worker;
using Cleaning.BLL.Infrastructure.Dispatch;
using Cleaning.BLL.Infrastructure.Email;
using Cleaning.BLL.Infrastructure.Sms;
using Cleaning.BLL.Infrastructure.Storage;
using Cleaning.DAL.Data;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;
using Cleaning.DAL.Repositories;
using CleaningService.API.Common;
using CleaningService.API.Data;
using CleaningService.API.Infrastructure.BackgroundJobs;
using CleaningService.API.Infrastructure.Dispatch;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactAdmin", policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

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
            dataSourceBuilder.MapEnum<RescheduleStatus>("reschedule_status");
            dataSourceBuilder.MapEnum<AiSenderType>("ai_sender_type");
            dataSourceBuilder.MapEnum<PhotoType>("photo_type");
            dataSourceBuilder.MapEnum<CleanlinessLevel>("cleanliness_level");
            dataSourceBuilder.MapEnum<NotificationType>("notification_type");

            var dataSource = dataSourceBuilder.Build();

            var seedDevelopmentData = builder.Environment.IsDevelopment();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options
                    .UseNpgsql(dataSource, npgsqlOptions => npgsqlOptions
                        .MapEnum<UserRole>("user_role")
                        .MapEnum<AccountStatus>("account_status")
                        .MapEnum<VerificationPurpose>("verification_purpose")
                        .MapEnum<PropertyType>("property_type")
                        .MapEnum<ServiceUnitType>("service_unit_type")
                        .MapEnum<BookingType>("booking_type")
                        .MapEnum<BookingStatus>("booking_status")
                        .MapEnum<WorkerOnlineStatus>("worker_online_status")
                        .MapEnum<AvailabilityStatus>("availability_status")
                        .MapEnum<PaymentMethod>("payment_method")
                        .MapEnum<PaymentStatus>("payment_status")
                        .MapEnum<RescheduleStatus>("reschedule_status")
                        .MapEnum<AiSenderType>("ai_sender_type")
                        .MapEnum<PhotoType>("photo_type")
                        .MapEnum<CleanlinessLevel>("cleanliness_level")
                        .MapEnum<NotificationType>("notification_type"))
                    .UseSeeding((context, _) =>
                    {
                        if (seedDevelopmentData)
                        {
                            DatabaseSeeder.Seed((AppDbContext)context);
                        }
                    })
                    .UseAsyncSeeding(async (context, _, cancellationToken) =>
                    {
                        if (seedDevelopmentData)
                        {
                            await DatabaseSeeder.SeedAsync((AppDbContext)context, cancellationToken);
                        }
                    }));

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
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var token = context.Request.Query["access_token"];
                            if (!string.IsNullOrEmpty(token) &&
                                context.HttpContext.Request.Path.StartsWithSegments(DispatchConstants.DispatchHubPath))
                                context.Token = token;
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<ApiResponseWrapperFilter>();
            })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            builder.Services.AddSignalR();

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddFixedWindowLimiter(RateLimiterPolicies.AiChat, limiterOptions =>
                {
                    limiterOptions.PermitLimit = AiChatRateLimiterSettings.PermitLimit;
                    limiterOptions.Window = AiChatRateLimiterSettings.Window;
                    limiterOptions.QueueLimit = AiChatRateLimiterSettings.QueueLimit;
                });
            });

            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(kvp => kvp.Value is { Errors.Count: > 0 })
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
                    return new BadRequestObjectResult(
                        ApiResponse.Fail(ResponseMessages.ValidationFailed, AppErrors.ValidationError.Code, errors));
                };
            });

            builder.Services.AddAutoMapper(configuration =>
            {
                configuration.AddProfile<BookingMappingProfile>();
                configuration.AddProfile<WorkerMappingProfile>();
                configuration.AddProfile<ReviewMappingProfile>();
                configuration.AddProfile<UserAddressMappingProfile>();
                configuration.AddProfile<ServiceMappingProfile>();
                configuration.AddProfile<AdminMappingProfile>();
                configuration.AddProfile<ChatMappingProfile>();
                configuration.AddProfile<AiMappingProfile>();
            });
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

            builder.Services.Configure<EmailConfiguration>(builder.Configuration.GetSection("EmailConfiguration"));
            builder.Services.Configure<VnpaySettings>(builder.Configuration.GetSection("VNPay"));
            builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinaryConfig"));

            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IFileStorageService, CloudinaryFileStorageService>();

            builder.Services.AddScoped<ISmsService, SmsService>();

            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IProfileService, ProfileService>();
            builder.Services.AddScoped<IUserAddressService, UserAddressService>();

            builder.Services.AddScoped<IWorkerService, WorkerService>();

            builder.Services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
            builder.Services.AddScoped<IBookingService, BookingService>();
            builder.Services.AddScoped<IBookingAvailabilityService, BookingAvailabilityService>();
            builder.Services.AddScoped<IBookingCreationService, BookingCreationService>();
            builder.Services.AddScoped<IBookingSweepService, BookingSweepService>();
            builder.Services.AddScoped<IDispatchPublisher, DispatchPublisher>();
            builder.Services.AddSingleton<IWorkerPushSender, NullWorkerPushSender>();
            builder.Services.AddHostedService<NearbyWorkerLocationsBroadcastService>();
            builder.Services.AddHostedService<BookingSweeperService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<IReviewService, ReviewService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IChatService, ChatService>();

            builder.Services.AddHttpClient<IAiService, AiService>().AddStandardResilienceHandler();
            builder.Services.AddScoped<IVnpayCheckoutService, VnpayCheckoutService>();

            var app = builder.Build();

            app.UseExceptionHandler();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
            {
                app.UseHttpsRedirection();
            }

            app.UseStaticFiles();

            app.UseCors("AllowReactAdmin");

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter();

            app.MapControllers();
            app.MapHub<DispatchHub>(DispatchConstants.DispatchHubPath);

            app.Run();
        }
    }
}