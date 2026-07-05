using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleaning.DAL.Migrations
{
    /// <inheritdoc />
    public partial class WorkerLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The immediate-booking view depends on immediate_booking_enabled, so PostgreSQL would
            // refuse the column drop. The EligibilityViews migration recreates both views right after.
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_online_workers_for_immediate_booking;");

            migrationBuilder.DropColumn(
                name: "immediate_booking_enabled",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "notification_preferences",
                table: "accounts");

            migrationBuilder.AddColumn<DateTime>(
                name: "suspended_at",
                table: "worker_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "onboarding_completed_at",
                table: "profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "worker_hidden_bookings",
                columns: table => new
                {
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_hidden_bookings", x => new { x.worker_id, x.booking_id });
                    table.ForeignKey(
                        name: "FK_worker_hidden_bookings_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_worker_hidden_bookings_worker_profiles_worker_id",
                        column: x => x.worker_id,
                        principalTable: "worker_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_worker_hidden_bookings_booking_id",
                table: "worker_hidden_bookings",
                column: "booking_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "worker_hidden_bookings");

            migrationBuilder.DropColumn(
                name: "suspended_at",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "onboarding_completed_at",
                table: "profiles");

            migrationBuilder.AddColumn<bool>(
                name: "immediate_booking_enabled",
                table: "worker_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "notification_preferences",
                table: "accounts",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            // Restore the original view dropped in Up().
            migrationBuilder.Sql("""
                CREATE VIEW v_online_workers_for_immediate_booking AS
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
        }
    }
}
