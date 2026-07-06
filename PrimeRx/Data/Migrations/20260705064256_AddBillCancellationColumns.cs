using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeRx.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBillCancellationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "Bills",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Bills",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Bills",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Bills");
        }
    }
}
