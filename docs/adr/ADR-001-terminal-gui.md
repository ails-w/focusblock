# ADR-001: Terminal.Gui v2 como framework TUI

- **Estado**: Aceptado
- **Fecha**: 2026-09-02

## Contexto

La TUI de FocusBlock necesita un framework con widget set completo (menús, listas, formularios, charts) y nativo .NET.

## Decisión

Usar **Terminal.Gui v2** (2.4.17) con su API por instancia (`IApplication`). La API estática legacy de v1 está obsoleta y no compila contra v2.

## Alternativas consideradas

- **Spectre.Console**: excelente para output, pero no es un framework de TUI interactiva (sin widgets de navegación).
- **GUI (Avalonia)**: fuera de alcance (el proyecto es TUI por decisión de visión).

## Consecuencias

- Widget set completo y tests unitarios posibles sobre vistas.
- Hay que conocer la API v2 (sub-namespaces, propiedades en lugar de constructores).
- El driver ANSI por defecto tiene bugs en Linux → ver `ADR-011`.

## Referencias

- `docs/architecture.md` · `docs/learning/phase-00-setup.md`