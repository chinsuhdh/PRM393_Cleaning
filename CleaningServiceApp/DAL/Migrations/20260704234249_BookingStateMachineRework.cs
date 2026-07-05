using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleaning.DAL.Migrations
{
    /// <inheritdoc />
    public partial class BookingStateMachineRework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Added outside the migrations transaction (suppressTransaction): PostgreSQL forbids
            // using a new enum label inside the transaction that added it, and EF 9 runs all
            // pending migrations in one transaction — the EligibilityViews migration later in the
            // same run references 'on_the_way'. The AlterDatabase below is annotation-only (its
            // enum lists are hand-matched) so this Sql() is the only enum DDL.
            migrationBuilder.Sql(
                "ALTER TYPE booking_status ADD VALUE IF NOT EXISTS 'on_the_way';",
                suppressTransaction: true);

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:account_status", "active,banned,pending_verification")
                .Annotation("Npgsql:Enum:ai_sender_type", "user,ai")
                .Annotation("Npgsql:Enum:availability_status", "available,blocked,booked")
                .Annotation("Npgsql:Enum:booking_status", "pending_payment,accepted,reschedule_requested,in_progress,completed,cancelled,awaiting_worker,on_the_way")
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
                // Npgsql can't emit SQL for enum label removals, so the old list below is
                // hand-matched to the new one (no-op — 'on_the_way' is added by the raw Sql()
                // above). The retired 'paid_pending_worker'/'refunded' labels stay in the PG type.
                .OldAnnotation("Npgsql:Enum:booking_status", "pending_payment,accepted,reschedule_requested,in_progress,completed,cancelled,awaiting_worker,on_the_way")
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

            // Pay-after-job rework: 'paid_pending_worker' and 'refunded' no longer exist in the C# enum,
            // and 'pending_payment' now means post-job payment. Remap existing rows before the app reads
            // them (Npgsql throws on unmapped labels). The stale labels stay in the PG type on purpose —
            // dropping an enum value would require a full type rebuild for no benefit.
            migrationBuilder.Sql("""
                UPDATE bookings SET status = 'awaiting_worker' WHERE status = 'paid_pending_worker';
                UPDATE bookings SET status = 'cancelled' WHERE status IN ('refunded', 'pending_payment');
                UPDATE booking_status_logs SET old_status = 'awaiting_worker' WHERE old_status = 'paid_pending_worker';
                UPDATE booking_status_logs SET new_status = 'awaiting_worker' WHERE new_status = 'paid_pending_worker';
                UPDATE booking_status_logs SET old_status = 'cancelled' WHERE old_status IN ('refunded', 'pending_payment');
                UPDATE booking_status_logs SET new_status = 'cancelled' WHERE new_status IN ('refunded', 'pending_payment');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:account_status", "active,banned,pending_verification")
                .Annotation("Npgsql:Enum:ai_sender_type", "user,ai")
                .Annotation("Npgsql:Enum:availability_status", "available,blocked,booked")
                // Down is enum-wise a no-op: PG labels can't be dropped, and the retired labels
                // were never removed from the type. The pair below matches OldAnnotation exactly.
                .Annotation("Npgsql:Enum:booking_status", "pending_payment,accepted,reschedule_requested,in_progress,completed,cancelled,awaiting_worker,on_the_way")
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
                .OldAnnotation("Npgsql:Enum:booking_status", "pending_payment,accepted,reschedule_requested,in_progress,completed,cancelled,awaiting_worker,on_the_way")
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
        }
    }
}
