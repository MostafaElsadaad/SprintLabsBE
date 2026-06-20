---
name: sprintlabs-efcore-mysql
description: Use for SprintLabs database work involving EF Core entities, MySQL relationships, indexes, migrations, seed data, and DbContext configuration.
---

# SprintLabs EF Core MySQL Skill

Use this skill when a task changes schema, entities, relationships, constraints, indexes, or seed data.

## First inspect
- `Infrastructure/DataAccess/ApplicationDbContext.cs`
- `Domain/Models/`
- Existing migrations in `Infrastructure/Migrations/`
- Existing repositories in `Infrastructure/Repositories/`
- Existing seed classes in `Infrastructure/Seed/`

## Schema rules
1. Model only what the current story requires.
2. Add tenant/school/owner relationship for SaaS-owned data.
3. Configure required fields, max lengths, defaults, and indexes.
4. Use unique indexes for natural uniqueness only when product rules require it.
5. Use soft delete only if the epic explicitly requires recovery/audit behavior.
6. Keep JSON payloads as string/text unless the story needs DB querying inside JSON.
7. Do not change existing migration history manually unless fixing an uncommitted migration.
8. Generate a migration after model changes.

## MySQL guidance
- Be explicit with max lengths for indexed strings.
- Avoid giant unique indexes on long strings.
- Prefer `DateTime` fields used consistently with existing code.
- Add composite indexes for common filters, such as tenant + status, grade + assignment, tenant + createdAt.

## Commands
Common commands from repo root:

```bash
dotnet ef migrations add <MigrationName> --project Infrastructure --startup-project API
dotnet build SprintLabs.sln
```

If EF tools are unavailable, state the exact command that should be run locally.

## Checklist
- [ ] Entity exists in Domain.
- [ ] DbSet added when needed.
- [ ] Relationships configured.
- [ ] Indexes match expected query filters.
- [ ] Migration generated or command provided.
- [ ] No unrelated schema changes.
