using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeRx.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicineMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MedicineMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GenericName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    BrandName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Form = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Strength = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    HSNCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    RackLocation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineMasters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicineMasters_BrandName",
                table: "MedicineMasters",
                column: "BrandName");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineMasters_GenericName",
                table: "MedicineMasters",
                column: "GenericName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicineMasters");
        }
    }
}
