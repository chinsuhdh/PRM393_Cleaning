
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums; // Gọi thư viện Enums
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

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<UserAddress> UserAddresses { get; set; }

    public virtual DbSet<VAvailableWorkersForScheduledBooking> VAvailableWorkersForScheduledBookings { get; set; }

    public virtual DbSet<VOnlineWorkersForImmediateBooking> VOnlineWorkersForImmediateBookings { get; set; }

    public virtual DbSet<VerificationCode> VerificationCodes { get; set; }

    public virtual DbSet<WorkerAvailability> WorkerAvailabilities { get; set; }

    public virtual DbSet<WorkerProfile> WorkerProfiles { get; set; }

    public virtual DbSet<WorkerService> WorkerServices { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            optionsBuilder.UseNpgsql(config.GetConnectionString("DefaultConnection"));
        }
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Cấu hình Map Enum C# với Enum PostgreSQL
        modelBuilder
            .HasPostgresEnum<AccountStatus>(name: "account_status")
            .HasPostgresEnum<AiSenderType>(name: "ai_sender_type")
            .HasPostgresEnum<AvailabilityStatus>(name: "availability_status")
            .HasPostgresEnum<BookingStatus>(name: "booking_status")
            .HasPostgresEnum<BookingType>(name: "booking_type")
            .HasPostgresEnum<CleanlinessLevel>(name: "cleanliness_level")
            .HasPostgresEnum<NotificationType>(name: "notification_type")
            .HasPostgresEnum<PaymentMethod>(name: "payment_method")
            .HasPostgresEnum<PaymentStatus>(name: "payment_status")
            .HasPostgresEnum<PhotoType>(name: "photo_type")
            .HasPostgresEnum<PropertyType>(name: "property_type")
            .HasPostgresEnum<RescheduleStatus>(name: "reschedule_status")
            .HasPostgresEnum<ServiceUnitType>(name: "service_unit_type")
            .HasPostgresEnum<UserRole>(name: "user_role")
            .HasPostgresEnum<VerificationPurpose>(name: "verification_purpose")
            .HasPostgresEnum<WorkerOnlineStatus>(name: "worker_online_status")
            .HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("accounts_pkey");

            entity.ToTable("accounts");

            entity.HasIndex(e => e.Email, "accounts_email_key").IsUnique();

            entity.HasIndex(e => e.PhoneNumber, "accounts_phone_number_key").IsUnique();

            entity.HasIndex(e => e.Email, "idx_accounts_email");

            entity.HasIndex(e => e.PhoneNumber, "idx_accounts_phone");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.IsEmailVerified)
                .HasDefaultValue(false)
                .HasColumnName("is_email_verified");
            entity.Property(e => e.IsPhoneVerified)
                .HasDefaultValue(false)
                .HasColumnName("is_phone_verified");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.PasswordSalt).HasColumnName("password_salt");
            entity.Property(e => e.PhoneNumber).HasColumnName("phone_number");

            // Map Enum
            entity.Property(e => e.Role).HasColumnType("user_role").HasColumnName("role");
            entity.Property(e => e.Status).HasColumnType("account_status").HasColumnName("status");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<AiCleanlinessAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ai_cleanliness_analyses_pkey");

            entity.ToTable("ai_cleanliness_analyses");

            entity.HasIndex(e => e.BookingPhotoId, "idx_ai_cleanliness_photo");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BookingPhotoId).HasColumnName("booking_photo_id");

            // Map Enum
            entity.Property(e => e.CleanlinessLevel).HasColumnType("cleanliness_level").HasColumnName("cleanliness_level");

            entity.Property(e => e.ConfidenceScore)
                .HasPrecision(5, 4)
                .HasColumnName("confidence_score");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DetectedIssues)
                .HasColumnType("jsonb")
                .HasColumnName("detected_issues");
            entity.Property(e => e.SuggestedTasks)
                .HasColumnType("jsonb")
                .HasColumnName("suggested_tasks");
            entity.Property(e => e.Summary).HasColumnName("summary");

            entity.HasOne(d => d.BookingPhoto).WithMany(p => p.AiCleanlinessAnalyses)
                .HasForeignKey(d => d.BookingPhotoId)
                .HasConstraintName("ai_cleanliness_analyses_booking_photo_id_fkey");
        });

        modelBuilder.Entity<AiConversation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ai_conversations_pkey");

            entity.ToTable("ai_conversations");

            entity.HasIndex(e => e.UserId, "idx_ai_conversations_user");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.AiConversations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("ai_conversations_user_id_fkey");
        });

        modelBuilder.Entity<AiInferenceLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ai_inference_logs_pkey");

            entity.ToTable("ai_inference_logs");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.LatencyMs).HasColumnName("latency_ms");
            entity.Property(e => e.Prompt).HasColumnName("prompt");
            entity.Property(e => e.Response).HasColumnName("response");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.AiInferenceLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("ai_inference_logs_user_id_fkey");
        });

        modelBuilder.Entity<AiMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ai_messages_pkey");

            entity.ToTable("ai_messages");

            entity.HasIndex(e => e.ConversationId, "idx_ai_messages_conversation");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Message).HasColumnName("message");

            // Map Enum
            entity.Property(e => e.SenderType).HasColumnType("ai_sender_type").HasColumnName("sender_type");

            entity.HasOne(d => d.Conversation).WithMany(p => p.AiMessages)
                .HasForeignKey(d => d.ConversationId)
                .HasConstraintName("ai_messages_conversation_id_fkey");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("bookings_pkey");

            entity.ToTable("bookings");

            entity.HasIndex(e => e.ClientId, "idx_bookings_client");

            entity.HasIndex(e => new { e.ScheduledStartTime, e.ScheduledEndTime }, "idx_bookings_schedule");

            entity.HasIndex(e => e.WorkerId, "idx_bookings_worker");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActualEndTime).HasColumnName("actual_end_time");
            entity.Property(e => e.ActualStartTime).HasColumnName("actual_start_time");
            entity.Property(e => e.AddressId).HasColumnName("address_id");

            // Map Enum
            entity.Property(e => e.BookingType).HasColumnType("booking_type").HasColumnName("booking_type");
            entity.Property(e => e.Status).HasColumnType("booking_status").HasColumnName("status");
            entity.Property(e => e.PaymentMethod)
                .HasColumnType("payment_method")
                .HasColumnName("payment_method")
                .HasDefaultValue(PaymentMethod.Cash);

            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscountAmount)
                .HasPrecision(12, 2)
                .HasColumnName("discount_amount");
            entity.Property(e => e.DurationHours)
                .HasPrecision(4, 2)
                .HasColumnName("duration_hours");
            entity.Property(e => e.ExtraFee)
                .HasPrecision(12, 2)
                .HasColumnName("extra_fee");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.ScheduledEndTime).HasColumnName("scheduled_end_time");
            entity.Property(e => e.ScheduledStartTime).HasColumnName("scheduled_start_time");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.TotalPrice)
                .HasPrecision(12, 2)
                .HasColumnName("total_price");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(12, 2)
                .HasColumnName("unit_price");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.WorkerId).HasColumnName("worker_id");

            entity.HasOne(d => d.Address).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.AddressId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("bookings_address_id_fkey");

            entity.HasOne(d => d.Client).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("bookings_client_id_fkey");

            entity.HasOne(d => d.Service).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("bookings_service_id_fkey");

            entity.HasOne(d => d.Worker).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.WorkerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("bookings_worker_id_fkey");
        });

        modelBuilder.Entity<BookingCancellation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("booking_cancellations_pkey");

            entity.ToTable("booking_cancellations");

            entity.HasIndex(e => e.BookingId, "booking_cancellations_booking_id_key");

            entity.HasIndex(e => e.CreatedAt, "idx_booking_cancellations_created_at");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");

            // Map Enum
            entity.Property(e => e.ActorRole).HasColumnType("user_role").HasColumnName("actor_role");

            entity.Property(e => e.ReasonCode)
                .HasMaxLength(40)
                .HasColumnName("reason_code");
            entity.Property(e => e.Reason).HasColumnName("reason");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingCancellations)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("booking_cancellations_booking_id_fkey");

            entity.HasOne(d => d.CancelledByNavigation).WithMany(p => p.BookingCancellations)
                .HasForeignKey(d => d.CancelledBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("booking_cancellations_cancelled_by_fkey");
        });

        modelBuilder.Entity<BookingPhoto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("booking_photos_pkey");

            entity.ToTable("booking_photos");

            entity.HasIndex(e => e.BookingId, "idx_booking_photos_booking");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Note).HasColumnName("note");

            // Map Enum
            entity.Property(e => e.PhotoType).HasColumnType("photo_type").HasColumnName("photo_type");

            entity.Property(e => e.PhotoUrl).HasColumnName("photo_url");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingPhotos)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("booking_photos_booking_id_fkey");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.BookingPhotos)
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("booking_photos_uploaded_by_fkey");
        });

        modelBuilder.Entity<BookingRescheduleRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("booking_reschedule_requests_pkey");

            entity.ToTable("booking_reschedule_requests");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.NewEndTime).HasColumnName("new_end_time");
            entity.Property(e => e.NewStartTime).HasColumnName("new_start_time");
            entity.Property(e => e.OldEndTime).HasColumnName("old_end_time");
            entity.Property(e => e.OldStartTime).HasColumnName("old_start_time");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.RequestedBy).HasColumnName("requested_by");
            entity.Property(e => e.RespondedAt).HasColumnName("responded_at");
            entity.Property(e => e.RespondedBy).HasColumnName("responded_by");

            // Map Enum
            entity.Property(e => e.Status).HasColumnType("reschedule_status").HasColumnName("status");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingRescheduleRequests)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("booking_reschedule_requests_booking_id_fkey");

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.BookingRescheduleRequestRequestedByNavigations)
                .HasForeignKey(d => d.RequestedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("booking_reschedule_requests_requested_by_fkey");

            entity.HasOne(d => d.RespondedByNavigation).WithMany(p => p.BookingRescheduleRequestRespondedByNavigations)
                .HasForeignKey(d => d.RespondedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("booking_reschedule_requests_responded_by_fkey");
        });

        modelBuilder.Entity<BookingStatusLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("booking_status_logs_pkey");

            entity.ToTable("booking_status_logs");

            entity.HasIndex(e => e.BookingId, "idx_booking_logs_booking");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");

            // Map Enum
            entity.Property(e => e.OldStatus).HasColumnType("booking_status").HasColumnName("old_status");
            entity.Property(e => e.NewStatus).HasColumnType("booking_status").HasColumnName("new_status");

            entity.Property(e => e.Reason).HasColumnName("reason");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingStatusLogs)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("booking_status_logs_booking_id_fkey");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.BookingStatusLogs)
                .HasForeignKey(d => d.ChangedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("booking_status_logs_changed_by_fkey");
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("external_logins_pkey");

            entity.ToTable("external_logins");

            entity.HasIndex(e => new { e.Provider, e.ProviderKey }, "external_logins_provider_provider_key_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Provider).HasColumnName("provider");
            entity.Property(e => e.ProviderDisplayName).HasColumnName("provider_display_name");
            entity.Property(e => e.ProviderKey).HasColumnName("provider_key");

            entity.HasOne(d => d.Account).WithMany(p => p.ExternalLogins)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("external_logins_account_id_fkey");
        });

        modelBuilder.Entity<KnowledgeDocument>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("knowledge_documents_pkey");

            entity.ToTable("knowledge_documents");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Source).HasColumnName("source");
            entity.Property(e => e.Title).HasColumnName("title");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.HasIndex(e => e.UserId, "idx_notifications_user_unread").HasFilter("(is_read = false)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.Title).HasColumnName("title");

            // Map Enum
            entity.Property(e => e.Type).HasColumnType("notification_type").HasColumnName("type");

            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Booking).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("notifications_booking_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("notifications_user_id_fkey");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payments_pkey");

            entity.ToTable("payments");

            entity.HasIndex(e => e.BookingId, "idx_payments_booking");

            entity.HasIndex(e => e.BookingId, "payments_booking_id_key").IsUnique();

            entity.HasIndex(e => e.TransactionId, "payments_transaction_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(12, 2)
                .HasColumnName("amount");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");

            // Map Enum
            entity.Property(e => e.Method).HasColumnType("payment_method").HasColumnName("method");
            entity.Property(e => e.Status).HasColumnType("payment_status").HasColumnName("status");

            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");

            entity.HasOne(d => d.Booking).WithOne(p => p.Payment)
                .HasForeignKey<Payment>(d => d.BookingId)
                .HasConstraintName("payments_booking_id_fkey");
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("profiles_pkey");

            entity.ToTable("profiles");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Profile)
                .HasForeignKey<Profile>(d => d.Id)
                .HasConstraintName("profiles_id_fkey");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");

            entity.ToTable("refresh_tokens");

            entity.HasIndex(e => e.AccountId, "idx_refresh_tokens_account");

            entity.HasIndex(e => e.TokenHash, "idx_refresh_tokens_token_hash");

            entity.HasIndex(e => e.TokenHash, "refresh_tokens_token_hash_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByIp).HasColumnName("created_by_ip");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsRevoked)
                .HasDefaultValue(false)
                .HasColumnName("is_revoked");
            entity.Property(e => e.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.RevokedByIp).HasColumnName("revoked_by_ip");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash");

            entity.HasOne(d => d.Account).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("refresh_tokens_account_id_fkey");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("reviews_pkey");

            entity.ToTable("reviews");

            entity.HasIndex(e => e.BookingId, "idx_reviews_booking");

            entity.HasIndex(e => e.RevieweeId, "idx_reviews_reviewee");

            entity.HasIndex(e => new { e.BookingId, e.ReviewerId, e.RevieweeId }, "reviews_booking_id_reviewer_id_reviewee_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.RevieweeId).HasColumnName("reviewee_id");
            entity.Property(e => e.ReviewerId).HasColumnName("reviewer_id");

            entity.HasOne(d => d.Booking).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("reviews_booking_id_fkey");

            entity.HasOne(d => d.Reviewee).WithMany(p => p.ReviewReviewees)
                .HasForeignKey(d => d.RevieweeId)
                .HasConstraintName("reviews_reviewee_id_fkey");

            entity.HasOne(d => d.Reviewer).WithMany(p => p.ReviewReviewers)
                .HasForeignKey(d => d.ReviewerId)
                .HasConstraintName("reviews_reviewer_id_fkey");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("services_pkey");

            entity.ToTable("services");

            entity.HasIndex(e => e.Name, "services_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BasePrice)
                .HasPrecision(12, 2)
                .HasColumnName("base_price");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MinimumHours)
                .HasDefaultValue(2)
                .HasColumnName("minimum_hours");
            entity.Property(e => e.Name).HasColumnName("name");

            // Map Enum
            entity.Property(e => e.PropertyType).HasColumnType("property_type").HasColumnName("property_type");
            entity.Property(e => e.UnitType).HasColumnType("service_unit_type").HasColumnName("unit_type");
        });

        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_addresses_pkey");

            entity.ToTable("user_addresses");

            entity.HasIndex(e => e.UserId, "idx_user_addresses_user");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AddressText).HasColumnName("address_text");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsDefault)
                .HasDefaultValue(false)
                .HasColumnName("is_default");
            entity.Property(e => e.Label)
                .HasDefaultValueSql("'Home'::text")
                .HasColumnName("label");
            entity.Property(e => e.Latitude)
                .HasPrecision(10, 7)
                .HasColumnName("latitude");
            entity.Property(e => e.Longitude)
                .HasPrecision(10, 7)
                .HasColumnName("longitude");

            // Map Enum
            entity.Property(e => e.PropertyType).HasColumnType("property_type").HasColumnName("property_type");

            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserAddresses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_addresses_user_id_fkey");
        });

        modelBuilder.Entity<VAvailableWorkersForScheduledBooking>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_available_workers_for_scheduled_booking");

            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.AverageRating)
                .HasPrecision(3, 2)
                .HasColumnName("average_rating");
            entity.Property(e => e.CurrentLat)
                .HasPrecision(10, 7)
                .HasColumnName("current_lat");
            entity.Property(e => e.CurrentLng)
                .HasPrecision(10, 7)
                .HasColumnName("current_lng");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.WorkerId).HasColumnName("worker_id");
        });

        modelBuilder.Entity<VOnlineWorkersForImmediateBooking>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_online_workers_for_immediate_booking");

            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.AverageRating)
                .HasPrecision(3, 2)
                .HasColumnName("average_rating");
            entity.Property(e => e.CurrentLat)
                .HasPrecision(10, 7)
                .HasColumnName("current_lat");
            entity.Property(e => e.CurrentLng)
                .HasPrecision(10, 7)
                .HasColumnName("current_lng");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.WorkerId).HasColumnName("worker_id");
        });

        modelBuilder.Entity<VerificationCode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("verification_codes_pkey");

            entity.ToTable("verification_codes");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CodeHash).HasColumnName("code_hash");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsUsed)
                .HasDefaultValue(false)
                .HasColumnName("is_used");

            // Map Enum
            entity.Property(e => e.Purpose).HasColumnType("verification_purpose").HasColumnName("purpose");

            entity.HasOne(d => d.Account).WithMany(p => p.VerificationCodes)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("verification_codes_account_id_fkey");
        });

        modelBuilder.Entity<WorkerAvailability>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("worker_availability_pkey");

            entity.ToTable("worker_availability");

            entity.HasIndex(e => new { e.WorkerId, e.StartTime, e.EndTime }, "idx_worker_availability_worker_time");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.StartTime).HasColumnName("start_time");

            // Map Enum
            entity.Property(e => e.Status).HasColumnType("availability_status").HasColumnName("status");

            entity.Property(e => e.WorkerId).HasColumnName("worker_id");

            entity.HasOne(d => d.Worker).WithMany(p => p.WorkerAvailabilities)
                .HasForeignKey(d => d.WorkerId)
                .HasConstraintName("worker_availability_worker_id_fkey");
        });

        modelBuilder.Entity<WorkerProfile>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("worker_profiles_pkey");

            entity.ToTable("worker_profiles");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.AverageRating)
                .HasPrecision(3, 2)
                .HasDefaultValueSql("5.00")
                .HasColumnName("average_rating");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentLat)
                .HasPrecision(10, 7)
                .HasColumnName("current_lat");
            entity.Property(e => e.CurrentLng)
                .HasPrecision(10, 7)
                .HasColumnName("current_lng");

            // Map Enum
            entity.Property(e => e.OnlineStatus).HasColumnType("worker_online_status").HasColumnName("online_status");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.VerifiedAt).HasColumnName("verified_at");

            entity.HasOne(d => d.User).WithOne(p => p.WorkerProfile)
                .HasForeignKey<WorkerProfile>(d => d.UserId)
                .HasConstraintName("worker_profiles_user_id_fkey");
        });

        modelBuilder.Entity<WorkerService>(entity =>
        {
            entity.HasKey(e => new { e.WorkerId, e.ServiceId }).HasName("worker_services_pkey");

            entity.ToTable("worker_services");

            entity.HasIndex(e => e.ServiceId, "idx_worker_services_service");

            entity.Property(e => e.WorkerId).HasColumnName("worker_id");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExperienceMonths)
                .HasDefaultValue(0)
                .HasColumnName("experience_months");
            entity.Property(e => e.IsVerified)
                .HasDefaultValue(false)
                .HasColumnName("is_verified");

            entity.HasOne(d => d.Service).WithMany(p => p.WorkerServices)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("worker_services_service_id_fkey");

            entity.HasOne(d => d.Worker).WithMany(p => p.WorkerServices)
                .HasForeignKey(d => d.WorkerId)
                .HasConstraintName("worker_services_worker_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
