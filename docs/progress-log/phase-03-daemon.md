# Fase 3: Daemon y Monitor de Procesos — Log

> Log HISTÓRICO de la fase. Se acumula, no se borra.
> Estado actual → `docs/handoff.md` · Conceptos → `docs/learning/phase-03-daemon.md`

## Estado

**Estado**: Completada (con cableado end-to-end diferido a Fase 4)
**Última Actualización**: 2026-09-15

## Objetivos

- Proyecto `FocusBlock.Daemon/` con `Worker` (BackgroundService).
- `ProcessMonitor` escaneando `/proc`.
- `BlockEnforcer` con SIGTERM → SIGKILL (P/Invoke).
- `IpcServer` en Unix socket.

## Progreso

- [x] Feature 3.1: Worker BackgroundService (2026-09-09)
- [x] Feature 3.2: Monitor de Procesos (2026-09-09)
- [x] Feature 3.3: Ejecutor de Bloqueos (2026-09-15)
- [x] Feature 3.4: Servidor IPC (2026-09-15)
- [x] Endurecimiento de tests de 3.1/3.2 + seams (2026-09-15)

## Tareas Completadas

### 2026-09-09 — Features 3.1 y 3.2
- **Descripción**: Nace el proyecto `FocusBlock.Daemon`. El daemon aprende a arrancar como servicio
  y a leer la lista de procesos del sistema.
- **Archivos**:
  - `src/FocusBlock.Daemon/` — proyecto + `Worker.cs`, `Program.cs`
  - `src/FocusBlock.Daemon/Services/ProcessMonitor.cs` — escaneo de `/proc`
  - `tests/.../WorkerTests.cs`, `ProcessMonitorTests.cs`
- **Commits**: `a559bc8`, `f6fc68d`
- **Tests**: 16 verdes (suite total al momento).

### 2026-09-15 — Endurecimiento de 3.1/3.2
- **Descripción**: Los tests originales eran débiles: `Worker` solo arrancaba/paraba, y
  `ProcessMonitor` testeaba contra `/proc` real (`NotBeEmpty`, no determinista). Se introdujo el
  primer seam de la fase.
- **Archivos**:
  - `Services/IProcessSource.cs` + `Services/ProcProcessSource.cs` — seam sobre `/proc`
  - `Worker.cs` — tick inyectable (`Func<CancellationToken, Task>`)
  - `tests/.../WorkerTests.cs`, `ProcessMonitorTests.cs` — reescritos
- **Commit**: `fdb1a43`

### 2026-09-15 — Feature 3.3 (BlockEnforcer)
- **Descripción**: El daemon aprende a matar un proceso por PID, con escalación educada.
- **Archivos**:
  - `Services/Signal.cs` — `enum Signal { Term = 15, Kill = 9 }`
  - `Services/ISignalSender.cs` + `Services/LibcSignalSender.cs` — seam + P/Invoke `kill()`
  - `Services/BlockEnforcer.cs` — SIGTERM → gracia → SIGKILL
  - `tests/.../BlockEnforcerTests.cs`
- **Commit**: `262c2e6`

### 2026-09-15 — Feature 3.4 (IpcServer)
- **Descripción**: El daemon aprende a hablar por Unix socket. Nace el contrato IPC compartido.
- **Archivos**:
  - `src/FocusBlock.Contracts/IpcProtocol.cs` — `MessageType` + `IpcMessage`
  - `Services/IIpcRequestHandler.cs` — seam del handler
  - `Services/IpcServer.cs` — accept loop + framing JSON newline-delimited
  - `tests/.../IpcServerTests.cs`
  - `FocusBlock.Daemon.csproj` — ProjectReference a Contracts
- **Commit**: `06defbd`

### 2026-09-15 — Refactor y estilo
- `45dd8d0` — `ConfigSerializer` cachea `JsonSerializerOptions` (ya no las crea por llamada).
- `d27bc65` — `style: fix final newline` en 13 archivos para pasar `dotnet format` (gate de CI).

## Decisiones

1. **Feature 3.5 (Docker) eliminada.** Un contenedor no puede correr este daemon: aísla el PID
   namespace, así que no ve ni puede matar procesos del host. Docker quedó como extra de aprendizaje
   (`docs/extras/docker-multistage.md`) y el testing pasó a **puntos de inyección (seams)** → ADR-009.
2. **Seams por dependencia del SO.** `IProcessSource`, `ISignalSender`, ruta del socket y tick
   inyectable. Unit tests con fakes; integración solo para lo que toca el kernel de verdad.
3. **`IProcessSource` incluye `Exists(pid)`** para que `BlockEnforcer` no mande SIGKILL a ciegas.
4. **`BlockEnforcer` opera sobre PID**, no sobre nombre: el mapeo nombre→PID es responsabilidad de
   Fase 4.
5. **Contrato IPC tipado** (`IpcMessage` con campos) en lugar de un `Payload` string opaco: evita
   doble encoding. Se actualizó `architecture.md`.
6. **Wire format `snake_case` + enums como string** (`JsonNamingPolicy.SnakeCaseLower`).
7. **Los tests de socket NO se marcan `Integration`**: son herméticos (path temporal) y rápidos. Sí
   se marcan los que tocan `/proc` real y procesos reales.

## Problemas

1. **`ProtocolType.Unix` no existe en .NET 10** (`CS0117`). *Solución*: para `AddressFamily.Unix`
   con `SocketType.Stream` se usa `ProtocolType.Unspecified`. Verificado empíricamente.
2. **`FocusBlock.Daemon.csproj` no referenciaba `Contracts`.** *Solución*: se agregó el
   `ProjectReference` cuando `IpcServer` necesitó `IpcMessage`.
3. **`dotnet format --verify-no-changes` fallaba** en 13 archivos nuevos por `FINALNEWLINE`.
   *Solución*: `dotnet format` + commit `d27bc65`. Era un bloqueante real de CI.
4. **Docs de estado desactualizados** (decían que Fase 3 no había empezado). *Solución*: corregidos
   `handoff.md`, `phase-plan.md` y `AGENTS.md`.

## Métricas

- Tests: **32** (29 unit + 3 integración), todos verdes.
- Tests nuevos de la fase: 16 → 32.
- Commits de la fase: 7 (2 features originales + 5 de cierre/refactor/docs).
- Cobertura: N/A.

## Pendientes / Diferido a Fase 4

- **Cableado end-to-end**: `Program.cs` solo registra `Worker` con tick vacío; `IpcServer` no tiene
  handler real; falta el mapeo **nombre → PID**. Depende del `BlockEngine`.
- **Campos ricos de `status_response`** (`active_blocks`, `daemon_uptime`) — con el motor.
- **Race de reuso de PID** en `BlockEnforcer` — mitigación completa leyendo el start-time de
  `/proc/<pid>/stat`.
- **`TimeProvider`** para el reloj del motor y del cooldown.
