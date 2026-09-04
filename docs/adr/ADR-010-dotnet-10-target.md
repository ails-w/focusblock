# ADR-010: Target .NET 10

- **Estado**: Aceptado
- **Fecha**: 2026-09-02

## Contexto

El plan original indicaba .NET 8, pero en el entorno solo está instalado el SDK **.NET 10.0.111** (C# 14).

## Decisión

Target **`net10.0`** para todos los proyectos (TUI y tests). La solución usa el formato `.slnx` que genera el SDK 10.

## Alternativas consideradas

- **Instalar SDK 8**: fiel al plan original, pero añade fricción de entorno sin ganancia de aprendizaje.
- **Multi-target**: complejidad innecesaria en un proyecto de aprendizaje.

## Consecuencias

- Sin fricción de instalación; mismo lenguaje C# moderno.
- `dotnet new sln` genera `.slnx` (XML) en lugar de `.sln`.
- Docs de stack actualizadas a .NET 10.

## Referencias

- `docs/progress-log/phase-00-setup.md` (decisión 1)