---
name: EF Core manual migrations
description: Hand-written EF Core migration files need specific attributes to be discovered by MigrateAsync at runtime.
---

EF Core's `MigrateAsync()` discovers migrations via reflection. A migration class without the right attributes is silently ignored — the log says "No migrations were applied. The database is already up to date."

**Rule:** Every hand-written migration `.cs` file must have both attributes on the class:

```csharp
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260630000001_AddPurchaseModule")]
public partial class AddPurchaseModule : Migration { ... }
```

**Why:** When you run `dotnet ef migrations add`, the tooling generates a Designer.cs with these attributes. Hand-written migrations skip the tooling, so the attributes must be placed directly on the migration class itself.

**How to apply:** Any time a new migration is written manually (without `dotnet ef migrations add`), add the two attributes shown above, matching the migration timestamp ID exactly.
