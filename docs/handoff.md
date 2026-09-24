# Handoff — Continuidad

> ⚠️ **ESTADO MUTABLE.** Se SOBREESCRIBE al cerrar sesión. No es historial.
> Historial por fase → `docs/progress-log/`. Conceptos → `docs/learning/`.
> Aprendizajes y decisiones persistentes → Engram (memoria).

**Última actualización**: 2026-09-24

## Estado actual

| | |
|---|---|
| **Fase activa** | Fase 5 — Seguridad Anti-Bypass |
| **Última completada** | Fase 4 — Núcleo del Bloqueador ✅ |
| **Progreso** | Fases 0–4 ✅ · Fase 5 sin iniciar (91 tests verdes) |

## Próximo paso

Fase 5 — Feature 5.1 (Protector de Archivos): escribir test `FileProtector_LockFile_SetsImmutableFlag` (RED), crear `Services/FileProtector.cs` (GREEN) con P/Invoke para `ioctl` (`chattr +i`) detrás del seam `IFileAttributes`.

## Decisiones pendientes

- Verificar si el driver ANSI de Terminal.Gui ya está estable en una versión futura (para quitar el workaround del driver DOTNET si conviene).
- Revisar ADR-012 cuando se quieran las estrategias de bloqueo por uso (Racha/Tope) — requieren telemetría del daemon.
- Revisar ADR-004 (formato PHC) antes de Fase 5: los parámetros de Argon2id (`MemorySize`, `Iterations`) NO se guardan con el hash, así que subirlos invalida las huellas existentes. Evaluar formato PHC (`$argon2id$...`).
- Envolver `OnEarlyStopAsync` (`App.cs`) en try/catch con un MessageBox de error: es fire-and-forget desde el menú y una excepción no capturada dejaría al usuario sin feedback.

## Riesgos activos / Gotchas

- ⚠️ **Driver ANSI de Terminal.Gui roto en Linux** — la ventana se renderiza vacía sin subviews (issues upstream #4848 y #4374). Workaround: forzar `app.Init(DriverRegistry.Names.DOTNET)`.
- ⚠️ **API por instancia de Terminal.Gui v2** — la API estática legacy de v1 no compila contra v2.4.17 (CS0246/CS0117). Usar `Application.Create()` → `IApplication`.
- ⚠️ **La doc oficial de Terminal.Gui a veces miente** — verificar el API contra la DLL (`Terminal.Gui.xml`) antes de codificar.
- ⚠️ **`.slnx` en .NET 10** — `dotnet new sln` genera el formato XML nuevo, no el `.sln` clásico.
- ℹ️ **Case-sensitivity** — las propiedades de librerías son PascalCase (`Y`, no `y`).
- ⚠️ **Race de reuso de PID en `BlockEnforcer`** — entre el SIGTERM y el chequeo de vida, el PID puede reciclarse y señalarse otro proceso. Mitigación completa: comparar el start-time de `/proc/<pid>/stat`.
- ⚠️ **`comm` limitado a 15 caracteres** — `Name:` en `/proc/<pid>/status` es el `comm` del kernel, truncado a 15 chars: un `AppName` más largo nunca matchea (`google-chrome-stable` aparece como `google-chrome-s`). Mitigación futura: leer `cmdline`.
- ⚠️ **Socket path hardcodeado en la TUI** — `App.cs` usa `/run/focusblock/focusblock.sock` fijo; el daemon sí lo lee de configuración (`FocusBlock:SocketPath`). Unificar antes de deployment.
- ⚠️ **La config no incluye contraseña por defecto** — con `PasswordHash`/`PasswordSalt` vacíos, `force_stop` siempre falla (`TryEarlyStop` devuelve `false`). Hay que configurar una contraseña para probar el early stop.
- ⚠️ **`AddBlock`/`RemoveBlock` sin handler real** — `DaemonRequestHandler` responde `Error` ("Unsupported message type"); el CRUD de reglas por IPC sigue pendiente.

## Entorno

- SDK .NET 10.0.111 (Arch: `sudo pacman -S dotnet-sdk`).
- Build/test: `dotnet build` / `dotnet test`.
- Run TUI: `dotnet run --project src/FocusBlock.Tui`.
- TDD estricto: tests ANTES de implementar.
- Convención: código en inglés, docs en español (bloques de código con `cs`).