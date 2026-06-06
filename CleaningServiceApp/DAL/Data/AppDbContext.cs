using System;
using System.Collections.Generic;
using Cleaning.DAL.Entities;
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

    public virtual DbSet<AiConversation> AiConversations { get; set; }

    public virtual DbSet<AiInferenceLog> AiInferenceLogs { get; set; }

    public virtual DbSet<AiMessage> AiMessages { get; set; }

    public virtual DbSet<AiModel> AiModels { get; set; }

    public virtual DbSet<AiRecommendation> AiRecommendations { get; set; }

    public virtual DbSet<AiTrainingDatum> AiTrainingData { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingStatusLog> BookingStatusLogs { get; set; }

    public virtual DbSet<DeploymentLog> DeploymentLogs { get; set; }

    public virtual DbSet<DocumentEmbedding> DocumentEmbeddings { get; set; }

    public virtual DbSet<ExternalLogin> ExternalLogins { get; set; }

    public virtual DbSet<KnowledgeDocument> KnowledgeDocuments { get; set; }

    public virtual DbSet<LoginHistory> LoginHistories { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<OtpVerification> OtpVerifications { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Profile> Profiles { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<ServiceCategory> ServiceCategories { get; set; }

    public virtual DbSet<SystemLog> SystemLogs { get; set; }

    public virtual DbSet<UserAddress> UserAddresses { get; set; }

    public virtual DbSet<WorkerProfile> WorkerProfiles { get; set; }

    public virtual DbSet<WorkerSkill> WorkerSkills { get; set; }

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
            .HasPostgresEnum("account_status", new[] { "active", "banned", "pending_verification" })
            .HasPostgresEnum("ai_sender_type", new[] { "user", "ai" })
            .HasPostgresEnum("booking_status", new[] { "pending", "accepted", "in_progress", "completed", "cancelled" })
            .HasPostgresEnum("deploy_status_type", new[] { "success", "failed", "in_progress" })
            .HasPostgresEnum("log_level_type", new[] { "info", "warning", "error", "critical" })
            .HasPostgresEnum("payment_method", new[] { "cash", "momo", "vnpay", "zalopay", "bank_transfer" })
            .HasPostgresEnum("payment_status", new[] { "pending", "success", "failed", "refunded" })
            .HasPostgresEnum("service_unit_type", new[] { "hour", "square_meter", "package" })
            .HasPostgresEnum("user_role", new[] { "client", "worker", "admin" })
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
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Role)
                .HasColumnName("role")
                .HasColumnType("user_role");

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasColumnType("account_status");
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
            entity.Property(e => e.ModelId).HasColumnName("model_id");
            entity.Property(e => e.Prompt).HasColumnName("prompt");
            entity.Property(e => e.Response).HasColumnName("response");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Model).WithMany(p => p.AiInferenceLogs)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("ai_inference_logs_model_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.AiInferenceLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("ai_inference_logs_user_id_fkey");
        });

        modelBuilder.Entity<AiMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ai_messages_pkey");

            entity.ToTable("ai_messages");

            entity.HasIndex(e => e.ConversationId, "idx_ai_messages_conv");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.SenderType).HasColumnName("sender_type");

            entity.HasOne(d => d.Conversation).WithMany(p => p.AiMessages)
                .HasForeignKey(d => d.ConversationId)
                .HasConstraintName("ai_messages_conversation_id_fkey");
        });

        modelBuilder.Entity<AiModel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ai_models_pkey");

            entity.ToTable("ai_models");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.GithubUrl).HasColumnName("github_url");
            entity.Property(e => e.HuggingfaceUrl).HasColumnName("huggingface_url");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(false)
                .HasColumnName("is_active");
            entity.Property(e => e.ModelName).HasColumnName("model_name");
            entity.Property(e => e.ModelVersion).HasColumnName("model_version");
        });

        modelBuilder.Entity<AiRecommendation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ai_recommendations_pkey");

            entity.ToTable("ai_recommendations");

            entity.HasIndex(e => e.BookingId, "idx_ai_recs_booking");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.Score)
                .HasPrecision(5, 4)
                .HasColumnName("score");
            entity.Property(e => e.WorkerId).HasColumnName("worker_id");

            entity.HasOne(d => d.Booking).WithMany(p => p.AiRecommendations)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("ai_recommendations_booking_id_fkey");

            entity.HasOne(d => d.Worker).WithMany(p => p.AiRecommendations)
                .HasForeignKey(d => d.WorkerId)
                .HasConstraintName("ai_recommendations_worker_id_fkey");
        });

        modelBuilder.Entity<AiTrainingDatum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ai_training_data_pkey");

            entity.ToTable("ai_training_data");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Answer).HasColumnName("answer");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Question).HasColumnName("question");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("bookings_pkey");

            entity.ToTable("bookings");

            entity.HasIndex(e => e.ClientId, "idx_bookings_client_id");

            entity.HasIndex(e => e.ScheduledTime, "idx_bookings_scheduled_time");

            entity.HasIndex(e => e.WorkerId, "idx_bookings_worker_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AddressId).HasColumnName("address_id");
            entity.Property(e => e.CancelReason).HasColumnName("cancel_reason");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DurationHours)
                .HasDefaultValue(2)
                .HasColumnName("duration_hours");
            entity.Property(e => e.ExtraFee)
                .HasPrecision(12, 2)
                .HasColumnName("extra_fee");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.Quantity)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("1")
                .HasColumnName("quantity");
            entity.Property(e => e.ScheduledTime).HasColumnName("scheduled_time");
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

            entity.Property(e => e.Status)
          .HasColumnName("status")
          .HasColumnType("booking_status");

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

        modelBuilder.Entity<BookingStatusLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("booking_status_logs_pkey");

            entity.ToTable("booking_status_logs");

            entity.HasIndex(e => e.BookingId, "idx_booking_logs_booking_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Reason).HasColumnName("reason");

            entity.Property(e => e.OldStatus)
          .HasColumnName("old_status")
          .HasColumnType("booking_status");

            entity.Property(e => e.NewStatus)
                  .HasColumnName("new_status")
                  .HasColumnType("booking_status");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingStatusLogs)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("booking_status_logs_booking_id_fkey");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.BookingStatusLogs)
                .HasForeignKey(d => d.ChangedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("booking_status_logs_changed_by_fkey");
        });

        modelBuilder.Entity<DeploymentLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("deployment_logs_pkey");

            entity.ToTable("deployment_logs");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Environment).HasColumnName("environment");
            entity.Property(e => e.FinishedAt).HasColumnName("finished_at");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("started_at");
            entity.Property(e => e.Version).HasColumnName("version");
        });

        modelBuilder.Entity<DocumentEmbedding>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("document_embeddings_pkey");

            entity.ToTable("document_embeddings");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.Embedding).HasColumnName("embedding");

            entity.HasOne(d => d.Document).WithMany(p => p.DocumentEmbeddings)
                .HasForeignKey(d => d.DocumentId)
                .HasConstraintName("document_embeddings_document_id_fkey");
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
            entity.Property(e => e.Source).HasColumnName("source");
            entity.Property(e => e.Title).HasColumnName("title");
        });

        modelBuilder.Entity<LoginHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("login_history_pkey");

            entity.ToTable("login_history");

            entity.HasIndex(e => e.AccountId, "idx_login_history_account");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.FailReason).HasColumnName("fail_reason");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.IsSuccess).HasColumnName("is_success");
            entity.Property(e => e.LoginTime)
                .HasDefaultValueSql("now()")
                .HasColumnName("login_time");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");

            entity.HasOne(d => d.Account).WithMany(p => p.LoginHistories)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("login_history_account_id_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.HasIndex(e => e.UserId, "idx_notifications_user_unread").HasFilter("(is_read = false)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("notifications_user_id_fkey");
        });

        modelBuilder.Entity<OtpVerification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("otp_verifications_pkey");

            entity.ToTable("otp_verifications");

            entity.HasIndex(e => new { e.AccountId, e.Purpose }, "idx_otp_account_purpose").HasFilter("(is_used = false)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsUsed)
                .HasDefaultValue(false)
                .HasColumnName("is_used");
            entity.Property(e => e.OtpCode).HasColumnName("otp_code");
            entity.Property(e => e.Purpose).HasColumnName("purpose");

            entity.HasOne(d => d.Account).WithMany(p => p.OtpVerifications)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("otp_verifications_account_id_fkey");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payments_pkey");

            entity.ToTable("payments");

            entity.HasIndex(e => e.BookingId, "idx_payments_booking_id");

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
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");

            entity.Property(e => e.Method)
          .HasColumnName("method")
          .HasColumnType("payment_method");

            entity.Property(e => e.Status)
                  .HasColumnName("status")
                  .HasColumnType("payment_status");

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
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

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Profile)
                .HasForeignKey<Profile>(d => d.Id)
                .HasConstraintName("profiles_id_fkey");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");

            entity.ToTable("refresh_tokens");

            entity.HasIndex(e => e.Token, "idx_refresh_tokens_token");

            entity.HasIndex(e => e.Token, "refresh_tokens_token_key").IsUnique();

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
            entity.Property(e => e.ReplacedByToken).HasColumnName("replaced_by_token");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.RevokedByIp).HasColumnName("revoked_by_ip");
            entity.Property(e => e.Token).HasColumnName("token");

            entity.HasOne(d => d.Account).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("refresh_tokens_account_id_fkey");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("reviews_pkey");

            entity.ToTable("reviews");

            entity.HasIndex(e => e.BookingId, "idx_reviews_booking_id");

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

            entity.HasIndex(e => e.CategoryId, "idx_services_category_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BasePrice)
                .HasPrecision(12, 2)
                .HasColumnName("base_price");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name).HasColumnName("name");

            entity.Property(e => e.UnitType)
          .HasColumnName("unit_type")
          .HasColumnType("service_unit_type");

            entity.HasOne(d => d.Category).WithMany(p => p.Services)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("services_category_id_fkey");
        });

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("service_categories_pkey");

            entity.ToTable("service_categories");

            entity.HasIndex(e => e.Name, "service_categories_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IconUrl).HasColumnName("icon_url");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.SortOrder)
                .HasDefaultValue(0)
                .HasColumnName("sort_order");
        });

        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("system_logs_pkey");

            entity.ToTable("system_logs");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.ServiceName).HasColumnName("service_name");
        });

        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_addresses_pkey");

            entity.ToTable("user_addresses");

            entity.HasIndex(e => e.UserId, "idx_user_addresses_user_id");

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
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserAddresses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_addresses_user_id_fkey");
        });

        modelBuilder.Entity<WorkerProfile>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("worker_profiles_pkey");

            entity.ToTable("worker_profiles");

            entity.HasIndex(e => e.IdentityCardNumber, "worker_profiles_identity_card_number_key").IsUnique();

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.AverageRating)
                .HasPrecision(3, 2)
                .HasDefaultValueSql("5.00")
                .HasColumnName("average_rating");
            entity.Property(e => e.CompletedJobs)
                .HasDefaultValue(0)
                .HasColumnName("completed_jobs");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentLat)
                .HasPrecision(10, 7)
                .HasColumnName("current_lat");
            entity.Property(e => e.CurrentLng)
                .HasPrecision(10, 7)
                .HasColumnName("current_lng");
            entity.Property(e => e.IdentityCardNumber).HasColumnName("identity_card_number");
            entity.Property(e => e.VerifiedAt).HasColumnName("verified_at");

            entity.HasOne(d => d.User).WithOne(p => p.WorkerProfile)
                .HasForeignKey<WorkerProfile>(d => d.UserId)
                .HasConstraintName("worker_profiles_user_id_fkey");
        });

        modelBuilder.Entity<WorkerSkill>(entity =>
        {
            entity.HasKey(e => new { e.WorkerId, e.ServiceId }).HasName("worker_skills_pkey");

            entity.ToTable("worker_skills");

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

            entity.HasOne(d => d.Service).WithMany(p => p.WorkerSkills)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("worker_skills_service_id_fkey");

            entity.HasOne(d => d.Worker).WithMany(p => p.WorkerSkills)
                .HasForeignKey(d => d.WorkerId)
                .HasConstraintName("worker_skills_worker_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
