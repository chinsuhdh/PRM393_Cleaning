using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleaning.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddMvpFeatureFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_booking_reschedule_requests_booking_id",
                table: "booking_reschedule_requests");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:account_status", "active,banned,pending_verification")
                .Annotation("Npgsql:Enum:ai_sender_type", "user,ai")
                .Annotation("Npgsql:Enum:availability_status", "available,blocked,booked")
                .Annotation("Npgsql:Enum:booking_status", "pending_payment,paid_pending_worker,accepted,reschedule_requested,in_progress,completed,cancelled,refunded,awaiting_worker")
                .Annotation("Npgsql:Enum:booking_type", "scheduled,immediate")
                .Annotation("Npgsql:Enum:cleanliness_level", "clean,light,medium,heavy")
                .Annotation("Npgsql:Enum:notification_type", "booking,payment,schedule,system,ai")
                .Annotation("Npgsql:Enum:payment_method", "cash,momo,vnpay,zalopay,bank_transfer")
                .Annotation("Npgsql:Enum:payment_status", "pending,success,failed,refunded,partially_refunded")
                .Annotation("Npgsql:Enum:payout_status", "pending,paid,failed")
                .Annotation("Npgsql:Enum:photo_type", "before,after,issue,ai_reference")
                .Annotation("Npgsql:Enum:property_type", "apartment,house")
                .Annotation("Npgsql:Enum:reschedule_status", "pending,accepted,rejected,cancelled")
                .Annotation("Npgsql:Enum:service_unit_type", "hour")
                .Annotation("Npgsql:Enum:user_role", "client,worker,admin")
                .Annotation("Npgsql:Enum:verification_purpose", "email_verification,phone_verification,password_reset")
                .Annotation("Npgsql:Enum:worker_online_status", "offline,online,busy")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .OldAnnotation("Npgsql:Enum:account_status", "active,banned,pending_verification")
                .OldAnnotation("Npgsql:Enum:ai_sender_type", "user,ai")
                .OldAnnotation("Npgsql:Enum:availability_status", "available,blocked,booked")
                .OldAnnotation("Npgsql:Enum:booking_status", "pending_payment,paid_pending_worker,accepted,reschedule_requested,in_progress,completed,cancelled,refunded")
                .OldAnnotation("Npgsql:Enum:booking_type", "scheduled,immediate")
                .OldAnnotation("Npgsql:Enum:cleanliness_level", "clean,light,medium,heavy")
                .OldAnnotation("Npgsql:Enum:notification_type", "booking,payment,schedule,system,ai")
                .OldAnnotation("Npgsql:Enum:payment_method", "cash,momo,vnpay,zalopay,bank_transfer")
                .OldAnnotation("Npgsql:Enum:payment_status", "pending,success,failed,refunded,partially_refunded")
                .OldAnnotation("Npgsql:Enum:payout_status", "pending,paid,failed")
                .OldAnnotation("Npgsql:Enum:photo_type", "before,after,issue,ai_reference")
                .OldAnnotation("Npgsql:Enum:property_type", "apartment,house")
                .OldAnnotation("Npgsql:Enum:reschedule_status", "pending,accepted,rejected,cancelled")
                .OldAnnotation("Npgsql:Enum:service_unit_type", "hour")
                .OldAnnotation("Npgsql:Enum:user_role", "client,worker,admin")
                .OldAnnotation("Npgsql:Enum:verification_purpose", "email_verification,phone_verification,password_reset")
                .OldAnnotation("Npgsql:Enum:worker_online_status", "offline,online,busy")
                .OldAnnotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "worker_services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "worker_services",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "verified_at",
                table: "worker_services",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "verified_by",
                table: "worker_services",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "base_latitude",
                table: "worker_profiles",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "base_longitude",
                table: "worker_profiles",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "location_updated_at",
                table: "worker_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "service_radius_km",
                table: "worker_profiles",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 10m);

            migrationBuilder.AddColumn<string>(
                name: "verification_status",
                table: "worker_profiles",
                type: "text",
                nullable: false,
                defaultValue: "pending");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "worker_availability",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "user_addresses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "archived_at",
                table: "services",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "booking_form_schema",
                table: "services",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "operating_schedule",
                table: "services",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "services",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "services",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "editable_until",
                table: "reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "reviews",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "refunds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "processed_at",
                table: "refunds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_refund_id",
                table: "refunds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "requested_at",
                table: "refunds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<Guid>(
                name: "requested_by",
                table: "refunds",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "refunds",
                type: "text",
                nullable: false,
                defaultValue: "pending");

            migrationBuilder.AddColumn<DateTime>(
                name: "callback_verified_at",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "failure_code",
                table: "payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "failure_message",
                table: "payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_reference",
                table: "payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "payments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "archived_at",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deduplication_key",
                table: "notifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deep_link",
                table: "notifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payload",
                table: "notifications",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<DateTime>(
                name: "read_at",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_snapshot",
                table: "bookings",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "option_answers",
                table: "bookings",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "pricing_breakdown",
                table: "bookings",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "bookings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "expires_at",
                table: "booking_reschedule_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "price_difference",
                table: "booking_reschedule_requests",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "booking_reschedule_requests",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deletion_requested_at",
                table: "accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deletion_status",
                table: "accounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notification_preferences",
                table: "accounts",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.CreateTable(
                name: "admin_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    admin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<string>(type: "text", nullable: true),
                    before_state = table.Column<string>(type: "jsonb", nullable: true),
                    after_state = table.Column<string>(type: "jsonb", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_admin_audit_logs_accounts_admin_id",
                        column: x => x.admin_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "booking_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_booking_messages_accounts_sender_id",
                        column: x => x.sender_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_messages_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "booking_worker_offers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    rank_score = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_worker_offers", x => x.id);
                    table.ForeignKey(
                        name: "FK_booking_worker_offers_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_booking_worker_offers_worker_profiles_worker_id",
                        column: x => x.worker_id,
                        principalTable: "worker_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "device_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    platform = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_device_tokens_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    available_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_outbox", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_outbox_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notification_outbox_notifications_notification_id",
                        column: x => x.notification_id,
                        principalTable: "notifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "promotions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    discount_type = table.Column<string>(type: "text", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    maximum_discount_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    minimum_booking_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_quota = table.Column<int>(type: "integer", nullable: true),
                    per_user_quota = table.Column<int>(type: "integer", nullable: false),
                    redeemed_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    conditions = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "worker_applications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    government_id = table.Column<string>(type: "text", nullable: true),
                    experience_summary = table.Column<string>(type: "text", nullable: true),
                    evidence = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_applications", x => x.id);
                    table.ForeignKey(
                        name: "FK_worker_applications_accounts_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_worker_applications_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "worker_earnings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    earned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_earnings", x => x.id);
                    table.ForeignKey(
                        name: "FK_worker_earnings_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_worker_earnings_worker_profiles_worker_id",
                        column: x => x.worker_id,
                        principalTable: "worker_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "promotion_redemptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_redemptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_promotion_redemptions_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_redemptions_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_redemptions_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_worker_services_verified_by",
                table: "worker_services",
                column: "verified_by");

            migrationBuilder.CreateIndex(
                name: "IX_user_addresses_user_id",
                table: "user_addresses",
                column: "user_id",
                unique: true,
                filter: "is_default = true");

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_addresses_latitude",
                table: "user_addresses",
                sql: "latitude IS NULL OR latitude BETWEEN -90 AND 90");

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_addresses_longitude",
                table: "user_addresses",
                sql: "longitude IS NULL OR longitude BETWEEN -180 AND 180");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_idempotency_key",
                table: "refunds",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_requested_by",
                table: "refunds",
                column: "requested_by");

            migrationBuilder.CreateIndex(
                name: "IX_payments_idempotency_key",
                table: "payments",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payments_provider_reference",
                table: "payments",
                column: "provider_reference",
                unique: true,
                filter: "provider_reference IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id_deduplication_key",
                table: "notifications",
                columns: new[] { "user_id", "deduplication_key" },
                unique: true,
                filter: "deduplication_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_client_id_idempotency_key",
                table: "bookings",
                columns: new[] { "client_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_booking_reschedule_requests_booking_id",
                table: "booking_reschedule_requests",
                column: "booking_id",
                unique: true,
                filter: "status = 'pending'");

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_admin_id",
                table: "admin_audit_logs",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_entity_type_entity_id_created_at",
                table: "admin_audit_logs",
                columns: new[] { "entity_type", "entity_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_messages_booking_id_created_at",
                table: "booking_messages",
                columns: new[] { "booking_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_messages_sender_id",
                table: "booking_messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_worker_offers_booking_id",
                table: "booking_worker_offers",
                column: "booking_id",
                unique: true,
                filter: "status = 'accepted'");

            migrationBuilder.CreateIndex(
                name: "IX_booking_worker_offers_booking_id_worker_id",
                table: "booking_worker_offers",
                columns: new[] { "booking_id", "worker_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_worker_offers_worker_id",
                table: "booking_worker_offers",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "IX_device_tokens_token",
                table: "device_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_device_tokens_user_id_is_active",
                table: "device_tokens",
                columns: new[] { "user_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_outbox_notification_id",
                table: "notification_outbox",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_outbox_status_available_at",
                table: "notification_outbox",
                columns: new[] { "status", "available_at" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_outbox_user_id",
                table: "notification_outbox",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_redemptions_booking_id",
                table: "promotion_redemptions",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promotion_redemptions_promotion_id_user_id",
                table: "promotion_redemptions",
                columns: new[] { "promotion_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_redemptions_user_id",
                table: "promotion_redemptions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotions_code",
                table: "promotions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_worker_applications_reviewed_by",
                table: "worker_applications",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "IX_worker_applications_user_id",
                table: "worker_applications",
                column: "user_id",
                unique: true,
                filter: "status = 'pending'");

            migrationBuilder.CreateIndex(
                name: "IX_worker_earnings_booking_id",
                table: "worker_earnings",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_worker_earnings_worker_id_earned_at",
                table: "worker_earnings",
                columns: new[] { "worker_id", "earned_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_refunds_accounts_requested_by",
                table: "refunds",
                column: "requested_by",
                principalTable: "accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_worker_services_accounts_verified_by",
                table: "worker_services",
                column: "verified_by",
                principalTable: "accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_refunds_accounts_requested_by",
                table: "refunds");

            migrationBuilder.DropForeignKey(
                name: "FK_worker_services_accounts_verified_by",
                table: "worker_services");

            migrationBuilder.DropTable(
                name: "admin_audit_logs");

            migrationBuilder.DropTable(
                name: "booking_messages");

            migrationBuilder.DropTable(
                name: "booking_worker_offers");

            migrationBuilder.DropTable(
                name: "device_tokens");

            migrationBuilder.DropTable(
                name: "notification_outbox");

            migrationBuilder.DropTable(
                name: "promotion_redemptions");

            migrationBuilder.DropTable(
                name: "worker_applications");

            migrationBuilder.DropTable(
                name: "worker_earnings");

            migrationBuilder.DropTable(
                name: "promotions");

            migrationBuilder.DropIndex(
                name: "IX_worker_services_verified_by",
                table: "worker_services");

            migrationBuilder.DropIndex(
                name: "IX_user_addresses_user_id",
                table: "user_addresses");

            migrationBuilder.DropCheckConstraint(
                name: "ck_user_addresses_latitude",
                table: "user_addresses");

            migrationBuilder.DropCheckConstraint(
                name: "ck_user_addresses_longitude",
                table: "user_addresses");

            migrationBuilder.DropIndex(
                name: "IX_refunds_idempotency_key",
                table: "refunds");

            migrationBuilder.DropIndex(
                name: "IX_refunds_requested_by",
                table: "refunds");

            migrationBuilder.DropIndex(
                name: "IX_payments_idempotency_key",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_provider_reference",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_notifications_user_id_deduplication_key",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_bookings_client_id_idempotency_key",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_booking_reschedule_requests_booking_id",
                table: "booking_reschedule_requests");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "worker_services");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "worker_services");

            migrationBuilder.DropColumn(
                name: "verified_at",
                table: "worker_services");

            migrationBuilder.DropColumn(
                name: "verified_by",
                table: "worker_services");

            migrationBuilder.DropColumn(
                name: "base_latitude",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "base_longitude",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "location_updated_at",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "service_radius_km",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "verification_status",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "worker_availability");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "user_addresses");

            migrationBuilder.DropColumn(
                name: "archived_at",
                table: "services");

            migrationBuilder.DropColumn(
                name: "booking_form_schema",
                table: "services");

            migrationBuilder.DropColumn(
                name: "operating_schedule",
                table: "services");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "services");

            migrationBuilder.DropColumn(
                name: "version",
                table: "services");

            migrationBuilder.DropColumn(
                name: "editable_until",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "processed_at",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "provider_refund_id",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "requested_at",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "requested_by",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "status",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "callback_verified_at",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "failure_code",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "failure_message",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "provider_reference",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "archived_at",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "deduplication_key",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "deep_link",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "payload",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "read_at",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "address_snapshot",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "option_answers",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "pricing_breakdown",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "version",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "booking_reschedule_requests");

            migrationBuilder.DropColumn(
                name: "price_difference",
                table: "booking_reschedule_requests");

            migrationBuilder.DropColumn(
                name: "version",
                table: "booking_reschedule_requests");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "deletion_requested_at",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "deletion_status",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "notification_preferences",
                table: "accounts");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:account_status", "active,banned,pending_verification")
                .Annotation("Npgsql:Enum:ai_sender_type", "user,ai")
                .Annotation("Npgsql:Enum:availability_status", "available,blocked,booked")
                .Annotation("Npgsql:Enum:booking_status", "pending_payment,paid_pending_worker,accepted,reschedule_requested,in_progress,completed,cancelled,refunded")
                .Annotation("Npgsql:Enum:booking_type", "scheduled,immediate")
                .Annotation("Npgsql:Enum:cleanliness_level", "clean,light,medium,heavy")
                .Annotation("Npgsql:Enum:notification_type", "booking,payment,schedule,system,ai")
                .Annotation("Npgsql:Enum:payment_method", "cash,momo,vnpay,zalopay,bank_transfer")
                .Annotation("Npgsql:Enum:payment_status", "pending,success,failed,refunded,partially_refunded")
                .Annotation("Npgsql:Enum:payout_status", "pending,paid,failed")
                .Annotation("Npgsql:Enum:photo_type", "before,after,issue,ai_reference")
                .Annotation("Npgsql:Enum:property_type", "apartment,house")
                .Annotation("Npgsql:Enum:reschedule_status", "pending,accepted,rejected,cancelled")
                .Annotation("Npgsql:Enum:service_unit_type", "hour")
                .Annotation("Npgsql:Enum:user_role", "client,worker,admin")
                .Annotation("Npgsql:Enum:verification_purpose", "email_verification,phone_verification,password_reset")
                .Annotation("Npgsql:Enum:worker_online_status", "offline,online,busy")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .OldAnnotation("Npgsql:Enum:account_status", "active,banned,pending_verification")
                .OldAnnotation("Npgsql:Enum:ai_sender_type", "user,ai")
                .OldAnnotation("Npgsql:Enum:availability_status", "available,blocked,booked")
                .OldAnnotation("Npgsql:Enum:booking_status", "pending_payment,paid_pending_worker,accepted,reschedule_requested,in_progress,completed,cancelled,refunded,awaiting_worker")
                .OldAnnotation("Npgsql:Enum:booking_type", "scheduled,immediate")
                .OldAnnotation("Npgsql:Enum:cleanliness_level", "clean,light,medium,heavy")
                .OldAnnotation("Npgsql:Enum:notification_type", "booking,payment,schedule,system,ai")
                .OldAnnotation("Npgsql:Enum:payment_method", "cash,momo,vnpay,zalopay,bank_transfer")
                .OldAnnotation("Npgsql:Enum:payment_status", "pending,success,failed,refunded,partially_refunded")
                .OldAnnotation("Npgsql:Enum:payout_status", "pending,paid,failed")
                .OldAnnotation("Npgsql:Enum:photo_type", "before,after,issue,ai_reference")
                .OldAnnotation("Npgsql:Enum:property_type", "apartment,house")
                .OldAnnotation("Npgsql:Enum:reschedule_status", "pending,accepted,rejected,cancelled")
                .OldAnnotation("Npgsql:Enum:service_unit_type", "hour")
                .OldAnnotation("Npgsql:Enum:user_role", "client,worker,admin")
                .OldAnnotation("Npgsql:Enum:verification_purpose", "email_verification,phone_verification,password_reset")
                .OldAnnotation("Npgsql:Enum:worker_online_status", "offline,online,busy")
                .OldAnnotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_booking_reschedule_requests_booking_id",
                table: "booking_reschedule_requests",
                column: "booking_id");
        }
    }
}
