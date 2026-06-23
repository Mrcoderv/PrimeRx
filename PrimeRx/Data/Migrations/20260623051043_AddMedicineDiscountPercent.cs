using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeRx.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicineDiscountPercent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new DiscountPercent column to SaleItems
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "SaleItems",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Add new DiscountAmount column to SaleItems
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "SaleItems",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Migrate data: copy DiscountPerItem to DiscountAmount
            migrationBuilder.Sql(
                @"UPDATE ""SaleItems"" 
                  SET ""DiscountAmount"" = ""DiscountPerItem"" 
                  WHERE ""DiscountPerItem"" > 0");

            // Add DiscountPercent column to Medicines
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "Medicines",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Drop old DiscountPerItem column from SaleItems
            migrationBuilder.DropColumn(
                name: "DiscountPerItem",
                table: "SaleItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add back DiscountPerItem column
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPerItem",
                table: "SaleItems",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Migrate data back: copy DiscountAmount to DiscountPerItem
            migrationBuilder.Sql(
                @"UPDATE ""SaleItems"" 
                  SET ""DiscountPerItem"" = ""DiscountAmount"" 
                  WHERE ""DiscountAmount"" > 0");

            // Drop new columns
            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "Medicines");
        }
    }
}
