# Arquitectura — FocusBlock

## Visión general

```text
Usuario
  │
  ▼
TUI (usuario regular) ── Unix socket ──▶ Daemon (root, systemd)
  │                                        ├── /proc
  │                                        ├── SIGTERM → SIGKILL
  │                                        ├── SQLite
  │                                        └── chattr +i
  └── config JSON compartida ◀─────────────┘
```

La TUI corre sin privilegios y el daemon como root, porque `/proc`, `kill()` y `chattr +i`
lo requieren. El Unix socket los desacopla y permite desarrollar la UI sin escalar permisos.
El daemon corre nativo en el host: necesita el PID namespace real y root
(`ADR-009`).

## Componentes

| Proyecto | Rol | Depende de |
|---|---|---|
| `FocusBlock.Tui` | Interfaz, cliente IPC y flujo de early stop | `Contracts`, `Core` |
| `FocusBlock.Core` | Lógica compartida: `AuthService`, `ConfigService`, `IPasswordVerifier` | `Contracts` |
| `FocusBlock.Daemon` | Worker, motor de reglas, enforcement y servidor IPC | `Contracts`, `Core` |
| `FocusBlock.Contracts` | Modelos de config y contrato IPC | — |
| `FocusBlock.Tests.Unit` | Tests unitarios de todos los proyectos | Todos |

La dirección importa: TUI y Daemon no se referencian entre sí. La decisión está en
`ADR-013`.

## Estructura de carpetas

```text
FocusBlock/
├── FocusBlock.slnx
├── Directory.Build.props
├── config/
│   ├── focusblock.json
│   └── focusblock-daemon.service
├── src/
│   ├── FocusBlock.Contracts/
│   │   ├── AppConfig.cs · BlockRuleConfig.cs · SecurityConfig.cs
│   │   ├── ConfigSerializer.cs
│   │   └── IpcProtocol.cs · IpcJson.cs
│   ├── FocusBlock.Core/
│   │   ├── AuthService.cs · IPasswordVerifier.cs
│   │   └── ConfigService.cs
│   ├── FocusBlock.Daemon/
│   │   ├── Program.cs · Worker.cs
│   │   └── Services/
│   │       ├── BlockEngine.cs · BlockCoordinator.cs · CooldownManager.cs
│   │       ├── ProcessMonitor.cs · ProcessInfo.cs · IProcessSource.cs · ProcProcessSource.cs
│   │       ├── BlockEnforcer.cs · Signal.cs · ISignalSender.cs · LibcSignalSender.cs
│   │       └── IpcServer.cs · IpcServerHostedService.cs · IIpcRequestHandler.cs
│   │           · DaemonRequestHandler.cs
│   └── FocusBlock.Tui/
│       ├── Program.cs · App.cs
│       ├── Models/DaemonStatus.cs
│       ├── Services/
│       │   ├── IpcClient.cs · IIpcClient.cs
│       │   ├── EarlyStopService.cs · IEarlyStopPrompt.cs · TerminalEarlyStopPrompt.cs
│       │   └── ChallengeSystem.cs
│       └── Views/
│           ├── MainWindow.cs · StatusView.cs · BlockListView.cs · AddBlockView.cs
│           └── ChallengeDialog.cs · PasswordDialog.cs · AppNameDialog.cs
├── tests/FocusBlock.Tests.Unit/        # Un archivo por clase bajo test
└── docs/
    ├── index.md · project.md · architecture.md
    ├── phase-plan.md · handoff.md
    ├── learning/                       # Conceptos, glosario y método
    └── adr/                            # Decisiones (ADR-001 a ADR-013)
```

## Paquetes NuGet

| Paquete | Proyecto | Uso |
|---|---|---|
| `Terminal.Gui` | Tui | Framework TUI |
| `Spectre.Console` | Tui | Charts y sparklines |
| `Microsoft.Data.Sqlite` | Daemon | Proveedor SQLite |
| `Dapper` | Daemon | Micro-ORM |
| `System.Text.Json` | Ambos | Serialización |
| `Konscious.Security.Cryptography.Argon2` | Core | Hashing de contraseñas |
| `xunit`, `Moq`, `FluentAssertions` | Tests | Stack de testing |

## Protocolo IPC

Unix socket en `/run/focusblock/focusblock.sock`. Cada mensaje es **una línea JSON** con
nombres en `snake_case`, delimitada por `\n`.

```text
{"type":"status"}                                      → {"type":"status_response","detail":"..."}
{"type":"add_block","app_name":"firefox","schedule":"09:00-17:00"}
{"type":"remove_block","app_name":"firefox"}
{"type":"force_stop","app_name":"firefox","password":"..."}
                                                       → {"type":"ok"} | {"type":"error","detail":"..."}
```

`add_block` y `remove_block` todavía no tienen handler real: el daemon responde `error`.
Los campos ricos de estado (`active_blocks`, `daemon_uptime`) están pendientes para la
fase 6.

### Contrato

```cs
public enum MessageType
{
    Status, StatusResponse, AddBlock, RemoveBlock, ForceStop, Ok, Error,
}

public record IpcMessage(
    MessageType Type,
    string? AppName = null,
    string? Schedule = null,
    string? Detail = null,
    string? Password = null);
```

`IpcJson.Options` es una única instancia compartida en `Contracts`: `snake_case` y enums
como string. La contraseña nunca se hace eco ni se loguea.

## Esquema SQLite

```sql
-- /var/lib/focusblock/metrics.db

CREATE TABLE IF NOT EXISTS block_events (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    app_name    TEXT NOT NULL,
    started_at  TEXT NOT NULL,  -- ISO 8601
    ended_at    TEXT,           -- NULL = aún activo
    reason      TEXT NOT NULL,  -- 'scheduled', 'manual', 'cooldown'
    killed_with TEXT            -- 'SIGTERM', 'SIGKILL', NULL
);

CREATE TABLE IF NOT EXISTS app_usage (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    app_name     TEXT NOT NULL,
    date         TEXT NOT NULL,  -- YYYY-MM-DD
    seconds_used INTEGER NOT NULL DEFAULT 0,
    UNIQUE(app_name, date)
);

CREATE TABLE IF NOT EXISTS bypass_attempts (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    app_name     TEXT NOT NULL,
    attempted_at TEXT NOT NULL,
    method       TEXT NOT NULL,  -- 'early_stop', 'config_edit', 'service_stop'
    blocked      INTEGER NOT NULL DEFAULT 1
);
```

## Servicio systemd

```ini
# config/focusblock-daemon.service
[Unit]
Description=FocusBlock App Blocking Daemon
After=network.target

[Service]
Type=simple
ExecStart=/usr/bin/dotnet /opt/focusblock/FocusBlock.Daemon.dll
Restart=always
RestartSec=5
User=root

RuntimeDirectory=focusblock
RuntimeDirectoryMode=0755

StandardOutput=journal
StandardError=journal
SyslogIdentifier=focusblock

[Install]
WantedBy=multi-user.target
```

## Decisiones

El índice de ADRs vive en `docs/adr/README.md`. Cada decisión tiene contexto, alternativas
y consecuencias en su archivo.
