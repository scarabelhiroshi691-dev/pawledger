using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetShop.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupBuyLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupBuyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlatformOrderNumber = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    VoucherCodeSuffix = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    DealName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    CustomerName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PetName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    GroomerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    GrossAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ExpectedSettlementAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ActualSettlementAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    SettlementStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    RedeemedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SettledAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    OperatorUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    ReconciledByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupBuyRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupBuyRecords_StaffMembers_GroomerId",
                        column: x => x.GroomerId,
                        principalTable: "StaffMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupBuyRefunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroupBuyRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    OperatorUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupBuyRefunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupBuyRefunds_GroupBuyRecords_GroupBuyRecordId",
                        column: x => x.GroupBuyRecordId,
                        principalTable: "GroupBuyRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupBuyRecords_GroomerId",
                table: "GroupBuyRecords",
                column: "GroomerId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupBuyRecords_PlatformOrderNumber",
                table: "GroupBuyRecords",
                column: "PlatformOrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupBuyRecords_RedeemedAt",
                table: "GroupBuyRecords",
                column: "RedeemedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GroupBuyRefunds_CreatedAt",
                table: "GroupBuyRefunds",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GroupBuyRefunds_GroupBuyRecordId",
                table: "GroupBuyRefunds",
                column: "GroupBuyRecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupBuyRefunds");

            migrationBuilder.DropTable(
                name: "GroupBuyRecords");
        }
    }
}
