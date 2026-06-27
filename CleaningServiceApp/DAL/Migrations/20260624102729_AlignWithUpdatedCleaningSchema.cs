using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleaning.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AlignWithUpdatedCleaningSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "ai_conversations_user_id_fkey",
                table: "ai_conversations");

            migrationBuilder.DropForeignKey(
                name: "ai_inference_logs_model_id_fkey",
                table: "ai_inference_logs");

            migrationBuilder.DropForeignKey(
                name: "services_category_id_fkey",
                table: "services");

            migrationBuilder.DropTable(
                name: "ai_models");

            migrationBuilder.DropTable(
                name: "ai_recommendations");

            migrationBuilder.DropTable(
                name: "ai_training_data");

            migrationBuilder.DropTable(
                name: "deployment_logs");

            migrationBuilder.DropTable(
                name: "document_embeddings");

            migrationBuilder.DropTable(
                name: "login_history");

            migrationBuilder.DropTable(
                name: "otp_verifications");

            migrationBuilder.DropTable(
                name: "service_categories");

            migrationBuilder.DropTable(
                name: "system_logs");

            migrationBuilder.DropTable(
                name: "worker_skills");

            migrationBuilder.DropIndex(
                name: "worker_profiles_identity_card_number_key",
                table: "worker_profiles");

            migrationBuilder.DropIndex(
                name: "idx_services_category_id",
                table: "services");

            migrationBuilder.DropIndex(
                name: "idx_notifications_user_unread",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "idx_bookings_scheduled_time",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_ai_inference_logs_model_id",
                table: "ai_inference_logs");

            migrationBuilder.DropColumn(
                name: "completed_jobs",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "identity_card_number",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "services");

            migrationBuilder.DropColumn(
                name: "cancel_reason",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "model_id",
                table: "ai_inference_logs");

            migrationBuilder.RenameIndex(
                name: "idx_user_addresses_user_id",
                table: "user_addresses",
                newName: "idx_user_addresses_user");

            migrationBuilder.RenameIndex(
                name: "IX_reviews_reviewee_id",
                table: "reviews",
                newName: "idx_reviews_reviewee");

            migrationBuilder.RenameIndex(
                name: "idx_reviews_booking_id",
                table: "reviews",
                newName: "idx_reviews_booking");

            migrationBuilder.RenameColumn(
                name: "token",
                table: "refresh_tokens",
                newName: "token_hash");

            migrationBuilder.RenameColumn(
                name: "replaced_by_token",
                table: "refresh_tokens",
                newName: "replaced_by_token_hash");

            migrationBuilder.RenameIndex(
                name: "refresh_tokens_token_key",
                table: "refresh_tokens",
                newName: "refresh_tokens_token_hash_key");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_account_id",
                table: "refresh_tokens",
                newName: "idx_refresh_tokens_account");

            migrationBuilder.RenameIndex(
                name: "idx_refresh_tokens_token",
                table: "refresh_tokens",
                newName: "idx_refresh_tokens_token_hash");

            migrationBuilder.RenameIndex(
                name: "idx_payments_booking_id",
                table: "payments",
                newName: "idx_payments_booking");

            migrationBuilder.RenameColumn(
                name: "scheduled_time",
                table: "bookings",
                newName: "scheduled_start_time");

            migrationBuilder.RenameIndex(
                name: "idx_bookings_worker_id",
                table: "bookings",
                newName: "idx_bookings_worker");

            migrationBuilder.RenameIndex(
                name: "idx_bookings_client_id",
                table: "bookings",
                newName: "idx_bookings_client");

            migrationBuilder.RenameIndex(
                name: "idx_booking_logs_booking_id",
                table: "booking_status_logs",
                newName: "idx_booking_logs_booking");

            migrationBuilder.RenameIndex(
                name: "idx_ai_messages_conv",
                table: "ai_messages",
                newName: "idx_ai_messages_conversation");

            migrationBuilder.Sql("""
                ALTER TABLE bookings ALTER COLUMN status TYPE text USING status::text;
                ALTER TABLE booking_status_logs ALTER COLUMN old_status TYPE text USING old_status::text;
                ALTER TABLE booking_status_logs ALTER COLUMN new_status TYPE text USING new_status::text;
                ALTER TABLE payments ALTER COLUMN method TYPE text USING method::text;
                ALTER TABLE payments ALTER COLUMN status TYPE text USING status::text;
                ALTER TABLE services ALTER COLUMN unit_type TYPE text USING unit_type::text;

                DROP TYPE booking_status;
                DROP TYPE payment_method;
                DROP TYPE payment_status;
                DROP TYPE service_unit_type;
                DROP TYPE IF EXISTS deploy_status_type;
                DROP TYPE IF EXISTS log_level_type;

                CREATE TYPE booking_status AS ENUM ('pending_payment', 'paid_pending_worker', 'accepted', 'reschedule_requested', 'in_progress', 'completed', 'cancelled', 'refunded');
                CREATE TYPE booking_type AS ENUM ('scheduled', 'immediate');
                CREATE TYPE payment_method AS ENUM ('cash', 'mo_mo', 'vn_pay', 'zalo_pay', 'bank_transfer');
                CREATE TYPE payment_status AS ENUM ('pending', 'success', 'failed', 'refunded', 'partially_refunded');
                CREATE TYPE service_unit_type AS ENUM ('hour');
                CREATE TYPE availability_status AS ENUM ('available', 'blocked', 'booked');
                CREATE TYPE cleanliness_level AS ENUM ('clean', 'light', 'medium', 'heavy');
                CREATE TYPE notification_type AS ENUM ('booking', 'payment', 'schedule', 'system', 'ai');
                CREATE TYPE payout_status AS ENUM ('pending', 'paid', 'failed');
                CREATE TYPE photo_type AS ENUM ('before', 'after', 'issue', 'ai_reference');
                CREATE TYPE property_type AS ENUM ('apartment', 'house');
                CREATE TYPE reschedule_status AS ENUM ('pending', 'accepted', 'rejected', 'cancelled');
                CREATE TYPE verification_purpose AS ENUM ('email_verification', 'phone_verification', 'password_reset');
                CREATE TYPE worker_online_status AS ENUM ('offline', 'online', 'busy');

                UPDATE bookings
                SET status = CASE status
                    WHEN 'pending' THEN 'pending_payment'
                    ELSE status
                END;

                UPDATE booking_status_logs
                SET old_status = CASE old_status
                    WHEN 'pending' THEN 'pending_payment'
                    ELSE old_status
                END,
                new_status = CASE new_status
                    WHEN 'pending' THEN 'pending_payment'
                    ELSE new_status
                END;

                UPDATE payments
                SET method = CASE method
                    WHEN 'momo' THEN 'mo_mo'
                    WHEN 'vnpay' THEN 'vn_pay'
                    WHEN 'zalopay' THEN 'zalo_pay'
                    ELSE method
                END;

                UPDATE services
                SET unit_type = 'hour'
                WHERE unit_type IN ('square_meter', 'package');

                ALTER TABLE bookings ALTER COLUMN status TYPE booking_status USING status::booking_status;
                ALTER TABLE booking_status_logs ALTER COLUMN old_status TYPE booking_status USING old_status::booking_status;
                ALTER TABLE booking_status_logs ALTER COLUMN new_status TYPE booking_status USING new_status::booking_status;
                ALTER TABLE payments ALTER COLUMN method TYPE payment_method USING method::payment_method;
                ALTER TABLE payments ALTER COLUMN status TYPE payment_status USING status::payment_status;
                ALTER TABLE services ALTER COLUMN unit_type TYPE service_unit_type USING unit_type::service_unit_type;
                """);

            migrationBuilder.AddColumn<bool>(
                name: "immediate_booking_enabled",
                table: "worker_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "online_status",
                table: "worker_profiles",
                type: "worker_online_status",
                nullable: false,
                defaultValueSql: "'offline'::worker_online_status");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "worker_profiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<int>(
                name: "property_type",
                table: "user_addresses",
                type: "property_type",
                nullable: false,
                defaultValueSql: "'apartment'::property_type");

            migrationBuilder.AddColumn<int>(
                name: "minimum_hours",
                table: "services",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "property_type",
                table: "services",
                type: "property_type",
                nullable: false,
                defaultValueSql: "'apartment'::property_type");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "profiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<Guid>(
                name: "booking_id",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "notifications",
                type: "notification_type",
                nullable: false,
                defaultValueSql: "'system'::notification_type");

            migrationBuilder.Sql("ALTER TABLE notifications ALTER COLUMN type DROP DEFAULT;");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "knowledge_documents",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "extra_fee",
                table: "bookings",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "duration_hours",
                table: "bookings",
                type: "numeric(4,2)",
                precision: 4,
                scale: 2,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 2);

            migrationBuilder.AddColumn<DateTime>(
                name: "actual_end_time",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "actual_start_time",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "booking_type",
                table: "bookings",
                type: "booking_type",
                nullable: false,
                defaultValueSql: "'scheduled'::booking_type");

            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount",
                table: "bookings",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "scheduled_end_time",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<int>(
                name: "latency_ms",
                table: "ai_inference_logs",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "booking_cancellations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    cancellation_fee = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    refund_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("booking_cancellations_pkey", x => x.id);
                    table.ForeignKey(
                        name: "booking_cancellations_booking_id_fkey",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "booking_cancellations_cancelled_by_fkey",
                        column: x => x.cancelled_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "booking_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    photo_url = table.Column<string>(type: "text", nullable: false),
                    photo_type = table.Column<int>(type: "photo_type", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("booking_photos_pkey", x => x.id);
                    table.ForeignKey(
                        name: "booking_photos_booking_id_fkey",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "booking_photos_uploaded_by_fkey",
                        column: x => x.uploaded_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "booking_reschedule_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                    old_start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    old_end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    new_start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    new_end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "reschedule_status", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    responded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("booking_reschedule_requests_pkey", x => x.id);
                    table.CheckConstraint("chk_reschedule_new_time", "new_end_time > new_start_time");
                    table.CheckConstraint("chk_reschedule_old_time", "old_end_time > old_start_time");
                    table.ForeignKey(
                        name: "booking_reschedule_requests_booking_id_fkey",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "booking_reschedule_requests_requested_by_fkey",
                        column: x => x.requested_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "booking_reschedule_requests_responded_by_fkey",
                        column: x => x.responded_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "refunds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("refunds_pkey", x => x.id);
                    table.CheckConstraint("chk_refunds_amount", "amount >= 0");
                    table.ForeignKey(
                        name: "refunds_payment_id_fkey",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "verification_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "text", nullable: false),
                    purpose = table.Column<int>(type: "verification_purpose", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("verification_codes_pkey", x => x.id);
                    table.ForeignKey(
                        name: "verification_codes_account_id_fkey",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "worker_availability",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "availability_status", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("worker_availability_pkey", x => x.id);
                    table.CheckConstraint("chk_worker_availability_time", "end_time > start_time");
                    table.ForeignKey(
                        name: "worker_availability_worker_id_fkey",
                        column: x => x.worker_id,
                        principalTable: "worker_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "worker_services",
                columns: table => new
                {
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    experience_months = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("worker_services_pkey", x => new { x.worker_id, x.service_id });
                    table.CheckConstraint("chk_worker_services_experience", "experience_months >= 0");
                    table.ForeignKey(
                        name: "worker_services_service_id_fkey",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "worker_services_worker_id_fkey",
                        column: x => x.worker_id,
                        principalTable: "worker_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_cleanliness_analyses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_photo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cleanliness_level = table.Column<int>(type: "cleanliness_level", nullable: false),
                    confidence_score = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    detected_issues = table.Column<string>(type: "jsonb", nullable: true),
                    suggested_tasks = table.Column<string>(type: "jsonb", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("ai_cleanliness_analyses_pkey", x => x.id);
                    table.CheckConstraint("chk_ai_cleanliness_confidence", "confidence_score BETWEEN 0 AND 1");
                    table.ForeignKey(
                        name: "ai_cleanliness_analyses_booking_photo_id_fkey",
                        column: x => x.booking_photo_id,
                        principalTable: "booking_photos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_worker_online_status",
                table: "worker_profiles",
                columns: new[] { "online_status", "immediate_booking_enabled" });

            migrationBuilder.AddCheckConstraint(
                name: "chk_worker_rating",
                table: "worker_profiles",
                sql: "average_rating BETWEEN 0 AND 5");

            migrationBuilder.CreateIndex(
                name: "idx_services_property_active",
                table: "services",
                columns: new[] { "property_type", "is_active" });

            migrationBuilder.CreateIndex(
                name: "services_name_key",
                table: "services",
                column: "name",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "chk_services_base_price",
                table: "services",
                sql: "base_price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_services_minimum_hours",
                table: "services",
                sql: "minimum_hours > 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_review_not_self",
                table: "reviews",
                sql: "reviewer_id <> reviewee_id");

            migrationBuilder.AddCheckConstraint(
                name: "chk_reviews_rating",
                table: "reviews",
                sql: "rating BETWEEN 1 AND 5");

            migrationBuilder.CreateIndex(
                name: "idx_payments_status",
                table: "payments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "payments_booking_id_key",
                table: "payments",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "payments_transaction_id_key",
                table: "payments",
                column: "transaction_id",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "chk_payments_amount",
                table: "payments",
                sql: "amount >= 0");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_user_unread",
                table: "notifications",
                column: "user_id",
                filter: "is_read = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_booking_id",
                table: "notifications",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "idx_bookings_schedule",
                table: "bookings",
                columns: new[] { "scheduled_start_time", "scheduled_end_time" });

            migrationBuilder.CreateIndex(
                name: "idx_bookings_status",
                table: "bookings",
                column: "status");

            migrationBuilder.AddCheckConstraint(
                name: "chk_booking_client_worker_different",
                table: "bookings",
                sql: "worker_id IS NULL OR client_id <> worker_id");

            migrationBuilder.AddCheckConstraint(
                name: "chk_booking_time",
                table: "bookings",
                sql: "scheduled_end_time > scheduled_start_time");

            migrationBuilder.AddCheckConstraint(
                name: "chk_account_identifier",
                table: "accounts",
                sql: "email IS NOT NULL OR phone_number IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "chk_password_pair",
                table: "accounts",
                sql: "(password_hash IS NULL AND password_salt IS NULL) OR (password_hash IS NOT NULL AND password_salt IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_ai_cleanliness_photo",
                table: "ai_cleanliness_analyses",
                column: "booking_photo_id");

            migrationBuilder.CreateIndex(
                name: "booking_cancellations_booking_id_key",
                table: "booking_cancellations",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_cancellations_cancelled_by",
                table: "booking_cancellations",
                column: "cancelled_by");

            migrationBuilder.CreateIndex(
                name: "idx_booking_photos_booking",
                table: "booking_photos",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_photos_uploaded_by",
                table: "booking_photos",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "idx_reschedule_booking_status",
                table: "booking_reschedule_requests",
                columns: new[] { "booking_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_reschedule_requests_requested_by",
                table: "booking_reschedule_requests",
                column: "requested_by");

            migrationBuilder.CreateIndex(
                name: "IX_booking_reschedule_requests_responded_by",
                table: "booking_reschedule_requests",
                column: "responded_by");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_payment_id",
                table: "refunds",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "idx_verification_codes_account_purpose",
                table: "verification_codes",
                columns: new[] { "account_id", "purpose" },
                filter: "is_used = FALSE");

            migrationBuilder.CreateIndex(
                name: "idx_worker_availability_time_status",
                table: "worker_availability",
                columns: new[] { "start_time", "end_time", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_worker_availability_worker_time",
                table: "worker_availability",
                columns: new[] { "worker_id", "start_time", "end_time" });

            migrationBuilder.CreateIndex(
                name: "idx_worker_services_service",
                table: "worker_services",
                column: "service_id");

            migrationBuilder.AddForeignKey(
                name: "ai_conversations_user_id_fkey",
                table: "ai_conversations",
                column: "user_id",
                principalTable: "accounts",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "notifications_booking_id_fkey",
                table: "notifications",
                column: "booking_id",
                principalTable: "bookings",
                principalColumn: "id");

            migrationBuilder.Sql("""
                CREATE OR REPLACE VIEW v_available_workers_for_scheduled_booking AS
                SELECT
                    wp.user_id AS worker_id,
                    p.full_name,
                    wp.average_rating,
                    ws.service_id,
                    wa.start_time,
                    wa.end_time
                FROM worker_profiles wp
                JOIN profiles p ON p.id = wp.user_id
                JOIN worker_services ws ON ws.worker_id = wp.user_id
                JOIN worker_availability wa ON wa.worker_id = wp.user_id
                WHERE wa.status = 'available'
                  AND wp.verified_at IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE VIEW v_online_workers_for_immediate_booking AS
                SELECT
                    wp.user_id AS worker_id,
                    p.full_name,
                    wp.average_rating,
                    wp.current_lat,
                    wp.current_lng,
                    ws.service_id
                FROM worker_profiles wp
                JOIN profiles p ON p.id = wp.user_id
                JOIN worker_services ws ON ws.worker_id = wp.user_id
                WHERE wp.online_status = 'online'
                  AND wp.immediate_booking_enabled = TRUE
                  AND wp.verified_at IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                INSERT INTO services (name, description, property_type, unit_type, base_price, minimum_hours)
                VALUES
                    ('Apartment Cleaning', 'Basic cleaning service for apartments.', 'apartment', 'hour', 120000, 2),
                    ('House Cleaning', 'Basic cleaning service for houses.', 'house', 'hour', 150000, 3)
                ON CONFLICT (name) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_online_workers_for_immediate_booking;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_available_workers_for_scheduled_booking;");

            migrationBuilder.DropForeignKey(
                name: "ai_conversations_user_id_fkey",
                table: "ai_conversations");

            migrationBuilder.DropForeignKey(
                name: "notifications_booking_id_fkey",
                table: "notifications");

            migrationBuilder.DropTable(
                name: "ai_cleanliness_analyses");

            migrationBuilder.DropTable(
                name: "booking_cancellations");

            migrationBuilder.DropTable(
                name: "booking_reschedule_requests");

            migrationBuilder.DropTable(
                name: "refunds");

            migrationBuilder.DropTable(
                name: "verification_codes");

            migrationBuilder.DropTable(
                name: "worker_availability");

            migrationBuilder.DropTable(
                name: "worker_services");

            migrationBuilder.DropTable(
                name: "booking_photos");

            migrationBuilder.DropIndex(
                name: "idx_worker_online_status",
                table: "worker_profiles");

            migrationBuilder.DropCheckConstraint(
                name: "chk_worker_rating",
                table: "worker_profiles");

            migrationBuilder.DropIndex(
                name: "idx_services_property_active",
                table: "services");

            migrationBuilder.DropIndex(
                name: "services_name_key",
                table: "services");

            migrationBuilder.DropCheckConstraint(
                name: "chk_services_base_price",
                table: "services");

            migrationBuilder.DropCheckConstraint(
                name: "chk_services_minimum_hours",
                table: "services");

            migrationBuilder.DropCheckConstraint(
                name: "chk_review_not_self",
                table: "reviews");

            migrationBuilder.DropCheckConstraint(
                name: "chk_reviews_rating",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "idx_payments_status",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "payments_booking_id_key",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "payments_transaction_id_key",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "chk_payments_amount",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "idx_notifications_user_unread",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_booking_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "idx_bookings_schedule",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "idx_bookings_status",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "chk_booking_client_worker_different",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "chk_booking_time",
                table: "bookings");

            migrationBuilder.DropCheckConstraint(
                name: "chk_account_identifier",
                table: "accounts");

            migrationBuilder.DropCheckConstraint(
                name: "chk_password_pair",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "immediate_booking_enabled",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "online_status",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "property_type",
                table: "user_addresses");

            migrationBuilder.DropColumn(
                name: "minimum_hours",
                table: "services");

            migrationBuilder.DropColumn(
                name: "property_type",
                table: "services");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "booking_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "type",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "knowledge_documents");

            migrationBuilder.DropColumn(
                name: "actual_end_time",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "actual_start_time",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "booking_type",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "discount_amount",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "scheduled_end_time",
                table: "bookings");

            migrationBuilder.RenameIndex(
                name: "idx_user_addresses_user",
                table: "user_addresses",
                newName: "idx_user_addresses_user_id");

            migrationBuilder.RenameIndex(
                name: "idx_reviews_reviewee",
                table: "reviews",
                newName: "IX_reviews_reviewee_id");

            migrationBuilder.RenameIndex(
                name: "idx_reviews_booking",
                table: "reviews",
                newName: "idx_reviews_booking_id");

            migrationBuilder.RenameColumn(
                name: "token_hash",
                table: "refresh_tokens",
                newName: "token");

            migrationBuilder.RenameColumn(
                name: "replaced_by_token_hash",
                table: "refresh_tokens",
                newName: "replaced_by_token");

            migrationBuilder.RenameIndex(
                name: "refresh_tokens_token_hash_key",
                table: "refresh_tokens",
                newName: "refresh_tokens_token_key");

            migrationBuilder.RenameIndex(
                name: "idx_refresh_tokens_token_hash",
                table: "refresh_tokens",
                newName: "idx_refresh_tokens_token");

            migrationBuilder.RenameIndex(
                name: "idx_refresh_tokens_account",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_account_id");

            migrationBuilder.RenameIndex(
                name: "idx_payments_booking",
                table: "payments",
                newName: "idx_payments_booking_id");

            migrationBuilder.RenameColumn(
                name: "scheduled_start_time",
                table: "bookings",
                newName: "scheduled_time");

            migrationBuilder.RenameIndex(
                name: "idx_bookings_worker",
                table: "bookings",
                newName: "idx_bookings_worker_id");

            migrationBuilder.RenameIndex(
                name: "idx_bookings_client",
                table: "bookings",
                newName: "idx_bookings_client_id");

            migrationBuilder.RenameIndex(
                name: "idx_booking_logs_booking",
                table: "booking_status_logs",
                newName: "idx_booking_logs_booking_id");

            migrationBuilder.RenameIndex(
                name: "idx_ai_messages_conversation",
                table: "ai_messages",
                newName: "idx_ai_messages_conv");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:account_status", "active,banned,pending_verification")
                .Annotation("Npgsql:Enum:ai_sender_type", "user,ai")
                .Annotation("Npgsql:Enum:booking_status", "pending,accepted,in_progress,completed,cancelled")
                .Annotation("Npgsql:Enum:deploy_status_type", "success,failed,in_progress")
                .Annotation("Npgsql:Enum:log_level_type", "info,warning,error,critical")
                .Annotation("Npgsql:Enum:payment_method", "cash,momo,vnpay,zalopay,bank_transfer")
                .Annotation("Npgsql:Enum:payment_status", "pending,success,failed,refunded")
                .Annotation("Npgsql:Enum:service_unit_type", "hour,square_meter,package")
                .Annotation("Npgsql:Enum:user_role", "client,worker,admin")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .OldAnnotation("Npgsql:Enum:account_status", "active,banned,pending_verification")
                .OldAnnotation("Npgsql:Enum:ai_sender_type", "user,ai")
                .OldAnnotation("Npgsql:Enum:availability_status", "available,blocked,booked")
                .OldAnnotation("Npgsql:Enum:booking_status", "pending_payment,paid_pending_worker,accepted,reschedule_requested,in_progress,completed,cancelled,refunded")
                .OldAnnotation("Npgsql:Enum:booking_type", "scheduled,immediate")
                .OldAnnotation("Npgsql:Enum:cleanliness_level", "clean,light,medium,heavy")
                .OldAnnotation("Npgsql:Enum:notification_type", "booking,payment,schedule,system,ai")
                .OldAnnotation("Npgsql:Enum:payment_method", "cash,mo_mo,vn_pay,zalo_pay,bank_transfer")
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

            migrationBuilder.AddColumn<int>(
                name: "completed_jobs",
                table: "worker_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "identity_card_number",
                table: "worker_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                table: "services",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<decimal>(
                name: "extra_fee",
                table: "bookings",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "duration_hours",
                table: "bookings",
                type: "integer",
                nullable: false,
                defaultValue: 2,
                oldClrType: typeof(decimal),
                oldType: "numeric(4,2)",
                oldPrecision: 4,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "cancel_reason",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity",
                table: "bookings",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValueSql: "1");

            migrationBuilder.AlterColumn<int>(
                name: "latency_ms",
                table: "ai_inference_logs",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "model_id",
                table: "ai_inference_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ai_models",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    github_url = table.Column<string>(type: "text", nullable: true),
                    huggingface_url = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    model_name = table.Column<string>(type: "text", nullable: false),
                    model_version = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ai_models_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_recommendations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    reason = table.Column<string>(type: "text", nullable: true),
                    score = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ai_recommendations_pkey", x => x.id);
                    table.ForeignKey(
                        name: "ai_recommendations_booking_id_fkey",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "ai_recommendations_worker_id_fkey",
                        column: x => x.worker_id,
                        principalTable: "worker_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_training_data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    answer = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    question = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ai_training_data_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "deployment_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    environment = table.Column<string>(type: "text", nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    version = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("deployment_logs_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_embeddings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    embedding = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("document_embeddings_pkey", x => x.id);
                    table.ForeignKey(
                        name: "document_embeddings_document_id_fkey",
                        column: x => x.document_id,
                        principalTable: "knowledge_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "login_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fail_reason = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    login_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    user_agent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("login_history_pkey", x => x.id);
                    table.ForeignKey(
                        name: "login_history_account_id_fkey",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "otp_verifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    otp_code = table.Column<string>(type: "text", nullable: false),
                    purpose = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("otp_verifications_pkey", x => x.id);
                    table.ForeignKey(
                        name: "otp_verifications_account_id_fkey",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    icon_url = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("service_categories_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    message = table.Column<string>(type: "text", nullable: false),
                    service_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("system_logs_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "worker_skills",
                columns: table => new
                {
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    experience_months = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("worker_skills_pkey", x => new { x.worker_id, x.service_id });
                    table.ForeignKey(
                        name: "worker_skills_service_id_fkey",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "worker_skills_worker_id_fkey",
                        column: x => x.worker_id,
                        principalTable: "worker_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "worker_profiles_identity_card_number_key",
                table: "worker_profiles",
                column: "identity_card_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_services_category_id",
                table: "services",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_user_unread",
                table: "notifications",
                column: "user_id",
                filter: "(is_read = false)");

            migrationBuilder.CreateIndex(
                name: "idx_bookings_scheduled_time",
                table: "bookings",
                column: "scheduled_time");

            migrationBuilder.CreateIndex(
                name: "IX_ai_inference_logs_model_id",
                table: "ai_inference_logs",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "idx_ai_recs_booking",
                table: "ai_recommendations",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_recommendations_worker_id",
                table: "ai_recommendations",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_embeddings_document_id",
                table: "document_embeddings",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "idx_login_history_account",
                table: "login_history",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "idx_otp_account_purpose",
                table: "otp_verifications",
                columns: new[] { "account_id", "purpose" },
                filter: "(is_used = false)");

            migrationBuilder.CreateIndex(
                name: "service_categories_name_key",
                table: "service_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_worker_skills_service_id",
                table: "worker_skills",
                column: "service_id");

            migrationBuilder.AddForeignKey(
                name: "ai_conversations_user_id_fkey",
                table: "ai_conversations",
                column: "user_id",
                principalTable: "accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "ai_inference_logs_model_id_fkey",
                table: "ai_inference_logs",
                column: "model_id",
                principalTable: "ai_models",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "services_category_id_fkey",
                table: "services",
                column: "category_id",
                principalTable: "service_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
