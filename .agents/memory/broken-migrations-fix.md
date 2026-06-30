---
name: Broken migrations fix pattern
description: How to fix conflicting/broken EF Core SQLite migrations in PrimeRx during Replit import.
---

**Rule:** When a migration tries to add a column that already exists (or references a column that doesn't exist), fix it in-place by editing the migration file to remove the conflicting operations, keeping only what is genuinely new.

**Why:** SQLite does not support transactional DDL for all operations, so partially-applied migrations can leave the DB in a broken state. The safest recovery when there is no production data is to delete the `.db` file and let all migrations re-run from scratch.

**How to apply:**
1. Identify the duplicate/wrong column in the error (`SQLite Error 1: 'duplicate column name'` or `'no such column'`).
2. Edit the broken migration `.cs` — remove the conflicting `AddColumn` or fix the `INSERT SELECT` to use correct column names.
3. If no data needs preserving, delete `Data/primerx.db`, `Data/primerx.db-wal`, `Data/primerx.db-shm` and restart.
4. Confirm all migrations apply cleanly in the startup logs.

**Known fixed migrations in this project:**
- `20260623051043_AddMedicineDiscountPercent` — was trying to re-add `DiscountPercent` to `SaleItems` (already renamed there by the prior migration). Fixed to only add `DiscountPercent` to `Medicines`.
- `20260629071649_FixExpenseAuditColumns` — raw SQL referenced `Description` column which doesn't exist (actual column is `Reason`). Fixed to a no-op since the prior migration already added the audit columns correctly.
