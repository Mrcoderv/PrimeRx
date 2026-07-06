using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeRx.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseItemConversionCharge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ConversionCharge",
                table: "PurchaseItems",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConversionCharge",
                table: "PurchaseItems");
        }
    }
}
