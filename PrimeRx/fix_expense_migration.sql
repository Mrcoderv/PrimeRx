-- Remove the problematic migration from history
DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628214006_AddExpenseAuditColumns';
