using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleaning.DAL.Migrations
{
    /// <inheritdoc />
    public partial class BookingCancellationOneToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "booking_cancellations_booking_id_key",
                table: "booking_cancellations");

            migrationBuilder.CreateIndex(
                name: "booking_cancellations_booking_id_key",
                table: "booking_cancellations",
                column: "booking_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "booking_cancellations_booking_id_key",
                table: "booking_cancellations");

            migrationBuilder.CreateIndex(
                name: "booking_cancellations_booking_id_key",
                table: "booking_cancellations",
                column: "booking_id",
                unique: true);
        }
    }
}
