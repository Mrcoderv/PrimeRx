using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrimeRx.Data;

#nullable disable

namespace PrimeRx.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260701200000_AddAuditLog")]
    public partial class AddAuditLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: true),
                    OldValue = table.Column<string>(type: "TEXT", nullable: true),
                    NewValue = table.Column<string>(type: "TEXT", nullable: true),
                    IPAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AuditLogs");
        }
    }
}
