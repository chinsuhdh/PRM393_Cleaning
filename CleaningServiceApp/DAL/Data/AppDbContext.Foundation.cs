using System.Text;
using Cleaning.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cleaning.DAL.Data;

public partial class AppDbContext
{
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();
    public DbSet<BookingMessage> BookingMessages => Set<BookingMessage>();
    public DbSet<BookingWorkerOffer> BookingWorkerOffers => Set<BookingWorkerOffer>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<NotificationOutbox> NotificationOutbox => Set<NotificationOutbox>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionRedemption> PromotionRedemptions => Set<PromotionRedemption>();
    public DbSet<WorkerApplication> WorkerApplications => Set<WorkerApplication>();
    public DbSet<WorkerEarning> WorkerEarnings => Set<WorkerEarning>();

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        ConfigureExistingFoundation(modelBuilder);
        ConfigureWorkerApplications(modelBuilder.Entity<WorkerApplication>());
        ConfigurePromotions(modelBuilder);
        ConfigureBookingOffers(modelBuilder.Entity<BookingWorkerOffer>());
        ConfigureBookingMessages(modelBuilder.Entity<BookingMessage>());
        ConfigureDeviceTokens(modelBuilder.Entity<DeviceToken>());
        ConfigureNotificationOutbox(modelBuilder.Entity<NotificationOutbox>());
        ConfigureWorkerEarnings(modelBuilder.Entity<WorkerEarning>());
        ConfigureAdminAuditLogs(modelBuilder.Entity<AdminAuditLog>());
    }

    private static void ConfigureExistingFoundation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.Property(e => e.DeletionRequestedAt).HasColumnName("deletion_requested_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletionStatus).HasColumnName("deletion_status");
            entity.Property(e => e.NotificationPreferences).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").HasColumnName("notification_preferences");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
            entity.Property(e => e.BookingFormSchema).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").HasColumnName("booking_form_schema");
            entity.Property(e => e.OperatingSchedule).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").HasColumnName("operating_schedule");
            entity.Property(e => e.ArchivedAt).HasColumnName("archived_at");
            entity.Property(e => e.Version).IsConcurrencyToken().HasDefaultValue(1).HasColumnName("version");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(e => e.OptionAnswers).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").HasColumnName("option_answers");
            entity.Property(e => e.PricingBreakdown).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").HasColumnName("pricing_breakdown");
            entity.Property(e => e.AddressSnapshot).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").HasColumnName("address_snapshot");
            entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key");
            entity.Property(e => e.Version).IsConcurrencyToken().HasDefaultValue(1).HasColumnName("version");
            entity.HasIndex(e => new { e.ClientId, e.IdempotencyKey }).IsUnique().HasFilter("idempotency_key IS NOT NULL");
        });

        modelBuilder.Entity<BookingRescheduleRequest>(entity =>
        {
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.PriceDifference).HasPrecision(12, 2).HasDefaultValue(0m).HasColumnName("price_difference");
            entity.Property(e => e.Version).IsConcurrencyToken().HasDefaultValue(1).HasColumnName("version");
            entity.HasIndex(e => e.BookingId).IsUnique().HasFilter("status = 'pending'");
        });

        modelBuilder.Entity<WorkerProfile>(entity =>
        {
            entity.Property(e => e.LocationUpdatedAt).HasColumnName("location_updated_at");
            entity.Property(e => e.BaseLatitude).HasPrecision(10, 7).HasColumnName("base_latitude");
            entity.Property(e => e.BaseLongitude).HasPrecision(10, 7).HasColumnName("base_longitude");
            entity.Property(e => e.ServiceRadiusKm).HasPrecision(6, 2).HasDefaultValue(10m).HasColumnName("service_radius_km");
            entity.Property(e => e.VerificationStatus).HasDefaultValue("pending").HasColumnName("verification_status");
        });

        modelBuilder.Entity<WorkerService>(entity =>
        {
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
            entity.Property(e => e.VerifiedAt).HasColumnName("verified_at");
            entity.Property(e => e.VerifiedBy).HasColumnName("verified_by");
            entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason");
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.VerifiedBy).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WorkerAvailability>().Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
            entity.HasIndex(e => e.UserId).IsUnique().HasFilter("is_default = true");
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_user_addresses_latitude", "latitude IS NULL OR latitude BETWEEN -90 AND 90");
                table.HasCheckConstraint("ck_user_addresses_longitude", "longitude IS NULL OR longitude BETWEEN -180 AND 180");
            });
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
            entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key");
            entity.Property(e => e.ProviderReference).HasColumnName("provider_reference");
            entity.Property(e => e.FailureCode).HasColumnName("failure_code");
            entity.Property(e => e.FailureMessage).HasColumnName("failure_message");
            entity.Property(e => e.CallbackVerifiedAt).HasColumnName("callback_verified_at");
            entity.HasIndex(e => e.IdempotencyKey).IsUnique().HasFilter("idempotency_key IS NOT NULL");
            entity.HasIndex(e => e.ProviderReference).IsUnique().HasFilter("provider_reference IS NOT NULL");
        });

        modelBuilder.Entity<Refund>(entity =>
        {
            entity.Property(e => e.Status).HasDefaultValue("pending").HasColumnName("status");
            entity.Property(e => e.ProviderRefundId).HasColumnName("provider_refund_id");
            entity.Property(e => e.RequestedAt).HasDefaultValueSql("now()").HasColumnName("requested_at");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            entity.Property(e => e.RequestedBy).HasColumnName("requested_by");
            entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key");
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.RequestedBy).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.IdempotencyKey).IsUnique().HasFilter("idempotency_key IS NOT NULL");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.ReadAt).HasColumnName("read_at");
            entity.Property(e => e.ArchivedAt).HasColumnName("archived_at");
            entity.Property(e => e.DeepLink).HasColumnName("deep_link");
            entity.Property(e => e.Payload).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").HasColumnName("payload");
            entity.Property(e => e.DeduplicationKey).HasColumnName("deduplication_key");
            entity.HasIndex(e => new { e.UserId, e.DeduplicationKey }).IsUnique().HasFilter("deduplication_key IS NOT NULL");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
            entity.Property(e => e.EditableUntil).HasColumnName("editable_until");
        });
    }

    private static void ConfigureWorkerApplications(EntityTypeBuilder<WorkerApplication> entity)
    {
        ConfigureEntity(entity, "worker_applications");
        entity.Property(e => e.Status).HasDefaultValue("pending");
        entity.Property(e => e.Evidence).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.Version).IsConcurrencyToken().HasDefaultValue(1);
        entity.HasIndex(e => e.UserId).IsUnique().HasFilter("status = 'pending'");
        entity.HasOne<Account>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Account>().WithMany().HasForeignKey(e => e.ReviewedBy).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigurePromotions(ModelBuilder modelBuilder)
    {
        var promotion = modelBuilder.Entity<Promotion>();
        ConfigureEntity(promotion, "promotions");
        promotion.Property(e => e.DiscountValue).HasPrecision(12, 2);
        promotion.Property(e => e.MaximumDiscountAmount).HasPrecision(12, 2);
        promotion.Property(e => e.MinimumBookingAmount).HasPrecision(12, 2).HasDefaultValue(0m);
        promotion.Property(e => e.Conditions).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        promotion.Property(e => e.Version).IsConcurrencyToken().HasDefaultValue(1);
        promotion.HasIndex(e => e.Code).IsUnique();

        var redemption = modelBuilder.Entity<PromotionRedemption>();
        ConfigureEntity(redemption, "promotion_redemptions");
        redemption.Property(e => e.DiscountAmount).HasPrecision(12, 2);
        redemption.HasIndex(e => e.BookingId).IsUnique();
        redemption.HasIndex(e => new { e.PromotionId, e.UserId });
        redemption.HasOne<Promotion>().WithMany().HasForeignKey(e => e.PromotionId).OnDelete(DeleteBehavior.Restrict);
        redemption.HasOne<Account>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        redemption.HasOne<Booking>().WithMany().HasForeignKey(e => e.BookingId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureBookingOffers(EntityTypeBuilder<BookingWorkerOffer> entity)
    {
        ConfigureEntity(entity, "booking_worker_offers");
        entity.Property(e => e.Status).HasDefaultValue("pending");
        entity.Property(e => e.RankScore).HasPrecision(10, 4);
        entity.HasIndex(e => new { e.BookingId, e.WorkerId }).IsUnique();
        entity.HasIndex(e => e.BookingId).IsUnique().HasFilter("status = 'accepted'");
        entity.HasOne<Booking>().WithMany().HasForeignKey(e => e.BookingId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<WorkerProfile>().WithMany().HasForeignKey(e => e.WorkerId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureBookingMessages(EntityTypeBuilder<BookingMessage> entity)
    {
        ConfigureEntity(entity, "booking_messages");
        entity.HasIndex(e => new { e.BookingId, e.CreatedAt });
        entity.HasOne<Booking>().WithMany().HasForeignKey(e => e.BookingId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Account>().WithMany().HasForeignKey(e => e.SenderId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureDeviceTokens(EntityTypeBuilder<DeviceToken> entity)
    {
        ConfigureEntity(entity, "device_tokens");
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.HasIndex(e => e.Token).IsUnique();
        entity.HasIndex(e => new { e.UserId, e.IsActive });
        entity.HasOne<Account>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureNotificationOutbox(EntityTypeBuilder<NotificationOutbox> entity)
    {
        ConfigureEntity(entity, "notification_outbox");
        entity.Property(e => e.Payload).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.Status).HasDefaultValue("pending");
        entity.HasIndex(e => new { e.Status, e.AvailableAt });
        entity.HasOne<Notification>().WithMany().HasForeignKey(e => e.NotificationId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<Account>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureWorkerEarnings(EntityTypeBuilder<WorkerEarning> entity)
    {
        ConfigureEntity(entity, "worker_earnings");
        entity.Property(e => e.Amount).HasPrecision(12, 2);
        entity.Property(e => e.Status).HasDefaultValue("pending");
        entity.HasIndex(e => e.BookingId).IsUnique();
        entity.HasIndex(e => new { e.WorkerId, e.EarnedAt });
        entity.HasOne<Booking>().WithMany().HasForeignKey(e => e.BookingId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<WorkerProfile>().WithMany().HasForeignKey(e => e.WorkerId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureAdminAuditLogs(EntityTypeBuilder<AdminAuditLog> entity)
    {
        ConfigureEntity(entity, "admin_audit_logs");
        entity.Property(e => e.BeforeState).HasColumnType("jsonb");
        entity.Property(e => e.AfterState).HasColumnType("jsonb");
        entity.HasIndex(e => new { e.EntityType, e.EntityId, e.CreatedAt });
        entity.HasOne<Account>().WithMany().HasForeignKey(e => e.AdminId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureEntity<TEntity>(EntityTypeBuilder<TEntity> entity, string tableName) where TEntity : class
    {
        entity.ToTable(tableName);
        entity.HasKey("Id");
        entity.Property<Guid>("Id").HasDefaultValueSql("gen_random_uuid()");
        foreach (var property in entity.Metadata.GetProperties()) property.SetColumnName(ToSnakeCase(property.Name));
        if (entity.Metadata.FindProperty("CreatedAt") is not null) entity.Property<DateTime>("CreatedAt").HasDefaultValueSql("now()");
        if (entity.Metadata.FindProperty("UpdatedAt") is not null) entity.Property<DateTime>("UpdatedAt").HasDefaultValueSql("now()");
    }

    private static string ToSnakeCase(string value)
    {
        var result = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (index > 0 && char.IsUpper(character)) result.Append('_');
            result.Append(char.ToLowerInvariant(character));
        }
        return result.ToString();
    }
}
