using Cleaning.DAL.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleaning.DAL.Migrations
{
    /// <inheritdoc />
    public partial class CancellationRecordRework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cancellation_fee",
                table: "booking_cancellations");

            migrationBuilder.DropColumn(
                name: "refund_amount",
                table: "booking_cancellations");

            migrationBuilder.AddColumn<UserRole>(
                name: "actor_role",
                table: "booking_cancellations",
                type: "user_role",
                nullable: false,
                defaultValue: UserRole.Client);

            migrationBuilder.AddColumn<string>(
                name: "reason_code",
                table: "booking_cancellations",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_booking_cancellations_created_at",
                table: "booking_cancellations",
                column: "created_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_booking_cancellations_created_at",
                table: "booking_cancellations");

            migrationBuilder.DropColumn(
                name: "actor_role",
                table: "booking_cancellations");

            migrationBuilder.DropColumn(
                name: "reason_code",
                table: "booking_cancellations");

            migrationBuilder.AddColumn<decimal>(
                name: "cancellation_fee",
                table: "booking_cancellations",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "refund_amount",
                table: "booking_cancellations",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
