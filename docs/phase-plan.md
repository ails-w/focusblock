# Plan de Fases — FocusBlock

> Índice maestro del desarrollo. Las fases cerradas se resumen; la próxima se detalla.
> Conceptos → `docs/learning/phase-NN-name.md` · Estado mutable → `docs/handoff.md`

## Reglas globales

- TDD estricto: RED → GREEN → REFACTOR. Test antes de implementar.
- Código en inglés, docs en español neutro.
- Commits convencionales; un commit por feature en `dev`.
- PRs `dev → main` al cerrar la fase: una de código y una de docs.
- Protocolo de documentación (qué se actualiza y cuándo) → `AGENTS.md`.

## Resumen

| # | Nombre | Estado | Tests al cierre | Conceptos |
|---|---|---|---|---|
| 0 | Setup | ✅ | 0 | `learning/phase-00-setup.md` |
| 1 | Esqueleto TUI | ✅ | 6 | `learning/phase-01-tui.md` |
| 2 | Configuración | ✅ | 13 | `learning/phase-02-config.md` |
| 3 | Daemon y monitor | ✅ | 32 | `learning/phase-03-daemon.md` |
| 4 | Núcleo bloqueador | ✅ | 91 | `learning/phase-04-blocker.md` |
| 5 | Anti-bypass | ⏳ | — | `learning/phase-05-antibypass.md` |
| 6 | Métricas | ⏳ | — | `learning/phase-06-metrics.md` |
| 7 | Pulido y testing | ⏳ | — | `learning/phase-07-polish.md` |

---

## Fase 0 — Setup ✅

**Objetivo:** solución .NET, proyecto TUI y base de tests.

**Scope:** solución `.slnx`, `FocusBlock.Tui` con Terminal.Gui v2, `FocusBlock.Tests.Unit`
con xUnit + Moq + FluentAssertions.

**Conceptos:** API por instancia · formato `.slnx` · driver DOTNET en Linux.

**Salida:** `dotnet run` muestra la ventana y `dotnet test` corre desde la raíz.

---

## Fase 1 — Esqueleto TUI ✅

**Objetivo:** estructura navegable de la TUI.

**Scope:** `FocusBlockApp`, `MainWindow` con menú y barra de estado, navegación entre
`StatusView`, `BlockListView` y `AddBlockView`.

**Conceptos:** composición y layout · navegación · DI y Composition Root ·
container/presentational · records y `ObservableCollection` · tests aislados.

**Salida:** la TUI navega con datos placeholder y los tests de orquestación están verdes.

---

## Fase 2 — Configuración ✅

**Objetivo:** config persistente y contraseña protegida.

**Scope:** `FocusBlock.Contracts` con `AppConfig`, `BlockRuleConfig` y `SecurityConfig`;
serialización JSON; `ConfigService` async; `AuthService` con Argon2id.

**Conceptos:** modelos y defaults · JSON round-trip · async/await · hashing seguro
(Argon2id, sal, costo y tiempo constante).

**Salida:** la config sobrevive al reinicio y hash/verify pasan sus tests.

---

## Fase 3 — Daemon y monitor ✅

**Objetivo:** daemon root que observa procesos, actúa sobre ellos y sirve IPC.

**Scope:** `Worker` (`BackgroundService`), `ProcessMonitor` sobre `/proc`,
`BlockEnforcer` con SIGTERM → SIGKILL, `IpcServer` en Unix socket.

**Conceptos:** ciclo de vida del worker · escaneo de `/proc` · P/Invoke y señales ·
Unix domain sockets y framing.

**Puntos de inyección:** `IProcessSource`, `ISignalSender`, ruta del socket y tick.

**Salida:** componentes testeados. El cableado end-to-end se difiere a la fase 4.

---

## Fase 4 — Núcleo del bloqueador ✅

**Objetivo:** decidir qué bloquear, ejecutarlo y permitir un early stop controlado.

**Scope:** `BlockEngine` (reglas puras), `CooldownManager` thread-safe,
`ChallengeSystem`, flujo de early stop con contraseña y `FocusBlock.Core` para compartir
`IPasswordVerifier` y `ConfigService`.

**Conceptos:** evaluación de reglas · cooldown y concurrencia · bucle de bloqueo ·
contrato IPC · early stop (fricción + contraseña) · inversión de dependencias.

**Nota:** `Channel<T>` no aplicó; el bucle es secuencial. Candidato para la fase 6.

**Salida:** el daemon bloquea de punta a punta, el early stop arranca cooldown y los 91
tests están verdes.

---

## Fase 5 — Seguridad anti-bypass ⏳

**Objetivo:** impedir que el usuario desactive el bloqueo editando archivos o servicios.

**Scope:** `FileProtector` con `chattr +i` (ioctl por P/Invoke); `SecurityManager` que
bloquea la config durante un bloqueo activo; tests de integración con filesystem real.

**Fuera de scope:** JWT, autenticación de red, cifrado de disco.

**Conceptos:** `chattr +i` e `ioctl` · P/Invoke avanzado.

**Punto de inyección:** `IFileAttributes`.

**Salida:** la config queda inmutable durante el bloqueo, desbloquear exige contraseña y
los tests de integración están verdes.

### Features

**5.1 Protector de archivos**
- [ ] Test RED: `FileProtector_LockFile_SetsImmutableFlag`.
- [ ] `Services/FileProtector.cs` con P/Invoke para `ioctl`.
- [ ] Test RED: `FileProtector_UnlockFile_ClearsImmutableFlag`.

**5.2 Gestor de seguridad**
- [ ] Test RED: `SecurityManager_LockConfig_DuringActiveBlock`.
- [ ] `Services/SecurityManager.cs`.
- [ ] Test RED: `SecurityManager_TryUnlockConfig_RequiresPassword`.

**5.3 Integración**
- [ ] Test `SecurityManager_WithRealFile_LocksAndUnlocks` con filesystem real.
- [ ] Verificar que `chattr +i` impide eliminar la config.

---

## Fase 6 — Métricas ⏳

**Objetivo:** registrar bloqueos y uso diario en SQLite, con gráficos en la TUI.

**Scope:** esquema SQLite (`block_events`, `app_usage`, `bypass_attempts`),
`MetricsCollector` con Dapper, UPSERT de uso diario y `MetricsView` con Spectre.Console.

**Fuera de scope:** analytics avanzado, dashboard web, exportación.

**Conceptos:** SQLite + Dapper · UPSERT y queries agregadas · charts ASCII.

**Punto de inyección:** conexión SQLite de `MetricsCollector`.

**Salida:** eventos grabados, uso diario acumulado y estadísticas semanales visibles.

---

## Fase 7 — Pulido y testing ⏳

**Objetivo:** robustez, casos borde, cobertura y documentación final.

**Scope:** manejo de errores (daemon caído, JSON corrupto), casos borde, suite completa y
documentación final. Esta fase no agrega features.

**Conceptos:** degradación elegante · cobertura y mutation testing.

**Salida:** suite 100 % verde, cobertura > 80 % unit y > 60 % integración, docs finales.
