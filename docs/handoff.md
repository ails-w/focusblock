# Handoff — Continuidad

> ⚠️ **ESTADO MUTABLE.** Se SOBREESCRIBE al cerrar sesión. No es historial.
> Historial por fase → `docs/progress-log/`. Conceptos → `docs/learning/`.
> Aprendizajes y decisiones persistentes → Engram (memoria).

**Última actualización**: 2026-09-15

## Estado actual

| | |
|---|---|
| **Fase activa** | Fase 3 — Daemon (en curso) |
| **Última completada** | Fase 2 — Configuración ✅ |
| **Progreso** | Fases 0, 1 y 2 ✅ · Fase 3: 3.1–3.4 implementadas y testeadas (32 tests) · falta el cableado end-to-end del daemon |

## Próximo paso

Cerrar Fase 3: escribir `docs/learning/phase-03-daemon.md` y `docs/progress-log/phase-03-daemon.md`. Decidir el cableado end-to-end (Worker + ProcessMonitor + BlockEnforcer + IpcServer) — depende del BlockEngine de Fase 4.

## Decisiones pendientes

- Verificar si el driver ANSI de Terminal.Gui ya está estable en una versión futura (para quitar el workaround del driver DOTNET si conviene).
- Revisar ADR-012 cuando se quieran las estrategias de bloqueo por uso (Racha/Tope) — requieren telemetría del daemon.
- Argon2id: los parámetros (`MemorySize`, `Iterations`) NO se guardan con el hash, así que subirlos invalida las huellas existentes. Evaluar formato PHC (`$argon2id$...`) antes de Fase 5 (anti-bypass).

## Riesgos activos / Gotchas

- ⚠️ **Driver ANSI de Terminal.Gui roto en Linux** — la ventana se renderiza vacía sin subviews (issues upstream #4848 y #4374). Workaround: forzar `app.Init(DriverRegistry.Names.DOTNET)`.
- ⚠️ **API por instancia de Terminal.Gui v2** — la API estática legacy de v1 no compila contra v2.4.17 (CS0246/CS0117). Usar `Application.Create()` → `IApplication`.
- ⚠️ **La doc oficial de Terminal.Gui a veces miente** — verificar el API contra la DLL (`Terminal.Gui.xml`) antes de codificar.
- ⚠️ **`.slnx` en .NET 10** — `dotnet new sln` genera el formato XML nuevo, no el `.sln` clásico.
- ℹ️ **Case-sensitivity** — las propiedades de librerías son PascalCase (`Y`, no `y`).
- ⚠️ **Race de reuso de PID en `BlockEnforcer`** — entre el SIGTERM y el chequeo de vida, el PID puede reciclarse y señalarse otro proceso. Mitigación completa: comparar el start-time de `/proc/<pid>/stat` (Fase 4).
- ⚠️ **Fase 3 sin cablear** — `Worker`, `ProcessMonitor`, `BlockEnforcer` e `IpcServer` existen y están testeados, pero `Program.cs` solo registra `Worker`, y `IpcServer` no tiene un `IIpcRequestHandler` real. El daemon todavía no escanea ni mata en runtime; el mapeo nombre→PID y el handler real dependen del BlockEngine (Fase 4).

## Entorno

- SDK .NET 10.0.111 (Arch: `sudo pacman -S dotnet-sdk`).
- Build/test: `dotnet build` / `dotnet test`.
- Run TUI: `dotnet run --project src/FocusBlock.Tui`.
- TDD estricto: tests ANTES de implementar.
- Convención: código en inglés, docs en español (bloques de código con `cs`).