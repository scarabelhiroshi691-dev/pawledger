using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetShop.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffAccountsCashShiftsAndExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentityUserId",
                table: "StaffMembers",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LoginEnabled",
                table: "StaffMembers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CashShifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OpeningCash = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    OpenedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    OpeningNote = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ClosedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    ExpectedCash = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    CountedCash = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Difference = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    ClosingNote = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashShifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreExpenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CashShiftId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    Payee = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    OperatorUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreExpenses_CashShifts_CashShiftId",
                        column: x => x.CashShiftId,
                        principalTable: "CashShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_IdentityUserId",
                table: "StaffMembers",
                column: "IdentityUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashShifts_OpenedAt",
                table: "CashShifts",
                column: "OpenedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StoreExpenses_CashShiftId",
                table: "StoreExpenses",
                column: "CashShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreExpenses_OccurredAt",
                table: "StoreExpenses",
                column: "OccurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreExpenses");

            migrationBuilder.DropTable(
                name: "CashShifts");

            migrationBuilder.DropIndex(
                name: "IX_StaffMembers_IdentityUserId",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "IdentityUserId",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "LoginEnabled",
                table: "StaffMembers");
        }
    }
}
