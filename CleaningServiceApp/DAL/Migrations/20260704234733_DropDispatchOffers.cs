using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleaning.DAL.Migrations
{
    /// <inheritdoc />
    public partial class DropDispatchOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_worker_offers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "booking_worker_offers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    rank_score = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_worker_offers", x => x.id);
                    table.ForeignKey(
                        name: "FK_booking_worker_offers_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_booking_worker_offers_worker_profiles_worker_id",
                        column: x => x.worker_id,
                        principalTable: "worker_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_worker_offers_booking_id",
                table: "booking_worker_offers",
                column: "booking_id",
                unique: true,
                filter: "status = 'accepted'");

            migrationBuilder.CreateIndex(
                name: "IX_booking_worker_offers_booking_id_worker_id",
                table: "booking_worker_offers",
                columns: new[] { "booking_id", "worker_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_worker_offers_worker_id",
                table: "booking_worker_offers",
                column: "worker_id");
        }
    }
}
