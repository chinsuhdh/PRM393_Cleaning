using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleaning.DAL.Migrations
{
    /// <inheritdoc />
    public partial class WorkerPayoutFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "payout_bank_account_name",
                table: "worker_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payout_bank_account_number",
                table: "worker_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payout_bank_bin",
                table: "worker_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payout_failure_reason",
                table: "worker_earnings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payout_id",
                table: "worker_earnings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payout_bank_account_name",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "payout_bank_account_number",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "payout_bank_bin",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "payout_failure_reason",
                table: "worker_earnings");

            migrationBuilder.DropColumn(
                name: "payout_id",
                table: "worker_earnings");
        }
    }
}
