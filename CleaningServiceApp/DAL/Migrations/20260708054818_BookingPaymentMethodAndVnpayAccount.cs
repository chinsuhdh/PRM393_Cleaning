using Cleaning.DAL.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleaning.DAL.Migrations
{
    /// <inheritdoc />
    public partial class BookingPaymentMethodAndVnpayAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<PaymentMethod>(
                name: "payment_method",
                table: "bookings",
                type: "payment_method",
                nullable: false,
                defaultValue: PaymentMethod.Cash);

            migrationBuilder.AddColumn<string>(
                name: "vnpay_account",
                table: "accounts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payment_method",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "vnpay_account",
                table: "accounts");
        }
    }
}
