using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleaning.DAL.Migrations
{
    /// <inheritdoc />
    public partial class PaymentsRework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 'momo'/'zalopay'/'bank_transfer' are removed from the C# enum with no sane automatic
            // remap for a real payment — fail loudly instead of silently rewriting money data.
            // Old refund statuses become 'failed' (no refunds in the product anymore).
            migrationBuilder.Sql("""
                DO $$
                DECLARE foreign_method_count int;
                BEGIN
                    SELECT count(*) INTO foreign_method_count
                    FROM payments WHERE method IN ('momo', 'zalopay', 'bank_transfer');
                    IF foreign_method_count > 0 THEN
                        RAISE EXCEPTION 'PaymentsRework: % payment(s) use momo/zalopay/bank_transfer — resolve them manually before migrating', foreign_method_count;
                    END IF;
                END $$;

                UPDATE payments SET status = 'failed' WHERE status IN ('refunded', 'partially_refunded');
                """);

            migrationBuilder.DropTable(
                name: "refunds");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:account_status", "active,banned,pending_verification")
                .Annotation("Npgsql:Enum:ai_sender_type", "user,ai")
                .Annotation("Npgsql:Enum:availability_status", "available,blocked,booked")
                .Annotation("Npgsql:Enum:booking_status", "pending_payment,accepted,reschedule_requested,in_progress,completed,cancelled,awaiting_worker,on_the_way")
                .Annotation("Npgsql:Enum:booking_type", "scheduled,immediate")
                .Annotation("Npgsql:Enum:cleanliness_level", "clean,light,medium,heavy")
                .Annotation("Npgsql:Enum:notification_type", "booking,payment,schedule,system,ai")
                .Annotation("Npgsql:Enum:payment_method", "cash,vnpay")
                .Annotation("Npgsql:Enum:payment_status", "pending,success,failed")
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
                .OldAnnotation("Npgsql:Enum:booking_status", "pending_payment,accepted,reschedule_requested,in_progress,completed,cancelled,awaiting_worker,on_the_way")
                .OldAnnotation("Npgsql:Enum:booking_type", "scheduled,immediate")
                .OldAnnotation("Npgsql:Enum:cleanliness_level", "clean,light,medium,heavy")
                .OldAnnotation("Npgsql:Enum:notification_type", "booking,payment,schedule,system,ai")
                // Npgsql can't emit SQL for enum label removals — the old lists for
                // payment_method/payment_status are hand-trimmed to match the new ones (no-op;
                // stale labels stay in the PG types). payout_status is genuinely dropped: the
                // whole type disappears from the model, which the generator does support.
                .OldAnnotation("Npgsql:Enum:payment_method", "cash,vnpay")
                .OldAnnotation("Npgsql:Enum:payment_status", "pending,success,failed")
                .OldAnnotation("Npgsql:Enum:payout_status", "pending,paid,failed")
                .OldAnnotation("Npgsql:Enum:photo_type", "before,after,issue,ai_reference")
                .OldAnnotation("Npgsql:Enum:property_type", "apartment,house")
                .OldAnnotation("Npgsql:Enum:reschedule_status", "pending,accepted,rejected,cancelled")
                .OldAnnotation("Npgsql:Enum:service_unit_type", "hour")
                .OldAnnotation("Npgsql:Enum:user_role", "client,worker,admin")
                .OldAnnotation("Npgsql:Enum:verification_purpose", "email_verification,phone_verification,password_reset")
                .OldAnnotation("Npgsql:Enum:worker_online_status", "offline,online,busy")
                .OldAnnotation("Npgsql:PostgresExtension:pgcrypto", ",,");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:account_status", "active,banned,pending_verification")
                .Annotation("Npgsql:Enum:ai_sender_type", "user,ai")
                .Annotation("Npgsql:Enum:availability_status", "available,blocked,booked")
                .Annotation("Npgsql:Enum:booking_status", "pending_payment,accepted,reschedule_requested,in_progress,completed,cancelled,awaiting_worker,on_the_way")
                .Annotation("Npgsql:Enum:booking_type", "scheduled,immediate")
                .Annotation("Npgsql:Enum:cleanliness_level", "clean,light,medium,heavy")
                .Annotation("Npgsql:Enum:notification_type", "booking,payment,schedule,system,ai")
                // Down: enum labels can't be re-removed/re-added meaningfully — pairs match
                // OldAnnotation (no-op) except payout_status, which is recreated on rollback.
                .Annotation("Npgsql:Enum:payment_method", "cash,vnpay")
                .Annotation("Npgsql:Enum:payment_status", "pending,success,failed")
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
                .OldAnnotation("Npgsql:Enum:booking_status", "pending_payment,accepted,reschedule_requested,in_progress,completed,cancelled,awaiting_worker,on_the_way")
                .OldAnnotation("Npgsql:Enum:booking_type", "scheduled,immediate")
                .OldAnnotation("Npgsql:Enum:cleanliness_level", "clean,light,medium,heavy")
                .OldAnnotation("Npgsql:Enum:notification_type", "booking,payment,schedule,system,ai")
                .OldAnnotation("Npgsql:Enum:payment_method", "cash,vnpay")
                .OldAnnotation("Npgsql:Enum:payment_status", "pending,success,failed")
                .OldAnnotation("Npgsql:Enum:photo_type", "before,after,issue,ai_reference")
                .OldAnnotation("Npgsql:Enum:property_type", "apartment,house")
                .OldAnnotation("Npgsql:Enum:reschedule_status", "pending,accepted,rejected,cancelled")
                .OldAnnotation("Npgsql:Enum:service_unit_type", "hour")
                .OldAnnotation("Npgsql:Enum:user_role", "client,worker,admin")
                .OldAnnotation("Npgsql:Enum:verification_purpose", "email_verification,phone_verification,password_reset")
                .OldAnnotation("Npgsql:Enum:worker_online_status", "offline,online,busy")
                .OldAnnotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "refunds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    idempotency_key = table.Column<string>(type: "text", nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    provider_refund_id = table.Column<string>(type: "text", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("refunds_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_refunds_accounts_requested_by",
                        column: x => x.requested_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "refunds_payment_id_fkey",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_refunds_idempotency_key",
                table: "refunds",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_payment_id",
                table: "refunds",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_requested_by",
                table: "refunds",
                column: "requested_by");
        }
    }
}
