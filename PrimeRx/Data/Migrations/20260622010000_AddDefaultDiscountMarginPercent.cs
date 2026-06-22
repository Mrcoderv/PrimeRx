using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeRx.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultDiscountMarginPercent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultDiscountMarginPercent",
                table: "CompanyProfiles",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 20m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultDiscountMarginPercent",
                table: "CompanyProfiles");
        }
    }
}
