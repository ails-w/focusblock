# ADR-005: Configuración en JSON

- **Estado**: Aceptado
- **Fecha**: 2026-09-02

## Contexto

Las reglas de bloqueo, horarios y seguridad necesitan un formato de configuración editable y versionable.

## Decisión

**JSON** con `System.Text.Json` (round-trip, converters si hace falta para `TimeOnly`).

## Alternativas consideradas

- **YAML**: requiere librería externa y parsing más complejo.
- **TOML**: menos común en .NET.

## Consecuencias

- Integrado en .NET, sin dependencias extra.
- Fácil de leer/editar y diff-able en git.

## Referencias

- Fase 2 (`docs/learning/phase-02-config.md`)