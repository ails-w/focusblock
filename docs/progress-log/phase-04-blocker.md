# Fase 4: Núcleo del Bloqueador — Log

> Log HISTÓRICO de la fase. Se acumula, no se borra.
> Estado actual → `docs/handoff.md` · Conceptos → `docs/learning/phase-04-blocker.md` ·
> Diagramas → `docs/diagrams/phase-04-blocker.md`

## Estado

**Estado**: Completada (features 4.1–4.4 + cableado end-to-end + cierre documental)
**Última Actualización**: 2026-09-24

## Objetivos

- `BlockEngine`: evaluación de reglas (horarios, estado) como función pura.
- `CooldownManager` thread-safe para ventanas de gracia.
- `ChallengeSystem` + diálogos de fricción (challenge y contraseña).
- Flujo de early stop: fricción + contraseña + IPC `force_stop`.
- Cableado end-to-end del daemon (nombre→PID, handler IPC real, DI) — diferido desde Fase 3.

## Progreso

- [x] Feature 4.1: Motor de Reglas (2026-09-23) — `8ce1f4c`
- [x] Feature 4.2: Gestor de Cooldown (2026-09-23) — `d971df2`
- [x] Feature 4.3: Sistema de Challenges (2026-09-24) — `900729d`
- [x] Feature 4.4: Flujo de Early Stop (2026-09-24) — `b6dc8c5`, `3f40462`, `65fb289`
- [x] Extracción de `FocusBlock.Core` / inversión de dependencias (2026-09-24) — `00993a1`, `770dca9`
- [x] Cableado end-to-end: nombre→PID + handler IPC + DI (2026-09-24) — `67ce6b8`, `3f40462`, `018cb5e`
- [x] Glue de UI: diálogos y menú Early Stop (2026-09-24) — `7e1753f`
- [x] Cierre documental (2026-09-24) — `8b8f182` + este doc

## Tareas Completadas

### 2026-09-23 — Feature 4.1 (BlockEngine)
- **Descripción**: Nace el motor de reglas. `Evaluate` decide si una regla aplica en un instante
  (`now` por parámetro): ventana semiabierta `[Start, End)`, wrap de medianoche, regla deshabilitada
  y ventana degenerada. `GetAppsToBlock` proyecta las apps activas.
- **Archivos**:
  - `src/FocusBlock.Daemon/Services/BlockEngine.cs` — motor puro
  - `tests/FocusBlock.Tests.Unit/BlockEngineTests.cs` — casos RED del plan + bordes (inicio/fin, wrap, degenerada)
- **Commits**: `8ce1f4c` (código), `30d0af2` (docs). Ya mergeados a `main` vía PR #8.

### 2026-09-23 — Feature 4.2 (CooldownManager)
- **Descripción**: Ventanas de gracia por app, thread-safe. `ConcurrentDictionary` con
  `StringComparer.Ordinal`; last-writer-wins al reiniciar un cooldown; expiración semiabierta.
- **Archivos**:
  - `src/FocusBlock.Daemon/Services/CooldownManager.cs`
  - `tests/FocusBlock.Tests.Unit/CooldownManagerTests.cs` — incluye `IsThreadSafe_UnderConcurrentAccess` con
    `Parallel.For(0, 200, ...)`
- **Commit**: `d971df2`

### 2026-09-24 — Feature 4.3 (ChallengeSystem + ChallengeDialog)
- **Descripción**: Fricción previa al early stop. `ChallengeSystem` genera 4 palabras aleatorias con
  `RandomNumberGenerator` (CSPRNG) y valida con comparación ordinal (no es un secreto, no lleva
  tiempo constante). `ChallengeDialog` muestra el challenge y valida el tipeo.
- **Archivos**:
  - `src/FocusBlock.Tui/Services/ChallengeSystem.cs`
  - `src/FocusBlock.Tui/Views/ChallengeDialog.cs`
  - `tests/FocusBlock.Tests.Unit/ChallengeSystemTests.cs`,
    `tests/FocusBlock.Tests.Unit/ChallengeDialogTests.cs`
- **Commit**: `900729d`

### 2026-09-24 — Inversión de dependencias (FocusBlock.Core)
- **Descripción**: El daemon (root) necesita verificar la contraseña y leer la config, pero
  `AuthService`/`ConfigService` vivían en la TUI. Se extrajo `FocusBlock.Core` con
  `IPasswordVerifier` (puerto) y `AuthService` (adaptador); después se movió `ConfigService`.
  Descartadas: duplicar código o que el daemon referencie la TUI.
- **Archivos**:
  - `src/FocusBlock.Core/FocusBlock.Core.csproj`, `IPasswordVerifier.cs`, `AuthService.cs`
  - `AuthService.cs` — movido de `FocusBlock.Tui/Services/` a `FocusBlock.Core/` (rename)
  - `ConfigService.cs` — movido de `FocusBlock.Tui/Services/` a `FocusBlock.Core/` (rename)
  - `tests/FocusBlock.Tests.Unit/AuthServiceTests.cs`,
    `tests/FocusBlock.Tests.Unit/ConfigServiceTests.cs` — solo cambio de namespace
- **Commits**: `00993a1`, `770dca9`

### 2026-09-24 — Feature 4.4 (TryEarlyStop + IPC ForceStop)
- **Descripción**: El gate de seguridad. `BlockEngine.TryEarlyStop` delega en `IPasswordVerifier`;
  `DaemonRequestHandler` (handler real) rutea `status` y `force_stop`, valida campos, verifica en el
  lado confiable y arranca el cooldown. `IpcServerHostedService` hospeda el socket en el host.
  `IpcJson.Options` se comparte entre cliente y servidor.
- **Archivos**:
  - `src/FocusBlock.Daemon/Services/BlockEngine.cs` — `TryEarlyStop`
  - `src/FocusBlock.Daemon/Services/DaemonRequestHandler.cs` — handler real
  - `src/FocusBlock.Daemon/Services/IpcServerHostedService.cs` — ciclo de vida del socket
  - `src/FocusBlock.Daemon/Program.cs` — DI completo
  - `src/FocusBlock.Contracts/IpcJson.cs` — opciones compartidas
  - `src/FocusBlock.Contracts/IpcProtocol.cs` — `ForceStop` + campo `Password`
  - `tests/FocusBlock.Tests.Unit/DaemonRequestHandlerTests.cs`
- **Commits**: `b6dc8c5`, `3f40462`, `018cb5e`, `b9704b0` (gitignore del índice local `.codegraph`)

### 2026-09-24 — Bucle de bloqueo end-to-end
- **Descripción**: Se cierra el cableado diferido de Fase 3. `ProcessInfo(Pid, Name)`,
  `ProcessMonitor.GetProcesses()` (nombre + PID), `BlockCoordinator.RunOnceAsync` (early return,
  una lectura de reloj, una de `/proc`, dedup de PIDs, skip por cooldown) y el `Worker` con tick
  real de 5 segundos.
- **Archivos**:
  - `src/FocusBlock.Daemon/Services/ProcessInfo.cs`
  - `src/FocusBlock.Daemon/Services/ProcessMonitor.cs` — `GetProcesses`
  - `src/FocusBlock.Daemon/Services/BlockCoordinator.cs`
  - `src/FocusBlock.Daemon/Program.cs` — `Worker` con `RunOnceAsync`
  - `tests/FocusBlock.Tests.Unit/BlockCoordinatorTests.cs`,
    `tests/FocusBlock.Tests.Unit/ProcessMonitorTests.cs`
- **Commit**: `67ce6b8`

### 2026-09-24 — Cliente IPC y flujo TUI
- **Descripción**: `IpcClient` (connect-per-request, framing newline-delimited), `EarlyStopService`
  (challenge → password → `force_stop`, degrada a `false` si el daemon no está), `IEarlyStopPrompt`
  + `TerminalEarlyStopPrompt` (glue modal) y `AppNameDialog`/`PasswordDialog`. `App.cs` conecta el
  flujo al menú y marshalea el resultado al hilo de UI con `_app.Invoke`.
- **Archivos**:
  - `src/FocusBlock.Tui/Services/IIpcClient.cs`, `IpcClient.cs`, `IEarlyStopPrompt.cs`,
    `TerminalEarlyStopPrompt.cs`, `EarlyStopService.cs`
  - `src/FocusBlock.Tui/Views/AppNameDialog.cs`, `PasswordDialog.cs`
  - `src/FocusBlock.Tui/App.cs`, `src/FocusBlock.Tui/Views/MainWindow.cs`
  - `tests/FocusBlock.Tests.Unit/IpcClientTests.cs`, `EarlyStopServiceTests.cs`,
    `AppNameDialogTests.cs`, `PasswordDialogTests.cs`, `MainWindowTests.cs`
- **Commits**: `65fb289`, `7e1753f`

### 2026-09-24 — Cierre
- `8b8f182` — `docs: mark phase 4 features complete` (`docs/phase-plan.md`).
- Este log + `docs/learning/phase-04-blocker.md` + diagramas + `docs/handoff.md`.

## Decisiones

1. **Motor puro con `now` por parámetro.** Sin reloj interno: tests deterministas para bordes y wrap
   de medianoche. El reloj del coordinador es `TimeProvider` (seam).
2. **Ventana semiabierta `[Start, End)` y chequeo explícito de ventana degenerada.** Sin este
   último, `Start == End` bloquearía 24/7 por la rama de wrap.
3. **Cooldown con `ConcurrentDictionary` + `StringComparer.Ordinal`.** Lectura desde el tick,
   escritura desde el handler IPC; last-writer-wins para extender/reiniciar la gracia.
4. **`FocusBlock.Core` por inversión de dependencias.** `IPasswordVerifier` como puerto y
   `AuthService` como adaptador; `ConfigService` también se movió. Alternativas descartadas:
   duplicar Argon2id o que el daemon dependa de la TUI.
5. **El challenge es fricción, no seguridad.** Comparación ordinal (`string.Equals`) porque está en
   pantalla; la contraseña sí usa Argon2id + `FixedTimeEquals` y se verifica en el daemon.
6. **`IpcJson.Options` compartido en `Contracts`.** Una sola instancia snake_case + enums string para
   ambos lados; evita divergencia (commit `018cb5e`).
7. **`DaemonRequestHandler` valida y no hace eco de la contraseña.** Respuestas solo `Ok`/`Error`;
   el secreto no sale del daemon.
8. **Un instante y una lectura de `/proc` por pasada.** `nowUtc` + `nowLocal` desde la misma lectura;
   `.ToList()` congela la foto; `killedPids` evita kills duplicados.
9. **El bucle sigue siendo secuencial: `Channel<T>` no aplicó.** Un tick a la vez, sin
   productor/consumidor; se descartó como concepto de la fase (ver nota en el learning doc).
10. **Flujo de trabajo: un commit por feature en `dev`, PRs al cerrar la fase** (una de código y una
    de docs), según `4158b30`. `8ce1f4c` + `30d0af2` se mergearon a `main` antes de este cambio, vía
    PR #8.

## Problemas

1. **Bug de spec: `DateTimeOffset.TimeOfDay` es `TimeSpan`, no `TimeOnly`.** El plan asumía que se
   podía pasar `TimeOfDay` directo a `Evaluate`; no compila. *Solución*:
   `TimeOnly.FromDateTime(offset.DateTime)` en `BlockCoordinator`.
2. **Bug de DI: faltaban registros de `ProcessMonitor` y `BlockEnforcer`.** Al resolver
   `BlockCoordinator` (que los recibe por constructor), el host no encontraba las dependencias.
   *Solución*: registrarlos como singletons en `Program.cs`; verificado con el arranque real.
3. **`SocketException` NO deriva de `IOException`.** El primer `catch (IOException)` en
   `EarlyStopService` no cubría "daemon caído". *Solución*: dos `catch` separados; ambos degradan a
   `false`.
4. **`Dialog.Result` es el índice del botón.** OK = 0 (se agrega primero); `null` = Escape.
   *Solución*: constante `OkButtonIndex = 0` y comparación explícita, tanto en
   `TerminalEarlyStopPrompt` como en `App.cs`.
5. **Gotcha del `comm`: `Name:` en `/proc/<pid>/status` está limitado a 15 caracteres.** Un
   `AppName` más largo nunca matchea (ej. `google-chrome-stable` → `google-chrome-s`). Documentado
   en `ProcessMonitor`; mitigación completa (leer `cmdline`) queda pendiente.
6. **`.editorconfig` con `insert_final_newline = false`.** Los archivos nuevos deben quedar sin línea
   final extra para no romper el gate de `dotnet format --verify-no-changes`.

## Métricas

- Tests: **91** (88 unit + 3 integración), todos verdes (`dotnet test`).
- Tests nuevos de la fase: **59** (de 32 al cerrar Fase 3 → 91).
- Commits de la fase: **15** (13 en `dev` sobre `main` + 2 ya mergeados vía PR #8).
- Cobertura: N/A.
- Smoke test: el daemon real levantó con el host, registró `IpcServer` + `Worker` y respondió IPC
  (verificación manual, no automatizada).

## Pendientes / Diferido

- **PRs `dev → main`** al cerrar la fase: una de código y una de docs (regla vigente).
- **Race de reuso de PID** en `BlockEnforcer` — mitigación completa leyendo el start-time de
  `/proc/<pid>/stat` antes de escalar.
- **Nombres de app > 15 caracteres** — el `comm` truncado impide el match; evaluar `cmdline`.
- **Config sin contraseña** — con `PasswordHash`/`PasswordSalt` vacíos, `TryEarlyStop` siempre falla:
  `force_stop` queda inutilizable hasta configurar una contraseña.
- **Socket path hardcodeado en la TUI** (`/run/focusblock/focusblock.sock` en `App.cs`); el daemon sí
  lo lee de configuración. Unificar.
- **`OnEarlyStopAsync` sin try/catch** — es fire-and-forget desde el menú; una excepción no capturada
  (distinta de las que ya absorbe `EarlyStopService`) quedaría sin mensaje al usuario.
- **`AddBlock`/`RemoveBlock` siguen sin handler real** — `DaemonRequestHandler` responde `Error`
  ("Unsupported message type"). El CRUD de reglas por IPC no es parte de esta fase.
- **Campos ricos de `status_response`** (`active_blocks`, `daemon_uptime`) — pendientes.
- **Revisar ADR-004 (formato PHC)** antes de Fase 5: los parámetros de Argon2id no se guardan con el
  hash, así que subirlos invalida las huellas existentes.
