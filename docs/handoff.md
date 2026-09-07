# Handoff — Continuidad

> ⚠️ **ESTADO MUTABLE.** Se SOBREESCRIBE al cerrar sesión. No es historial.
> Historial por fase → `docs/progress-log/`. Conceptos → `docs/learning/`.
> Aprendizajes y decisiones persistentes → Engram (memoria).

**Última actualización**: 2026-09-07

## Estado actual

| | |
|---|---|
| **Fase activa** | Fase 3 — Daemon |
| **Última completada** | Fase 2 — Configuración ✅ |
| **Progreso** | Fases 0, 1 y 2 completadas · Fase 3 sin iniciar |

## Próximo paso

Fase 3 — Feature 3.1: escribir test `Worker_StartsAndRunsUntilCancelled` (RED), crear proyecto `FocusBlock.Daemon/` con `Worker.cs` (BackgroundService) (GREEN).

## Decisiones pendientes

- Verificar si el driver ANSI de Terminal.Gui ya está estable en una versión futura (para quitar el workaround del driver DOTNET si conviene).
- Revisar ADR-012 cuando se quieran las estrategias de bloqueo por uso (Racha/Tope) — requieren telemetría del daemon.

## Riesgos activos / Gotchas

- ⚠️ **Driver ANSI de Terminal.Gui roto en Linux** — la ventana se renderiza vacía sin subviews (issues upstream #4848 y #4374). Workaround: forzar `app.Init(DriverRegistry.Names.DOTNET)`.
- ⚠️ **API por instancia de Terminal.Gui v2** — la API estática legacy de v1 no compila contra v2.4.17 (CS0246/CS0117). Usar `Application.Create()` → `IApplication`.
- ⚠️ **La doc oficial de Terminal.Gui a veces miente** — verificar el API contra la DLL (`Terminal.Gui.xml`) antes de codificar.
- ⚠️ **`.slnx` en .NET 10** — `dotnet new sln` genera el formato XML nuevo, no el `.sln` clásico.
- ℹ️ **Case-sensitivity** — las propiedades de librerías son PascalCase (`Y`, no `y`).

## Entorno

- SDK .NET 10.0.111 (Arch: `sudo pacman -S dotnet-sdk`).
- Build/test: `dotnet build` / `dotnet test`.
- Run TUI: `dotnet run --project src/FocusBlock.Tui`.
- TDD estricto: tests ANTES de implementar.
- Convención: código en inglés, docs en español (bloques de código con `cs`).