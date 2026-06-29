using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeRx.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixExpenseAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS Expenses_new (
                    Id INTEGER NOT NULL CONSTRAINT PK_Expenses PRIMARY KEY AUTOINCREMENT,
                    Description TEXT NOT NULL,
                    Amount REAL NOT NULL,
                    Category TEXT NOT NULL,
                    ExpenseDate TEXT NOT NULL,
                    Notes TEXT NULL,
                    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                    CreatedBy TEXT NULL,
                    LastModifiedAt TEXT NULL,
                    LastModifiedBy TEXT NULL
                );
                INSERT OR IGNORE INTO Expenses_new SELECT Id, Description, Amount, Category, ExpenseDate, Notes,
                    COALESCE(CreatedAt, datetime('now')), CreatedBy, LastModifiedAt, LastModifiedBy FROM Expenses;
                DROP TABLE Expenses;
                ALTER TABLE Expenses_new RENAME TO Expenses;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
