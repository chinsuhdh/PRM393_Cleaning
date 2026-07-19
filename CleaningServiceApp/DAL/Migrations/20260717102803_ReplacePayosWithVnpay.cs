using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleaning.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePayosWithVnpay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_payos_order_code",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "payos_order_code",
                table: "payments");

            // Rename the enum label in place (not a real add/remove) so existing 'payos' rows survive as
            // 'vnpay' with no data loss — Npgsql throws if you try to add/drop enum labels via AlterDatabase.
            migrationBuilder.Sql("ALTER TYPE payment_method RENAME VALUE 'payos' TO 'vnpay';");

            migrationBuilder.AddColumn<string>(
                name: "vnp_txn_ref",
                table: "payments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_vnp_txn_ref",
                table: "payments",
                column: "vnp_txn_ref",
                unique: true,
                filter: "vnp_txn_ref IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_vnp_txn_ref",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "vnp_txn_ref",
                table: "payments");

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
    }
}
