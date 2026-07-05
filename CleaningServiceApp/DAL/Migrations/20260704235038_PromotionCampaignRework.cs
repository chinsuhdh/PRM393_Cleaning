using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleaning.DAL.Migrations
{
    /// <inheritdoc />
    public partial class PromotionCampaignRework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Old code-based promos have no service target and can't survive the NOT NULL
            // service_id FK below (the scaffolded Guid.Empty default would violate it).
            // The code/quota promo feature is cut, so existing rows are deleted outright.
            migrationBuilder.Sql("DELETE FROM promotions;");

            migrationBuilder.DropTable(
                name: "promotion_redemptions");

            migrationBuilder.DropIndex(
                name: "IX_promotions_code",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "code",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "conditions",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "maximum_discount_amount",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "minimum_booking_amount",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "per_user_quota",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "redeemed_count",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "total_quota",
                table: "promotions");

            migrationBuilder.AddColumn<string>(
                name: "banner_image_url",
                table: "promotions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "banner_title",
                table: "promotions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "service_id",
                table: "promotions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_promotions_service_id_starts_at_ends_at",
                table: "promotions",
                columns: new[] { "service_id", "starts_at", "ends_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_promotions_services_service_id",
                table: "promotions",
                column: "service_id",
                principalTable: "services",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_promotions_services_service_id",
                table: "promotions");

            migrationBuilder.DropIndex(
                name: "IX_promotions_service_id_starts_at_ends_at",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "banner_image_url",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "banner_title",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "service_id",
                table: "promotions");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "promotions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "conditions",
                table: "promotions",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<decimal>(
                name: "maximum_discount_amount",
                table: "promotions",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "minimum_booking_amount",
                table: "promotions",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "per_user_quota",
                table: "promotions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "redeemed_count",
                table: "promotions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "total_quota",
                table: "promotions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "promotion_redemptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    discount_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_redemptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_promotion_redemptions_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_redemptions_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_redemptions_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_promotions_code",
                table: "promotions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promotion_redemptions_booking_id",
                table: "promotion_redemptions",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promotion_redemptions_promotion_id_user_id",
                table: "promotion_redemptions",
                columns: new[] { "promotion_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_redemptions_user_id",
                table: "promotion_redemptions",
                column: "user_id");
        }
    }
}
