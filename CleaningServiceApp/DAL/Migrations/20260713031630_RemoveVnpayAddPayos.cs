using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleaning.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVnpayAddPayos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "vnpay_account",
                table: "accounts");

            migrationBuilder.Sql("ALTER TYPE payment_method RENAME VALUE 'vnpay' TO 'payos';");

            migrationBuilder.AddColumn<long>(
                name: "payos_order_code",
                table: "payments",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_payos_order_code",
                table: "payments",
                column: "payos_order_code",
                unique: true,
                filter: "payos_order_code IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_payos_order_code",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "payos_order_code",
                table: "payments");

            migrationBuilder.Sql("ALTER TYPE payment_method RENAME VALUE 'payos' TO 'vnpay';");

            migrationBuilder.AddColumn<string>(
                name: "vnpay_account",
                table: "accounts",
                type: "text",
                nullable: true);
        }
    }
}
