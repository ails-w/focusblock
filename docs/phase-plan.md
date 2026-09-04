# Plan de Fases — FocusBlock

> Índice maestro del desarrollo. La fase activa tiene detalle expandido.
> Conceptos → `docs/learning/phase-NN-name.md` · Log → `docs/progress-log/phase-NN-name.md`
> Estado mutable → `docs/handoff.md`

## Reglas globales

- TDD estricto: RED → GREEN → REFACTOR (test ANTES de implementar).
- Código en inglés, docs en español.
- Commits convencionales (feat:, fix:, test:, docs:, refactor:).
- Al cerrar una fase: actualizar log + conceptos + plan-fases + handoff + commit.

## Resumen de fases

| # | Nombre | Estado | Conceptos | Log |
|---|--------|--------|-----------|-----|
| 0 | Setup | ✅ | `learning/phase-00-setup.md` | `progress-log/phase-00-setup.md` |
| 1 | Esqueleto TUI | ⏳ | `learning/phase-01-tui.md` | `progress-log/phase-01-tui.md` |
| 2 | Configuración | ⏳ | `learning/phase-02-config.md` | `progress-log/phase-02-config.md` |
| 3 | Daemon y monitor | ⏳ | `learning/phase-03-daemon.md` | `progress-log/phase-03-daemon.md` |
| 4 | Núcleo bloqueador | ⏳ | `learning/phase-04-blocker.md` | `progress-log/phase-04-blocker.md` |
| 5 | Anti-bypass | ⏳ | `learning/phase-05-antibypass.md` | `progress-log/phase-05-antibypass.md` |
| 6 | Métricas | ⏳ | `learning/phase-06-metrics.md` | `progress-log/phase-06-metrics.md` |
| 7 | Pulido y testing | ⏳ | `learning/phase-07-polish.md` | `progress-log/phase-07-polish.md` |

---

## Fase 0 — Setup ✅

**Objetivo:** dejar lista la solución .NET, el esqueleto TUI y la infraestructura de tests.

### Scope

- Solución `.slnx` + proyectos base (`src/`, `tests/`).
- Hello World TUI con Terminal.Gui v2.
- Infraestructura de tests (xUnit + Moq + FluentAssertions).

### Fuera de scope

- Lógica de negocio, configuración, daemon, IPC.

### Conceptos de aprendizaje

- [x] Terminal.Gui v2 API por instancia → `docs/learning/phase-00-setup.md`
- [x] Formato `.slnx` de .NET 10 → `docs/learning/phase-00-setup.md`
- [x] Driver DOTNET workaround en Linux → `docs/learning/phase-00-setup.md`

### Criterio de salida

- [x] `dotnet run` muestra ventana TUI con label.
- [x] Proyecto de tests compila y corre.

---

## Fase 1 — Esqueleto TUI ⏳

**Objetivo:** construir la estructura de la TUI: orquestación de la app, ventana principal con menú y navegación entre vistas placeholder.

### Scope

- `FocusBlockApp` (orquestación).
- `MainWindow` con MenuBar y StatusBar.
- Navegación entre vistas.
- `StatusView` (estado del daemon), `BlockListView` (lista de bloqueos), `AddBlockView` (formulario placeholder).

### Fuera de scope

- Daemon real, IPC real, configuración real, persistencia (fases 2-6).

### Conceptos de aprendizaje

- [ ] Terminal.Gui v2: vistas, layout, navegación → `docs/learning/phase-01-tui.md`
- [ ] Composición de UI con `IApplication` → `docs/learning/phase-01-tui.md`

### Criterio de salida

- [ ] La TUI arranca y muestra MainWindow con menú.
- [ ] La navegación entre vistas funciona con datos placeholder.
- [ ] Tests unitarios de orquestación y vistas verdes.

### Features (TDD)

#### Feature 1.1: Orquestación de la App
- [ ] Escribir test: `FocusBlockApp_CreatesMainWindow` (RED)
- [ ] Crear `App.cs` con clase `FocusBlockApp` (GREEN)
- [ ] Refactorizar si es necesario

#### Feature 1.2: Ventana Principal con Menú
- [ ] Escribir test: `MainWindow_HasMenuBarAndStatusBar` (RED)
- [ ] Crear `Views/MainWindow.cs` con MenuBar, StatusBar (GREEN)
- [ ] Implementar items de menú: Block, View, Settings, Help

#### Feature 1.3: Navegación entre Vistas
- [ ] Escribir test: `MainWindow_MenuNavigatesToViews` (RED)
- [ ] Implementar lógica de cambio de vistas (GREEN)

#### Feature 1.4: Vista de Estado
- [ ] Escribir test: `StatusView_DisplaysDaemonStatus` (RED)
- [ ] Crear `Views/StatusView.cs` (GREEN)
- [ ] Implementar método `RefreshStatus()`

#### Feature 1.5: Lista de Bloqueos
- [ ] Escribir test: `BlockListView_DisplaysListOfApps` (RED)
- [ ] Crear `Views/BlockListView.cs` (GREEN)

#### Feature 1.6: Agregar Bloqueo (Placeholder)
- [ ] Escribir test: `AddBlockView_ShowsFormFields` (RED)
- [ ] Crear `Views/AddBlockView.cs` con formulario placeholder (GREEN)

---

## Fase 2 — Sistema de Configuración ⏳

**Objetivo:** modelos de configuración, serialización JSON y hashing de contraseñas.

### Scope

- Proyecto `FocusBlock.Contracts/` con modelos `AppConfig`, `BlockRuleConfig`, `SecurityConfig`.
- Serialización con `System.Text.Json` (round-trip).
- `ConfigService` con carga/guardado async.
- `AuthService` con Argon2id (hash + verify).

### Fuera de scope

- Daemon, IPC, motor de reglas, métricas.

### Conceptos de aprendizaje

- [ ] Records y modelos de configuración → `docs/learning/phase-02-config.md`
- [ ] `System.Text.Json` round-trip y converters → `docs/learning/phase-02-config.md`
- [ ] Async I/O en archivos → `docs/learning/phase-02-config.md`
- [ ] Hashing de contraseñas (Argon2id, salt) → `docs/learning/phase-02-config.md`

### Criterio de salida

- [ ] `ConfigService` carga defaults si falta archivo y persiste cambios.
- [ ] `AuthService` hash/verify con Argon2id pasa tests.
- [ ] Suite unitaria de la fase verde.

### Features (TDD)

#### Feature 2.1: Modelos de Configuración
- [ ] Escribir test: `AppConfig_DefaultValues_AreCorrect` (RED)
- [ ] Crear proyecto `FocusBlock.Contracts/` (GREEN)
- [ ] Crear modelos: `AppConfig`, `BlockRuleConfig`, `SecurityConfig`
- [ ] Agregar a solución

#### Feature 2.2: Serialización JSON
- [ ] Escribir test: `AppConfig_SerializeDeserialize_RoundTrips` (RED)
- [ ] Implementar serialización con `System.Text.Json` (GREEN)
- [ ] Manejar converter personalizado para `TimeOnly` si se necesita

#### Feature 2.3: Servicio de Configuración
- [ ] Escribir test: `ConfigService_LoadAsync_ReturnsDefaults_WhenFileMissing` (RED)
- [ ] Escribir test: `ConfigService_SaveAsync_PersistsToFile` (RED)
- [ ] Crear `Services/ConfigService.cs` (GREEN)
- [ ] Implementar carga/guardado con async/await

#### Feature 2.4: Hashing de Contraseñas
- [ ] Escribir test: `AuthService_HashPassword_ReturnsHashAndSalt` (RED)
- [ ] Escribir test: `AuthService_VerifyPassword_ReturnsTrue_WhenCorrect` (RED)
- [ ] Escribir test: `AuthService_VerifyPassword_ReturnsFalse_WhenWrong` (RED)
- [ ] Agregar paquete Argon2: `dotnet add package Konscious.Security.Cryptography.Argon2`
- [ ] Crear `Services/AuthService.cs` (GREEN)
- [ ] Implementar `HashPassword()` y `VerifyPassword()`

---

## Fase 3 — Daemon y Monitor de Procesos ⏳

**Objetivo:** daemon root que monitorea `/proc`, mata procesos y sirve IPC por Unix socket.

### Scope

- Proyecto `FocusBlock.Daemon/` con `Worker` (BackgroundService).
- `ProcessMonitor` escaneando `/proc`.
- `BlockEnforcer` con SIGTERM → SIGKILL (P/Invoke).
- `IpcServer` en Unix socket.
- Integración Docker (multi-stage + compose).

### Fuera de scope

- Motor de reglas (Fase 4), anti-bypass (Fase 5), métricas (Fase 6).

### Conceptos de aprendizaje

- [ ] `BackgroundService` y ciclo de vida del worker → `docs/learning/phase-03-daemon.md`
- [ ] Escaneo de `/proc` en Linux → `docs/learning/phase-03-daemon.md`
- [ ] P/Invoke y señales (SIGTERM/SIGKILL) → `docs/learning/phase-03-daemon.md`
- [ ] Unix domain sockets (servidor) → `docs/learning/phase-03-daemon.md`
- [ ] Docker multi-stage → `docs/learning/phase-03-daemon.md`

### Criterio de salida

- [ ] Daemon arranca como servicio, escanea procesos y mata por nombre.
- [ ] Responde a requests IPC (status, add_block, remove_block).
- [ ] Corre en Docker con acceso a `/proc`.
- [ ] Tests unitarios + integración verdes.

### Features (TDD)

#### Feature 3.1: Worker BackgroundService
- [ ] Escribir test: `Worker_StartsAndRunsUntilCancelled` (RED)
- [ ] Crear proyecto `FocusBlock.Daemon/` (GREEN)
- [ ] Agregar `Worker.cs` como `BackgroundService`
- [ ] Agregar a solución

#### Feature 3.2: Monitor de Procesos
- [ ] Escribir test: `ProcessMonitor_GetRunningProcesses_ReturnsList` (RED)
- [ ] Crear `Services/ProcessMonitor.cs` (GREEN)
- [ ] Implementar escaneo de `/proc`
- [ ] Escribir test: `ProcessMonitor_ExtractProcessName_ParsesStatus` (RED)
- [ ] Implementar helper `ExtractProcessName()`

#### Feature 3.3: Ejecutor de Bloqueos
- [ ] Escribir test: `BlockEnforcer_KillProcess_SendsSigterm` (RED)
- [ ] Crear `Services/BlockEnforcer.cs` (GREEN)
- [ ] Implementar P/Invoke `kill()` syscall
- [ ] Escribir test: `BlockEnforcer_EscalatesToSigkill_WhenSigtermFails` (RED)
- [ ] Implementar escalación SIGTERM → SIGKILL

#### Feature 3.4: Servidor IPC
- [ ] Escribir test: `IpcServer_HandlesStatusRequest` (RED)
- [ ] Crear `Services/IpcServer.cs` (GREEN)
- [ ] Implementar escuchador Unix socket
- [ ] Escribir test: `IpcServer_HandlesAddBlockRequest` (RED)
- [ ] Implementar enrutamiento de mensajes

#### Feature 3.5: Integración Docker
- [ ] Crear `config/docker/Dockerfile.daemon` (GREEN)
- [ ] Crear `config/docker/docker-compose.yml` (GREEN)
- [ ] Probar daemon en Docker: `docker-compose up daemon`
- [ ] Verificar acceso a `/proc` en contenedor

---

## Fase 4 — Núcleo del Bloqueador ⏳

**Objetivo:** motor de reglas, cooldown thread-safe y sistema de challenges.

### Scope

- `BlockEngine` evaluación de reglas (horarios, estado).
- `CooldownManager` thread-safe.
- `ChallengeSystem` (texto aleatorio de verificación).
- Flujo de early stop (cooldown + challenge + contraseña).

### Fuera de scope

- Protección de archivos con `chattr +i` (Fase 5).

### Conceptos de aprendizaje

- [ ] Evaluación de reglas de dominio → `docs/learning/phase-04-blocker.md`
- [ ] Thread-safety y colecciones concurrentes → `docs/learning/phase-04-blocker.md`
- [ ] Patrón producer/consumer con `Channel<T>` → `docs/learning/phase-04-blocker.md`

### Criterio de salida

- [ ] `BlockEngine` decide bloquear según horario/estado con tests verdes.
- [ ] Cooldown funciona de forma thread-safe y expira correctamente.
- [ ] Early stop requiere challenge + contraseña correcta.

### Features (TDD)

#### Feature 4.1: Motor de Reglas
- [ ] Escribir test: `BlockEngine_Evaluate_ReturnsBlock_WhenInSchedule` (RED)
- [ ] Escribir test: `BlockEngine_Evaluate_ReturnsNoBlock_WhenOutsideSchedule` (RED)
- [ ] Escribir test: `BlockEngine_Evaluate_ReturnsNoBlock_WhenRuleDisabled` (RED)
- [ ] Crear `Services/BlockEngine.cs` (GREEN)
- [ ] Implementar lógica de evaluación de reglas

#### Feature 4.2: Gestor de Cooldown
- [ ] Escribir test: `CooldownManager_StartCooldown_SetsExpiry` (RED)
- [ ] Escribir test: `CooldownManager_IsOnCooldown_ReturnsTrue_WhenActive` (RED)
- [ ] Escribir test: `CooldownManager_IsOnCooldown_ReturnsFalse_WhenExpired` (RED)
- [ ] Crear `Services/CooldownManager.cs` (GREEN)
- [ ] Implementar tracking thread-safe de cooldowns

#### Feature 4.3: Sistema de Challenges
- [ ] Escribir test: `ChallengeSystem_GenerateChallenge_ReturnsRandomText` (RED)
- [ ] Crear `Services/ChallengeSystem.cs` (GREEN)
- [ ] Escribir test: `ChallengeDialog_ShowsChallenge_AndValidatesInput` (RED)
- [ ] Crear `ChallengeDialog` TUI (GREEN)

#### Feature 4.4: Flujo de Early Stop
- [ ] Escribir test: `BlockEngine_TryEarlyStop_ReturnsTrue_WhenPasswordCorrect` (RED)
- [ ] Escribir test: `BlockEngine_TryEarlyStop_ReturnsFalse_WhenPasswordWrong` (RED)
- [ ] Implementar `TryEarlyStop()` en BlockEngine (GREEN)
- [ ] Conectar cooldown + challenge + verificación de contraseña

---

## Fase 5 — Seguridad Anti-Bypass ⏳

**Objetivo:** impedir que el usuario desactive el bloqueo editando archivos o servicios.

### Scope

- `FileProtector` con `chattr +i` (ioctl P/Invoke).
- `SecurityManager` bloqueando config durante bloqueo activo.
- Tests de integración con filesystem real.

### Fuera de scope

- JWT, autenticación de red, cifrado de disco.

### Conceptos de aprendizaje

- [ ] `chattr +i` y `ioctl` en Linux → `docs/learning/phase-05-antibypass.md`
- [ ] P/Invoke avanzado (structs, syscalls) → `docs/learning/phase-05-antibypass.md`

### Criterio de salida

- [ ] Config inmodificable durante bloqueo activo (`chattr +i` verificado).
- [ ] Unlock requiere contraseña.
- [ ] Tests de integración con filesystem real verdes.

### Features (TDD)

#### Feature 5.1: Protector de Archivos
- [ ] Escribir test: `FileProtector_LockFile_SetsImmutableFlag` (RED)
- [ ] Crear `Services/FileProtector.cs` (GREEN)
- [ ] Implementar P/Invoke para `ioctl` (chattr +i)
- [ ] Escribir test: `FileProtector_UnlockFile_ClearsImmutableFlag` (RED)

#### Feature 5.2: Gestor de Seguridad
- [ ] Escribir test: `SecurityManager_LockConfig_DuringActiveBlock` (RED)
- [ ] Crear `Services/SecurityManager.cs` (GREEN)
- [ ] Implementar bloqueo de config durante bloques activos
- [ ] Escribir test: `SecurityManager_TryUnlockConfig_RequiresPassword` (RED)

#### Feature 5.3: Test de Integración
- [ ] Escribir test integración: `SecurityManager_WithRealFile_LocksAndUnlocks` (RED)
- [ ] Probar con filesystem real (GREEN)
- [ ] Verificar que `chattr +i` previene eliminación

---

## Fase 6 — Métricas y Analytics ⏳

**Objetivo:** registrar eventos de bloqueo y uso diario en SQLite, con gráficos ASCII en la TUI.

### Scope

- Esquema SQLite (block_events, app_usage, bypass_attempts).
- `MetricsCollector` con Dapper.
- UPSERT de uso diario.
- `MetricsView` con charts (Spectre.Console).

### Fuera de scope

- Analytics avanzado, dashboard web, exportación.

### Conceptos de aprendizaje

- [ ] SQLite + Dapper → `docs/learning/phase-06-metrics.md`
- [ ] UPSERT y queries agregadas → `docs/learning/phase-06-metrics.md`
- [ ] Charts ASCII con Spectre.Console → `docs/learning/phase-06-metrics.md`

### Criterio de salida

- [ ] Eventos de bloqueo grabados en SQLite.
- [ ] Uso diario acumulado con UPSERT.
- [ ] Stats semanales visibles en la TUI.

### Features (TDD)

#### Feature 6.1: Configuración SQLite
- [ ] Escribir test: `MetricsCollector_RecordBlockEvent_InsertsRow` (RED)
- [ ] Crear `Services/MetricsCollector.cs` (GREEN)
- [ ] Implementar inicialización de esquema SQLite
- [ ] Agregar paquete Dapper

#### Feature 6.2: Grabación de Métricas
- [ ] Escribir test: `MetricsCollector_RecordUsage_UpdatesDailyTotal` (RED)
- [ ] Implementar UPSERT para uso diario (GREEN)
- [ ] Escribir test: `MetricsCollector_GetDailyStats_ReturnsWeekData` (RED)

#### Feature 6.3: Charts en TUI
- [ ] Escribir test: `MetricsView_RenderBarChart_DisplaysData` (RED)
- [ ] Crear `Views/MetricsView.cs` (GREEN)
- [ ] Integrar Spectre.Console para charts ASCII

#### Feature 6.4: Test de Integración
- [ ] Escribir test integración: `MetricsCollector_WithRealSQLite_PersistsAndQueries` (RED)
- [ ] Probar con TestContainer o DB temporal (GREEN)

---

## Fase 7 — Pulido y Testing ⏳

**Objetivo:** robustez, casos borde, cobertura y documentación final.

### Scope

- Manejo de errores (daemon caído, JSON corrupto).
- Casos borde (procesos desaparecidos, cooldowns circulares).
- Suite completa con cobertura.
- Documentación final y Docker completo.

### Fuera de scope

- Nuevas features.

### Conceptos de aprendizaje

- [ ] Manejo de errores y degradación elegante → `docs/learning/phase-07-polish.md`
- [ ] Cobertura y mutation testing → `docs/learning/phase-07-polish.md`

### Criterio de salida

- [ ] Suite unitaria e integración 100% verde.
- [ ] Cobertura > 80% unit, > 60% integración.
- [ ] README, docs y Docker finalizados.

### Features (TDD)

#### Feature 7.1: Manejo de Errores
- [ ] Escribir test: `IpcClient_ThrowsWhenDaemonNotRunning` (RED)
- [ ] Implementar degradación elegante (GREEN)
- [ ] Escribir test: `ConfigService_HandlesCorruptJson` (RED)
- [ ] Implementar fallback a valores por defecto

#### Feature 7.2: Casos Borde
- [ ] Escribir test: `ProcessMonitor_HandlesMissingProcEntry` (RED)
- [ ] Escribir test: `BlockEngine_HandlesCircularCooldown` (RED)
- [ ] Manejar todos los casos borde

#### Feature 7.3: Suite Completa de Tests
- [ ] Ejecutar todos los unitarios: `dotnet test --filter "Category=Unit"`
- [ ] Ejecutar todas las integraciones: `dotnet test --filter "Category=Integration"`
- [ ] Verificar cobertura > 80% unit, > 60% integración
- [ ] Corregir tests fallidos

#### Feature 7.4: Documentación
- [ ] Actualizar `docs/architecture.md`
- [ ] Actualizar `docs/development-plan.md`
- [ ] Actualizar `docs/learning/` para todas las fases
- [ ] Finalizar `README.md`

#### Feature 7.5: Docker Final
- [ ] Probar `docker-compose up` completo
- [ ] Verificar que daemon inicia y responde
- [ ] Probar que TUI conecta con daemon Docker