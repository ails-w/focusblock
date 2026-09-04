# ADR-003: SQLite + Dapper

- **Estado**: Aceptado
- **Fecha**: 2026-09-02

## Contexto

El daemon necesita persistir métricas de bloqueo y uso (eventos, uso diario, intentos de bypass) en local.

## Decisión

**SQLite** (`Microsoft.Data.Sqlite`) + **Dapper** (micro-ORM).

## Alternativas consideradas

- **EF Core**: sobrepeso de migraciones y abstracción para un esquema simple.
- **ADO.NET crudo**: demasiado manual.

## Consecuencias

- Simple, rápido, sin ceremonia de migraciones (riesgo: si el esquema crece, adoptar FluentMigrator).
- SQL explícito y controlado.

## Referencias

- `docs/architecture.md` (Esquema SQLite) · `docs/development-plan.md`