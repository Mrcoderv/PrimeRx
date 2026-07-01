using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeRx.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase1CustomerMedicineFormModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create Customers table
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TotalSpent = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    LoyaltyPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    LastPurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            // Create indices on Customers
            migrationBuilder.CreateIndex(
                name: "IX_Customers_Name",
                table: "Customers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Phone",
                table: "Customers",
                column: "Phone");

            // Create MedicineForms table
            migrationBuilder.CreateTable(
                name: "MedicineForms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MedicineId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Strength = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    MRP = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    LowStockThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicineForms_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicineForms_MedicineId_FormType",
                table: "MedicineForms",
                columns: new[] { "MedicineId", "FormType" });

            // Add MedicineFormId to InventoryBatches
            migrationBuilder.AddColumn<int>(
                name: "MedicineFormId",
                table: "InventoryBatches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Add index on InventoryBatches (MedicineId, ExpiryDate)
            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_MedicineId_ExpiryDate",
                table: "InventoryBatches",
                columns: new[] { "MedicineId", "ExpiryDate" });

            // Add CustomerId and BatchId to Bills
            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "Bills",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bills_BillDate",
                table: "Bills",
                column: "BillDate");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_CustomerId",
                table: "Bills",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_Customers_CustomerId",
                table: "Bills",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Add BatchId to SaleItems
            migrationBuilder.AddColumn<int>(
                name: "BatchId",
                table: "SaleItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_BatchId",
                table: "SaleItems",
                column: "BatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_InventoryBatches_BatchId",
                table: "SaleItems",
                column: "BatchId",
                principalTable: "InventoryBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Add Medicine FK to SaleItems if not already there
            migrationBuilder.AddColumn<int>(
                name: "MedicineId",
                table: "SaleItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_MedicineId",
                table: "SaleItems",
                column: "MedicineId");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_Medicines_MedicineId",
                table: "SaleItems",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Add foreign key for InventoryBatch.MedicineFormId
            migrationBuilder.AddForeignKey(
                name: "FK_InventoryBatches_MedicineForms_MedicineFormId",
                table: "InventoryBatches",
                column: "MedicineFormId",
                principalTable: "MedicineForms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign keys
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_Customers_CustomerId",
                table: "Bills");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_InventoryBatches_BatchId",
                table: "SaleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_Medicines_MedicineId",
                table: "SaleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryBatches_MedicineForms_MedicineFormId",
                table: "InventoryBatches");

            // Drop tables and columns
            migrationBuilder.DropTable(name: "Customers");
            migrationBuilder.DropTable(name: "MedicineForms");

            migrationBuilder.DropIndex(name: "IX_Bills_BillDate", table: "Bills");
            migrationBuilder.DropIndex(name: "IX_Bills_CustomerId", table: "Bills");
            migrationBuilder.DropIndex(name: "IX_InventoryBatches_MedicineId_ExpiryDate", table: "InventoryBatches");
            migrationBuilder.DropIndex(name: "IX_SaleItems_BatchId", table: "SaleItems");
            migrationBuilder.DropIndex(name: "IX_SaleItems_MedicineId", table: "SaleItems");

            migrationBuilder.DropColumn(name: "CustomerId", table: "Bills");
            migrationBuilder.DropColumn(name: "MedicineFormId", table: "InventoryBatches");
            migrationBuilder.DropColumn(name: "BatchId", table: "SaleItems");
            migrationBuilder.DropColumn(name: "MedicineId", table: "SaleItems");
        }
    }
}
