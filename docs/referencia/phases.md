# Fases de Desarrollo

Lista de features por fase con checkboxes para tracking de progreso.

---

## Fase 0: Setup del Proyecto

- [ ] Crear solución: `dotnet new sln -n FocusBlock`
- [ ] Crear proyecto TUI: `dotnet new console -n FocusBlock.Tui -o src/FocusBlock.Tui`
- [ ] Agregar a solución: `dotnet sln add src/FocusBlock.Tui`
- [ ] Agregar paquete Terminal.Gui: `dotnet add package Terminal.Gui`
- [ ] Crear `Program.cs` con Hello World
- [ ] Ejecutar y verificar: `dotnet run` muestra ventana TUI
- [ ] Crear carpeta `tests/FocusBlock.Tests.Unit/`
- [ ] Crear carpetas `docs/` y `progreso/`

---

## Fase 1: Esqueleto TUI

### Feature 1.1: Orquestación de la App
- [ ] Escribir test: `FocusBlockApp_CreatesMainWindow` (RED)
- [ ] Crear `App.cs` con clase `FocusBlockApp` (GREEN)
- [ ] Refactorizar si es necesario

### Feature 1.2: Ventana Principal con Menú
- [ ] Escribir test: `MainWindow_HasMenuBarAndStatusBar` (RED)
- [ ] Crear `Views/MainWindow.cs` con MenuBar, StatusBar (GREEN)
- [ ] Implementar items de menú: Block, View, Settings, Help

### Feature 1.3: Navegación entre Vistas
- [ ] Escribir test: `MainWindow_MenuNavigatesToViews` (RED)
- [ ] Implementar lógica de cambio de vistas (GREEN)

### Feature 1.4: Vista de Estado
- [ ] Escribir test: `StatusView_DisplaysDaemonStatus` (RED)
- [ ] Crear `Views/StatusView.cs` (GREEN)
- [ ] Implementar método `RefreshStatus()`

### Feature 1.5: Lista de Bloqueos
- [ ] Escribir test: `BlockListView_DisplaysListOfApps` (RED)
- [ ] Crear `Views/BlockListView.cs` (GREEN)

### Feature 1.6: Agregar Bloqueo (Placeholder)
- [ ] Escribir test: `AddBlockView_ShowsFormFields` (RED)
- [ ] Crear `Views/AddBlockView.cs` con formulario placeholder (GREEN)

---

## Fase 2: Sistema de Configuración

### Feature 2.1: Modelos de Configuración
- [ ] Escribir test: `AppConfig_DefaultValues_AreCorrect` (RED)
- [ ] Crear proyecto `FocusBlock.Contracts/` (GREEN)
- [ ] Crear modelos: `AppConfig`, `BlockRuleConfig`, `SecurityConfig`
- [ ] Agregar a solución

### Feature 2.2: Serialización JSON
- [ ] Escribir test: `AppConfig_SerializeDeserialize_RoundTrips` (RED)
- [ ] Implementar serialización con `System.Text.Json` (GREEN)
- [ ] Manejar converter personalizado para `TimeOnly` si se necesita

### Feature 2.3: Servicio de Configuración
- [ ] Escribir test: `ConfigService_LoadAsync_ReturnsDefaults_WhenFileMissing` (RED)
- [ ] Escribir test: `ConfigService_SaveAsync_PersistsToFile` (RED)
- [ ] Crear `Services/ConfigService.cs` (GREEN)
- [ ] Implementar carga/guardado con async/await

### Feature 2.4: Hashing de Contraseñas
- [ ] Escribir test: `AuthService_HashPassword_ReturnsHashAndSalt` (RED)
- [ ] Escribir test: `AuthService_VerifyPassword_ReturnsTrue_WhenCorrect` (RED)
- [ ] Escribir test: `AuthService_VerifyPassword_ReturnsFalse_WhenWrong` (RED)
- [ ] Agregar paquete Argon2: `dotnet add package Konscious.Security.Cryptography.Argon2`
- [ ] Crear `Services/AuthService.cs` (GREEN)
- [ ] Implementar `HashPassword()` y `VerifyPassword()`

---

## Fase 3: Daemon y Monitor de Procesos

### Feature 3.1: Worker BackgroundService
- [ ] Escribir test: `Worker_StartsAndRunsUntilCancelled` (RED)
- [ ] Crear proyecto `FocusBlock.Daemon/` (GREEN)
- [ ] Agregar `Worker.cs` como `BackgroundService`
- [ ] Agregar a solución

### Feature 3.2: Monitor de Procesos
- [ ] Escribir test: `ProcessMonitor_GetRunningProcesses_ReturnsList` (RED)
- [ ] Crear `Services/ProcessMonitor.cs` (GREEN)
- [ ] Implementar escaneo de `/proc`
- [ ] Escribir test: `ProcessMonitor_ExtractProcessName_ParsesStatus` (RED)
- [ ] Implementar helper `ExtractProcessName()`

### Feature 3.3: Ejecutor de Bloqueos
- [ ] Escribir test: `BlockEnforcer_KillProcess_SendsSigterm` (RED)
- [ ] Crear `Services/BlockEnforcer.cs` (GREEN)
- [ ] Implementar P/Invoke `kill()` syscall
- [ ] Escribir test: `BlockEnforcer_EscalatesToSigkill_WhenSigtermFails` (RED)
- [ ] Implementar escalación SIGTERM → SIGKILL

### Feature 3.4: Servidor IPC
- [ ] Escribir test: `IpcServer_HandlesStatusRequest` (RED)
- [ ] Crear `Services/IpcServer.cs` (GREEN)
- [ ] Implementar escuchador Unix socket
- [ ] Escribir test: `IpcServer_HandlesAddBlockRequest` (RED)
- [ ] Implementar enrutamiento de mensajes

### Feature 3.5: Integración Docker
- [ ] Crear `config/docker/Dockerfile.daemon` (GREEN)
- [ ] Crear `config/docker/docker-compose.yml` (GREEN)
- [ ] Probar daemon en Docker: `docker-compose up daemon`
- [ ] Verificar acceso a `/proc` en contenedor

---

## Fase 4: Núcleo del Bloqueador

### Feature 4.1: Motor de Reglas
- [ ] Escribir test: `BlockEngine_Evaluate_ReturnsBlock_WhenInSchedule` (RED)
- [ ] Escribir test: `BlockEngine_Evaluate_ReturnsNoBlock_WhenOutsideSchedule` (RED)
- [ ] Escribir test: `BlockEngine_Evaluate_ReturnsNoBlock_WhenRuleDisabled` (RED)
- [ ] Crear `Services/BlockEngine.cs` (GREEN)
- [ ] Implementar lógica de evaluación de reglas

### Feature 4.2: Gestor de Cooldown
- [ ] Escribir test: `CooldownManager_StartCooldown_SetsExpiry` (RED)
- [ ] Escribir test: `CooldownManager_IsOnCooldown_ReturnsTrue_WhenActive` (RED)
- [ ] Escribir test: `CooldownManager_IsOnCooldown_ReturnsFalse_WhenExpired` (RED)
- [ ] Crear `Services/CooldownManager.cs` (GREEN)
- [ ] Implementar tracking thread-safe de cooldowns

### Feature 4.3: Sistema de Challenges
- [ ] Escribir test: `ChallengeSystem_GenerateChallenge_ReturnsRandomText` (RED)
- [ ] Crear `Services/ChallengeSystem.cs` (GREEN)
- [ ] Escribir test: `ChallengeDialog_ShowsChallenge_AndValidatesInput` (RED)
- [ ] Crear `ChallengeDialog` TUI (GREEN)

### Feature 4.4: Flujo de Early Stop
- [ ] Escribir test: `BlockEngine_TryEarlyStop_ReturnsTrue_WhenPasswordCorrect` (RED)
- [ ] Escribir test: `BlockEngine_TryEarlyStop_ReturnsFalse_WhenPasswordWrong` (RED)
- [ ] Implementar `TryEarlyStop()` en BlockEngine (GREEN)
- [ ] Conectar cooldown + challenge + verificación de contraseña

---

## Fase 5: Seguridad Anti-Bypass

### Feature 5.1: Protector de Archivos
- [ ] Escribir test: `FileProtector_LockFile_SetsImmutableFlag` (RED)
- [ ] Crear `Services/FileProtector.cs` (GREEN)
- [ ] Implementar P/Invoke para `ioctl` (chattr +i)
- [ ] Escribir test: `FileProtector_UnlockFile_ClearsImmutableFlag` (RED)

### Feature 5.2: Gestor de Seguridad
- [ ] Escribir test: `SecurityManager_LockConfig_DuringActiveBlock` (RED)
- [ ] Crear `Services/SecurityManager.cs` (GREEN)
- [ ] Implementar bloqueo de config durante bloques activos
- [ ] Escribir test: `SecurityManager_TryUnlockConfig_RequiresPassword` (RED)

### Feature 5.3: Test de Integración
- [ ] Escribir test integración: `SecurityManager_WithRealFile_LocksAndUnlocks` (RED)
- [ ] Probar con filesystem real (GREEN)
- [ ] Verificar que `chattr +i` previene eliminación

---

## Fase 6: Métricas y Analytics

### Feature 6.1: Configuración SQLite
- [ ] Escribir test: `MetricsCollector_RecordBlockEvent_InsertsRow` (RED)
- [ ] Crear `Services/MetricsCollector.cs` (GREEN)
- [ ] Implementar inicialización de esquema SQLite
- [ ] Agregar paquete Dapper

### Feature 6.2: Grabación de Métricas
- [ ] Escribir test: `MetricsCollector_RecordUsage_UpdatesDailyTotal` (RED)
- [ ] Implementar UPSERT para uso diario (GREEN)
- [ ] Escribir test: `MetricsCollector_GetDailyStats_ReturnsWeekData` (RED)

### Feature 6.3: Charts en TUI
- [ ] Escribir test: `MetricsView_RenderBarChart_DisplaysData` (RED)
- [ ] Crear `Views/MetricsView.cs` (GREEN)
- [ ] Integrar Spectre.Console para charts ASCII

### Feature 6.4: Test de Integración
- [ ] Escribir test integración: `MetricsCollector_WithRealSQLite_PersistsAndQueries` (RED)
- [ ] Probar con TestContainer o DB temporal (GREEN)

---

## Fase 7: Pulido y Testing

### Feature 7.1: Manejo de Errores
- [ ] Escribir test: `IpcClient_ThrowsWhenDaemonNotRunning` (RED)
- [ ] Implementar degradación elegante (GREEN)
- [ ] Escribir test: `ConfigService_HandlesCorruptJson` (RED)
- [ ] Implementar fallback a valores por defecto

### Feature 7.2: Casos Borde
- [ ] Escribir test: `ProcessMonitor_HandlesMissingProcEntry` (RED)
- [ ] Escribir test: `BlockEngine_HandlesCircularCooldown` (RED)
- [ ] Manejar todos los casos borde

### Feature 7.3: Suite Completa de Tests
- [ ] Ejecutar todos los unitarios: `dotnet test --filter "Category=Unit"`
- [ ] Ejecutar todas las integraciones: `dotnet test --filter "Category=Integration"`
- [ ] Verificar cobertura > 80% unit, > 60% integración
- [ ] Corregir tests fallidos

### Feature 7.4: Documentación
- [ ] Actualizar `docs/referencia/arquitectura.md`
- [ ] Actualizar `docs/referencia/plan-desarrollo.md`
- [ ] Actualizar `docs/aprendizaje/` para todas las fases
- [ ] Finalizar `README.md`

### Feature 7.5: Docker Final
- [ ] Probar `docker-compose up` completo
- [ ] Verificar que daemon inicia y responde
- [ ] Probar que TUI conecta con daemon Docker
