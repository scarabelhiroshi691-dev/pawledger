using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetShop.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMeituanVoucherIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeituanVoucherRedemptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VoucherCodeHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    VoucherCodeMasked = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    RequestId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DealId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DealName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    SalePrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    MarketPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ProviderMode = table.Column<int>(type: "INTEGER", nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    OperatorUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RedeemedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    RevokedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    RevokeReason = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    LastProviderMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeituanVoucherRedemptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeituanVoucherRedemptions_CreatedAt",
                table: "MeituanVoucherRedemptions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MeituanVoucherRedemptions_ProviderTransactionId",
                table: "MeituanVoucherRedemptions",
                column: "ProviderTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_MeituanVoucherRedemptions_RequestId",
                table: "MeituanVoucherRedemptions",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeituanVoucherRedemptions_VoucherCodeHash",
                table: "MeituanVoucherRedemptions",
                column: "VoucherCodeHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeituanVoucherRedemptions");
        }
    }
}
