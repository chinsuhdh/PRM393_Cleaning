using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Cleaning.DAL.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }
    public virtual DbSet<AiCleanlinessAnalysis> AiCleanlinessAnalyses { get; set; }
    public virtual DbSet<AiConversation> AiConversations { get; set; }
    public virtual DbSet<AiInferenceLog> AiInferenceLogs { get; set; }
    public virtual DbSet<AiMessage> AiMessages { get; set; }
    public virtual DbSet<Booking> Bookings { get; set; }
    public virtual DbSet<BookingCancellation> BookingCancellations { get; set; }
    public virtual DbSet<BookingPhoto> BookingPhotos { get; set; }
    public virtual DbSet<BookingRescheduleRequest> BookingRescheduleRequests { get; set; }
    public virtual DbSet<BookingStatusLog> BookingStatusLogs { get; set; }
    public virtual DbSet<ExternalLogin> ExternalLogins { get; set; }
    public virtual DbSet<KnowledgeDocument> KnowledgeDocuments { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }
    public virtual DbSet<Profile> Profiles { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<Refund> Refunds { get; set; }
    public virtual DbSet<Review> Reviews { get; set; }
    public virtual DbSet<Service> Services { get; set; }
    public virtual DbSet<UserAddress> UserAddresses { get; set; }
    public virtual DbSet<VerificationCode> VerificationCodes { get; set; }
    public virtual DbSet<WorkerAvailability> WorkerAvailabilities { get; set; }
    public virtual DbSet<WorkerProfile> WorkerProfiles { get; set; }
    public virtual DbSet<WorkerService> WorkerServices { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseNpgsql(connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum<UserRole>(null, "user_role")
            .HasPostgresEnum<AccountStatus>(null, "account_status")
            .HasPostgresEnum<VerificationPurpose>(null, "verification_purpose")
            .HasPostgresEnum<PropertyType>(null, "property_type")
            .HasPostgresEnum<ServiceUnitType>(null, "service_unit_type")
            .HasPostgresEnum<BookingType>(null, "booking_type")
            .HasPostgresEnum<BookingStatus>(null, "booking_status")
            .HasPostgresEnum<WorkerOnlineStatus>(null, "worker_online_status")
            .HasPostgresEnum<AvailabilityStatus>(null, "availability_status")
            .HasPostgresEnum<PaymentMethod>(null, "payment_method")
            .HasPostgresEnum<PaymentStatus>(null, "payment_status")
            .HasPostgresEnum<PayoutStatus>(null, "payout_status")
            .HasPostgresEnum<RescheduleStatus>(null, "reschedule_status")
            .HasPostgresEnum<AiSenderType>(null, "ai_sender_type")
            .HasPostgresEnum<PhotoType>(null, "photo_type")
            .HasPostgresEnum<CleanlinessLevel>(null, "cleanliness_level")
            .HasPostgresEnum<NotificationType>(null, "notification_type")
            .HasPostgresExtension("pgcrypto");

        ConfigureAccounts(modelBuilder);
        ConfigureProfiles(modelBuilder);
        ConfigureServices(modelBuilder);
        ConfigureWorkers(modelBuilder);
        ConfigureBookings(modelBuilder);
        ConfigurePayments(modelBuilder);
        ConfigureReviews(modelBuilder);
        ConfigureAi(modelBuilder);
        ConfigureNotifications(modelBuilder);

        OnModelCreatingPartial(modelBuilder);
    }

    private static void ConfigureAccounts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("accounts_pkey");
            entity.ToTable("accounts", table =>
            {
                table.HasCheckConstraint("chk_account_identifier", "email IS NOT NULL OR phone_number IS NOT NULL");
                table.HasCheckConstraint("chk_password_pair", "(password_hash IS NULL AND password_salt IS NULL) OR (password_hash IS NOT NULL AND password_salt IS NOT NULL)");
            });
            entity.HasIndex(e => e.Email, "accounts_email_key").IsUnique();
            entity.HasIndex(e => e.PhoneNumber, "accounts_phone_number_key").IsUnique();
            entity.HasIndex(e => e.Email, "idx_accounts_email");
            entity.HasIndex(e => e.PhoneNumber, "idx_accounts_phone");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PhoneNumber).HasColumnName("phone_number");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.PasswordSalt).HasColumnName("password_salt");
            entity.Property(e => e.Role).HasColumnType("user_role").HasColumnName("role");
            entity.Property(e => e.Status).HasColumnType("account_status").HasColumnName("status");
            entity.Property(e => e.IsEmailVerified).HasDefaultValue(false).HasColumnName("is_email_verified");
            entity.Property(e => e.IsPhoneVerified).HasDefaultValue(false).HasColumnName("is_phone_verified");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");
            entity.ToTable("refresh_tokens");
            entity.HasIndex(e => e.AccountId, "idx_refresh_tokens_account");
            entity.HasIndex(e => e.TokenHash, "idx_refresh_tokens_token_hash");
            entity.HasIndex(e => e.TokenHash, "refresh_tokens_token_hash_key").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsRevoked).HasDefaultValue(false).HasColumnName("is_revoked");
            entity.Property(e => e.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash");
            entity.Property(e => e.CreatedByIp).HasColumnName("created_by_ip");
            entity.Property(e => e.RevokedByIp).HasColumnName("revoked_by_ip");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");

            entity.HasOne(e => e.Account).WithMany(e => e.RefreshTokens)
                .HasForeignKey(e => e.AccountId)
                .HasConstraintName("refresh_tokens_account_id_fkey");
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("external_logins_pkey");
            entity.ToTable("external_logins");
            entity.HasIndex(e => new { e.Provider, e.ProviderKey }, "external_logins_provider_provider_key_key").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Provider).HasColumnName("provider");
            entity.Property(e => e.ProviderKey).HasColumnName("provider_key");
            entity.Property(e => e.ProviderDisplayName).HasColumnName("provider_display_name");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.HasOne(e => e.Account).WithMany(e => e.ExternalLogins)
                .HasForeignKey(e => e.AccountId)
                .HasConstraintName("external_logins_account_id_fkey");
        });

        modelBuilder.Entity<VerificationCode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("verification_codes_pkey");
            entity.ToTable("verification_codes");
            entity.HasIndex(e => new { e.AccountId, e.Purpose }, "idx_verification_codes_account_purpose")
                .HasFilter("is_used = FALSE");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CodeHash).HasColumnName("code_hash");
            entity.Property(e => e.Purpose).HasColumnType("verification_purpose").HasColumnName("purpose");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsUsed).HasDefaultValue(false).HasColumnName("is_used");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.HasOne(e => e.Account).WithMany(e => e.VerificationCodes)
                .HasForeignKey(e => e.AccountId)
                .HasConstraintName("verification_codes_account_id_fkey");
        });
    }

    private static void ConfigureProfiles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("profiles_pkey");
            entity.ToTable("profiles");
            entity.Property(e => e.Id).ValueGeneratedNever().HasColumnName("id");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");

            entity.HasOne(e => e.IdNavigation).WithOne(e => e.Profile)
                .HasForeignKey<Profile>(e => e.Id)
                .HasConstraintName("profiles_id_fkey");
        });

        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_addresses_pkey");
            entity.ToTable("user_addresses");
            entity.HasIndex(e => e.UserId, "idx_user_addresses_user");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Label).HasDefaultValueSql("'Home'::text").HasColumnName("label");
            entity.Property(e => e.AddressText).HasColumnName("address_text");
            entity.Property(e => e.Latitude).HasPrecision(10, 7).HasColumnName("latitude");
            entity.Property(e => e.Longitude).HasPrecision(10, 7).HasColumnName("longitude");
            entity.Property(e => e.PropertyType).HasColumnType("property_type").HasDefaultValue(PropertyType.Apartment).HasColumnName("property_type");
            entity.Property(e => e.IsDefault).HasDefaultValue(false).HasColumnName("is_default");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.HasOne(e => e.User).WithMany(e => e.UserAddresses)
                .HasForeignKey(e => e.UserId)
                .HasConstraintName("user_addresses_user_id_fkey");
        });
    }

    private static void ConfigureServices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("services_pkey");
            entity.ToTable("services", table =>
            {
                table.HasCheckConstraint("chk_services_base_price", "base_price >= 0");
                table.HasCheckConstraint("chk_services_minimum_hours", "minimum_hours > 0");
            });
            entity.HasIndex(e => e.Name, "services_name_key").IsUnique();
            entity.HasIndex(e => new { e.PropertyType, e.IsActive }, "idx_services_property_active");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PropertyType).HasColumnType("property_type").HasDefaultValue(PropertyType.Apartment).HasColumnName("property_type");
            entity.Property(e => e.UnitType).HasColumnType("service_unit_type").HasColumnName("unit_type");
            entity.Property(e => e.BasePrice).HasPrecision(12, 2).HasColumnName("base_price");
            entity.Property(e => e.MinimumHours).HasDefaultValue(2).HasColumnName("minimum_hours");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        });
    }

    private static void ConfigureWorkers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkerProfile>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("worker_profiles_pkey");
            entity.ToTable("worker_profiles", table =>
            {
                table.HasCheckConstraint("chk_worker_rating", "average_rating BETWEEN 0 AND 5");
            });
            entity.HasIndex(e => new { e.OnlineStatus, e.ImmediateBookingEnabled }, "idx_worker_online_status");

            entity.Property(e => e.UserId).ValueGeneratedNever().HasColumnName("user_id");
            entity.Property(e => e.AverageRating).HasPrecision(3, 2).HasDefaultValueSql("5.00").HasColumnName("average_rating");
            entity.Property(e => e.OnlineStatus).HasColumnType("worker_online_status").HasDefaultValue(WorkerOnlineStatus.Offline).HasColumnName("online_status");
            entity.Property(e => e.CurrentLat).HasPrecision(10, 7).HasColumnName("current_lat");
            entity.Property(e => e.CurrentLng).HasPrecision(10, 7).HasColumnName("current_lng");
            entity.Property(e => e.ImmediateBookingEnabled).HasDefaultValue(false).HasColumnName("immediate_booking_enabled");
            entity.Property(e => e.VerifiedAt).HasColumnName("verified_at");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
            entity.HasOne(e => e.User).WithOne(e => e.WorkerProfile)
                .HasForeignKey<WorkerProfile>(e => e.UserId)
                .HasConstraintName("worker_profiles_user_id_fkey");
        });

        modelBuilder.Entity<WorkerService>(entity =>
        {
            entity.HasKey(e => new { e.WorkerId, e.ServiceId }).HasName("worker_services_pkey");
            entity.ToTable("worker_services", table =>
            {
                table.HasCheckConstraint("chk_worker_services_experience", "experience_months >= 0");
            });
            entity.HasIndex(e => e.ServiceId, "idx_worker_services_service");

            entity.Property(e => e.WorkerId).HasColumnName("worker_id");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.ExperienceMonths).HasDefaultValue(0).HasColumnName("experience_months");
            entity.Property(e => e.IsVerified).HasDefaultValue(false).HasColumnName("is_verified");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.HasOne(e => e.Worker).WithMany(e => e.WorkerServices)
                .HasForeignKey(e => e.WorkerId)
                .HasConstraintName("worker_services_worker_id_fkey");
            entity.HasOne(e => e.Service).WithMany(e => e.WorkerServices)
                .HasForeignKey(e => e.ServiceId)
                .HasConstraintName("worker_services_service_id_fkey");
        });

        modelBuilder.Entity<WorkerAvailability>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("worker_availability_pkey");
            entity.ToTable("worker_availability", table =>
            {
                table.HasCheckConstraint("chk_worker_availability_time", "end_time > start_time");
            });
            entity.HasIndex(e => new { e.WorkerId, e.StartTime, e.EndTime }, "idx_worker_availability_worker_time");
            entity.HasIndex(e => new { e.StartTime, e.EndTime, e.Status }, "idx_worker_availability_time_status");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.WorkerId).HasColumnName("worker_id");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.Status).HasColumnType("availability_status").HasColumnName("status");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.HasOne(e => e.Worker).WithMany(e => e.WorkerAvailabilities)
                .HasForeignKey(e => e.WorkerId)
                .HasConstraintName("worker_availability_worker_id_fkey");
        });
    }

    private static void ConfigureBookings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("bookings_pkey");
            entity.ToTable("bookings", table =>
            {
                table.HasCheckConstraint("chk_booking_time", "scheduled_end_time > scheduled_start_time");
                table.HasCheckConstraint("chk_booking_client_worker_different", "worker_id IS NULL OR client_id <> worker_id");
            });
            entity.HasIndex(e => e.ClientId, "idx_bookings_client");
            entity.HasIndex(e => e.WorkerId, "idx_bookings_worker");
            entity.HasIndex(e => e.Status, "idx_bookings_status");
            entity.HasIndex(e => new { e.ScheduledStartTime, e.ScheduledEndTime }, "idx_bookings_schedule");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.WorkerId).HasColumnName("worker_id");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.AddressId).HasColumnName("address_id");
            entity.Property(e => e.BookingType).HasColumnType("booking_type").HasDefaultValue(BookingType.Scheduled).HasColumnName("booking_type");
            entity.Property(e => e.ScheduledStartTime).HasColumnName("scheduled_start_time");
            entity.Property(e => e.ScheduledEndTime).HasColumnName("scheduled_end_time");
            entity.Property(e => e.ActualStartTime).HasColumnName("actual_start_time");
            entity.Property(e => e.ActualEndTime).HasColumnName("actual_end_time");
            entity.Property(e => e.DurationHours).HasPrecision(4, 2).HasColumnName("duration_hours");
            entity.Property(e => e.UnitPrice).HasPrecision(12, 2).HasColumnName("unit_price");
            entity.Property(e => e.ExtraFee).HasPrecision(12, 2).HasDefaultValue(0m).HasColumnName("extra_fee");
            entity.Property(e => e.DiscountAmount).HasPrecision(12, 2).HasDefaultValue(0m).HasColumnName("discount_amount");
            entity.Property(e => e.TotalPrice).HasPrecision(12, 2).HasColumnName("total_price");
            entity.Property(e => e.Status).HasColumnType("booking_status").HasColumnName("status");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
            entity.HasOne(e => e.Client).WithMany(e => e.Bookings)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("bookings_client_id_fkey");
            entity.HasOne(e => e.Worker).WithMany(e => e.Bookings)
                .HasForeignKey(e => e.WorkerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("bookings_worker_id_fkey");
            entity.HasOne(e => e.Service).WithMany(e => e.Bookings)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("bookings_service_id_fkey");
            entity.HasOne(e => e.Address).WithMany(e => e.Bookings)
                .HasForeignKey(e => e.AddressId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("bookings_address_id_fkey");
        });

        modelBuilder.Entity<BookingStatusLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("booking_status_logs_pkey");
            entity.ToTable("booking_status_logs");
            entity.HasIndex(e => e.BookingId, "idx_booking_logs_booking");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.OldStatus).HasColumnType("booking_status").HasColumnName("old_status");
            entity.Property(e => e.NewStatus).HasColumnType("booking_status").HasColumnName("new_status");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.HasOne(e => e.Booking).WithMany(e => e.BookingStatusLogs)
                .HasForeignKey(e => e.BookingId)
                .HasConstraintName("booking_status_logs_booking_id_fkey");
            entity.HasOne(e => e.ChangedByNavigation).WithMany(e => e.BookingStatusLogs)
                .HasForeignKey(e => e.ChangedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("booking_status_logs_changed_by_fkey");
        });

        modelBuilder.Entity<BookingCancellation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("booking_cancellations_pkey");
            entity.ToTable("booking_cancellations");
            entity.HasIndex(e => e.BookingId, "booking_cancellations_booking_id_key").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.CancellationFee).HasPrecision(12, 2).HasDefaultValue(0m).HasColumnName("cancellation_fee");
            entity.Property(e => e.RefundAmount).HasPrecision(12, 2).HasDefaultValue(0m).HasColumnName("refund_amount");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.HasOne(e => e.Booking).WithMany(e => e.BookingCancellations)
                .HasForeignKey(e => e.BookingId)
                .HasConstraintName("booking_cancellations_booking_id_fkey");
            entity.HasOne(e => e.CancelledByNavigation).WithMany(e => e.BookingCancellations)
                .HasForeignKey(e => e.CancelledBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("booking_cancellations_cancelled_by_fkey");
        });

        modelBuilder.Entity<BookingRescheduleRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("booking_reschedule_requests_pkey");
            entity.ToTable("booking_reschedule_requests", table =>
            {
                table.HasCheckConstraint("chk_reschedule_old_time", "old_end_time > old_start_time");
                table.HasCheckConstraint("chk_reschedule_new_time", "new_end_time > new_start_time");
            });
            entity.HasIndex(e => new { e.BookingId, e.Status }, "idx_reschedule_booking_status");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.RequestedBy).HasColumnName("requested_by");
            entity.Property(e => e.OldStartTime).HasColumnName("old_start_time");
            entity.Property(e => e.OldEndTime).HasColumnName("old_end_time");
            entity.Property(e => e.NewStartTime).HasColumnName("new_start_time");
            entity.Property(e => e.NewEndTime).HasColumnName("new_end_time");
            entity.Property(e => e.Status).HasColumnType("reschedule_status").HasColumnName("status");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.RespondedBy).HasColumnName("responded_by");
            entity.Property(e => e.RespondedAt).HasColumnName("responded_at");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.HasOne(e => e.Booking).WithMany(e => e.BookingRescheduleRequests)
                .HasForeignKey(e => e.BookingId)
                .HasConstraintName("booking_reschedule_requests_booking_id_fkey");
            entity.HasOne(e => e.RequestedByNavigation).WithMany(e => e.BookingRescheduleRequestRequesters)
                .HasForeignKey(e => e.RequestedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("booking_reschedule_requests_requested_by_fkey");
            entity.HasOne(e => e.RespondedByNavigation).WithMany(e => e.BookingRescheduleRequestResponders)
                .HasForeignKey(e => e.RespondedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("booking_reschedule_requests_responded_by_fkey");
        });
    }

    private static void ConfigurePayments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payments_pkey");
            entity.ToTable("payments", table =>
            {
                table.HasCheckConstraint("chk_payments_amount", "amount >= 0");
            });
            entity.HasIndex(e => e.BookingId, "payments_booking_id_key").IsUnique();
            entity.HasIndex(e => e.BookingId, "idx_payments_booking");
            entity.HasIndex(e => e.Status, "idx_payments_status");
            entity.HasIndex(e => e.TransactionId, "payments_transaction_id_key").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.Amount).HasPrecision(12, 2).HasColumnName("amount");
            entity.Property(e => e.Method).HasColumnType("payment_method").HasColumnName("method");
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.Status).HasColumnType("payment_status").HasColumnName("status");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.HasOne(e => e.Booking).WithOne(e => e.Payment)
                .HasForeignKey<Payment>(e => e.BookingId)
                .HasConstraintName("payments_booking_id_fkey");
        });

        modelBuilder.Entity<Refund>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refunds_pkey");
            entity.ToTable("refunds", table =>
            {
                table.HasCheckConstraint("chk_refunds_amount", "amount >= 0");
            });

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount).HasPrecision(12, 2).HasColumnName("amount");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.HasOne(e => e.Payment).WithMany(e => e.Refunds)
                .HasForeignKey(e => e.PaymentId)
                .HasConstraintName("refunds_payment_id_fkey");
        });
    }

    private static void ConfigureReviews(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("reviews_pkey");
            entity.ToTable("reviews", table =>
            {
                table.HasCheckConstraint("chk_reviews_rating", "rating BETWEEN 1 AND 5");
                table.HasCheckConstraint("chk_review_not_self", "reviewer_id <> reviewee_id");
            });
            entity.HasIndex(e => e.BookingId, "idx_reviews_booking");
            entity.HasIndex(e => e.RevieweeId, "idx_reviews_reviewee");
            entity.HasIndex(e => new { e.BookingId, e.ReviewerId, e.RevieweeId }, "reviews_booking_id_reviewer_id_reviewee_id_key").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.ReviewerId).HasColumnName("reviewer_id");
            entity.Property(e => e.RevieweeId).HasColumnName("reviewee_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.HasOne(e => e.Booking).WithMany(e => e.Reviews)
                .HasForeignKey(e => e.BookingId)
                .HasConstraintName("reviews_booking_id_fkey");
            entity.HasOne(e => e.Reviewer).WithMany(e => e.ReviewReviewers)
                .HasForeignKey(e => e.ReviewerId)
                .HasConstraintName("reviews_reviewer_id_fkey");
            entity.HasOne(e => e.Reviewee).WithMany(e => e.ReviewReviewees)
                .HasForeignKey(e => e.RevieweeId)
                .HasConstraintName("reviews_reviewee_id_fkey");
        });
    }

    private static void ConfigureAi(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiConversation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ai_conversations_pkey");
            entity.ToTable("ai_conversations");
            entity.HasIndex(e => e.UserId, "idx_ai_conversations_user");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.HasOne(e => e.User).WithMany(e => e.AiConversations)
                .HasForeignKey(e => e.UserId)
                .HasConstraintName("ai_conversations_user_id_fkey");
        });

        modelBuilder.Entity<AiMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ai_messages_pkey");
            entity.ToTable("ai_messages");
            entity.HasIndex(e => e.ConversationId, "idx_ai_messages_conversation");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.SenderType).HasColumnType("ai_sender_type").HasColumnName("sender_type");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.HasOne(e => e.Conversation).WithMany(e => e.AiMessages)
                .HasForeignKey(e => e.ConversationId)
                .HasConstraintName("ai_messages_conversation_id_fkey");
        });

        modelBuilder.Entity<KnowledgeDocument>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("knowledge_documents_pkey");
            entity.ToTable("knowledge_documents");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.Source).HasColumnName("source");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        });

        modelBuilder.Entity<AiInferenceLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ai_inference_logs_pkey");
            entity.ToTable("ai_inference_logs");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Prompt).HasColumnName("prompt");
            entity.Property(e => e.Response).HasColumnName("response");
            entity.Property(e => e.LatencyMs).HasColumnName("latency_ms");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.HasOne(e => e.User).WithMany(e => e.AiInferenceLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("ai_inference_logs_user_id_fkey");
        });

        modelBuilder.Entity<BookingPhoto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("booking_photos_pkey");
            entity.ToTable("booking_photos");
            entity.HasIndex(e => e.BookingId, "idx_booking_photos_booking");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");
            entity.Property(e => e.PhotoUrl).HasColumnName("photo_url");
            entity.Property(e => e.PhotoType).HasColumnType("photo_type").HasColumnName("photo_type");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.HasOne(e => e.Booking).WithMany(e => e.BookingPhotos)
                .HasForeignKey(e => e.BookingId)
                .HasConstraintName("booking_photos_booking_id_fkey");
            entity.HasOne(e => e.UploadedByNavigation).WithMany(e => e.BookingPhotos)
                .HasForeignKey(e => e.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("booking_photos_uploaded_by_fkey");
        });

        modelBuilder.Entity<AiCleanlinessAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ai_cleanliness_analyses_pkey");
            entity.ToTable("ai_cleanliness_analyses", table =>
            {
                table.HasCheckConstraint("chk_ai_cleanliness_confidence", "confidence_score BETWEEN 0 AND 1");
            });
            entity.HasIndex(e => e.BookingPhotoId, "idx_ai_cleanliness_photo");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.BookingPhotoId).HasColumnName("booking_photo_id");
            entity.Property(e => e.CleanlinessLevel).HasColumnType("cleanliness_level").HasColumnName("cleanliness_level");
            entity.Property(e => e.ConfidenceScore).HasPrecision(5, 4).HasColumnName("confidence_score");
            entity.Property(e => e.DetectedIssues).HasColumnType("jsonb").HasColumnName("detected_issues");
            entity.Property(e => e.SuggestedTasks).HasColumnType("jsonb").HasColumnName("suggested_tasks");
            entity.Property(e => e.Summary).HasColumnName("summary");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.HasOne(e => e.BookingPhoto).WithMany(e => e.AiCleanlinessAnalyses)
                .HasForeignKey(e => e.BookingPhotoId)
                .HasConstraintName("ai_cleanliness_analyses_booking_photo_id_fkey");
        });
    }

    private static void ConfigureNotifications(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");
            entity.ToTable("notifications");
            entity.HasIndex(e => e.UserId, "idx_notifications_user_unread").HasFilter("is_read = FALSE");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.Type).HasColumnType("notification_type").HasColumnName("type");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.IsRead).HasDefaultValue(false).HasColumnName("is_read");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.HasOne(e => e.User).WithMany(e => e.Notifications)
                .HasForeignKey(e => e.UserId)
                .HasConstraintName("notifications_user_id_fkey");
            entity.HasOne(e => e.Booking).WithMany(e => e.Notifications)
                .HasForeignKey(e => e.BookingId)
                .HasConstraintName("notifications_booking_id_fkey");
        });
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
