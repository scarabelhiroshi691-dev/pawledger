using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetShop.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    OperatorUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Detail = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Barcode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    SalePrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    StockQuantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    LowStockThreshold = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    StandardPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    IsGroomer = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemberCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CardNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PrincipalBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    BonusBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberCards_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Species = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Breed = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Sex = table.Column<int>(type: "INTEGER", nullable: false),
                    Birthday = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    WeightKg = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    HealthNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    VaccineDueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pets_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RetailOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AmountDue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetailOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityChange = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    QuantityAfter = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OperatorUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LedgerTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemberCardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Bucket = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OriginalDocumentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RequestId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OperatorUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LedgerTransactions_MemberCards_MemberCardId",
                        column: x => x.MemberCardId,
                        principalTable: "MemberCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentType = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemberCardId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PrincipalPaid = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    BonusPaid = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ExternalPaid = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ExternalMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    PrincipalRefunded = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    BonusRefunded = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ExternalRefunded = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    RequestId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OperatorUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_MemberCards_MemberCardId",
                        column: x => x.MemberCardId,
                        principalTable: "MemberCards",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RechargeOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemberCardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrincipalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    BonusAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OperatorUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    PrincipalRefunded = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    BonusReversed = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ReversalRequestId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReversedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RechargeOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RechargeOrders_MemberCards_MemberCardId",
                        column: x => x.MemberCardId,
                        principalTable: "MemberCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroomingOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestedGroomerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ActualGroomerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ListPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AmountDue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroomingOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroomingOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroomingOrders_Pets_PetId",
                        column: x => x.PetId,
                        principalTable: "Pets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroomingOrders_ServiceItems_ServiceItemId",
                        column: x => x.ServiceItemId,
                        principalTable: "ServiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroomingOrders_StaffMembers_ActualGroomerId",
                        column: x => x.ActualGroomerId,
                        principalTable: "StaffMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetailOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RetailOrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetailOrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RetailOrderItems_RetailOrders_RetailOrderId",
                        column: x => x.RetailOrderId,
                        principalTable: "RetailOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRefunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrincipalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    BonusAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ExternalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    RequestId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OperatorUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRefunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_Payments_PaymentRecordId",
                        column: x => x.PaymentRecordId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Phone",
                table: "Customers",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_GroomingOrders_ActualGroomerId",
                table: "GroomingOrders",
                column: "ActualGroomerId");

            migrationBuilder.CreateIndex(
                name: "IX_GroomingOrders_CustomerId",
                table: "GroomingOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_GroomingOrders_OrderNumber",
                table: "GroomingOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroomingOrders_PetId",
                table: "GroomingOrders",
                column: "PetId");

            migrationBuilder.CreateIndex(
                name: "IX_GroomingOrders_ScheduledAt",
                table: "GroomingOrders",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_GroomingOrders_ServiceItemId",
                table: "GroomingOrders",
                column: "ServiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ProductId_CreatedAt",
                table: "InventoryTransactions",
                columns: new[] { "ProductId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerTransactions_MemberCardId_CreatedAt",
                table: "LedgerTransactions",
                columns: new[] { "MemberCardId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerTransactions_RequestId",
                table: "LedgerTransactions",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberCards_CardNumber",
                table: "MemberCards",
                column: "CardNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberCards_CustomerId",
                table: "MemberCards",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_PaymentRecordId",
                table: "PaymentRefunds",
                column: "PaymentRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_RequestId",
                table: "PaymentRefunds",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_DocumentType_DocumentId",
                table: "Payments",
                columns: new[] { "DocumentType", "DocumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MemberCardId",
                table: "Payments",
                column: "MemberCardId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RequestId",
                table: "Payments",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pets_CustomerId",
                table: "Pets",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                table: "Products",
                column: "Sku");

            migrationBuilder.CreateIndex(
                name: "IX_RechargeOrders_MemberCardId",
                table: "RechargeOrders",
                column: "MemberCardId");

            migrationBuilder.CreateIndex(
                name: "IX_RechargeOrders_RequestId",
                table: "RechargeOrders",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RetailOrderItems_ProductId",
                table: "RetailOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailOrderItems_RetailOrderId",
                table: "RetailOrderItems",
                column: "RetailOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailOrders_CustomerId",
                table: "RetailOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailOrders_OrderNumber",
                table: "RetailOrders",
                column: "OrderNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "GroomingOrders");

            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "LedgerTransactions");

            migrationBuilder.DropTable(
                name: "PaymentRefunds");

            migrationBuilder.DropTable(
                name: "RechargeOrders");

            migrationBuilder.DropTable(
                name: "RetailOrderItems");

            migrationBuilder.DropTable(
                name: "Pets");

            migrationBuilder.DropTable(
                name: "ServiceItems");

            migrationBuilder.DropTable(
                name: "StaffMembers");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "RetailOrders");

            migrationBuilder.DropTable(
                name: "MemberCards");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
