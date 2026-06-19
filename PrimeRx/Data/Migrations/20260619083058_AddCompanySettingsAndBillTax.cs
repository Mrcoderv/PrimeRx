using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeRx.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanySettingsAndBillTax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillFooterText",
                table: "CompanyProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillPrimaryColor",
                table: "CompanyProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BillTitle",
                table: "CompanyProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ShowGstinOnBill",
                table: "CompanyProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowPanOnBill",
                table: "CompanyProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TaxInclusive",
                table: "CompanyProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TaxLabel",
                table: "CompanyProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "CompanyProfiles",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "Bills",
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
                name: "BillFooterText",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "BillPrimaryColor",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "BillTitle",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "ShowGstinOnBill",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "ShowPanOnBill",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "TaxInclusive",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "TaxLabel",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "Bills");
        }
    }
}
