# ADR-005: Configuración en JSON

- **Estado**: Aceptado
- **Fecha**: 2026-09-02

## Contexto

Las reglas de bloqueo, horarios y seguridad necesitan un formato de configuración editable y versionable.

## Decisión

**JSON** con `System.Text.Json` (round-trip). En .NET 10 **no hace falta ningún converter para `TimeOnly`**: el soporte es nativo desde .NET 7, y el test de round-trip lo verificó (ver `docs/learning/phase-02-config.md`).

## Alternativas consideradas

- **YAML**: requiere librería externa y parsing más complejo.
- **TOML**: menos común en .NET.

## Consecuencias

- Integrado en .NET, sin dependencias extra.
- Fácil de leer/editar y diff-able en git.

## Referencias

- Fase 2 (`docs/learning/phase-02-config.md`)