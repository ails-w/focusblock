# Handoff — Continuidad

> ⚠️ **ESTADO MUTABLE.** Se SOBREESCRIBE al cerrar sesión. No es historial.
> Historial por fase → `docs/progress-log/`. Conceptos → `docs/learning/`.
> Aprendizajes y decisiones persistentes → Engram (memoria).

**Última actualización**: 2026-09-04

## Estado actual

| | |
|---|---|
| **Fase activa** | Fase 2 — Configuración |
| **Última completada** | Fase 1 — Esqueleto TUI ✅ |
| **Progreso** | Fases 0 y 1 completadas · Fase 2 sin iniciar · 5 commits de Fase 1 sin pushear |

## Próximo paso

Fase 2 — Feature 2.1: escribir test `AppConfig_DefaultValues_AreCorrect` (RED), crear proyecto `FocusBlock.Contracts/` con modelos `AppConfig`, `BlockRuleConfig`, `SecurityConfig` (GREEN).

## Decisiones pendientes

- Verificar si el driver ANSI de Terminal.Gui ya está estable en una versión futura (para quitar el workaround del driver DOTNET si conviene).
- Pushear los 5 commits de la Fase 1 a `dev` y decidir qué hacer con la PR #1 (docs).

## Riesgos activos / Gotchas

- ⚠️ **Driver ANSI de Terminal.Gui roto en Linux** — la ventana se renderiza vacía sin subviews (issues upstream #4848 y #4374). Workaround: forzar `app.Init(DriverRegistry.Names.DOTNET)`.
- ⚠️ **API por instancia de Terminal.Gui v2** — la API estática legacy de v1 no compila contra v2.4.17 (CS0246/CS0117). Usar `Application.Create()` → `IApplication`.
- ⚠️ **La doc oficial de Terminal.Gui a veces miente** — verificar el API contra la DLL (`Terminal.Gui.xml`) antes de codificar (ej: `MenuItem(string, Action)` no existe).
- ⚠️ **`.slnx` en .NET 10** — `dotnet new sln` genera el formato XML nuevo, no el `.sln` clásico.
- ℹ️ **Case-sensitivity** — las propiedades de librerías son PascalCase (`Y`, no `y`).

## Entorno

- SDK .NET 10.0.111 (Arch: `sudo pacman -S dotnet-sdk`).
- Build/test: `dotnet build` / `dotnet test`.
- Run TUI: `dotnet run --project src/FocusBlock.Tui`.
- TDD estricto: tests ANTES de implementar.
- Convención: código en inglés, docs en español.