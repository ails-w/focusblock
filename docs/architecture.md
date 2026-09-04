# Arquitectura del Proyecto

## Visión General del Sistema

```
┌─────────────────────────────────────────────────────────┐
│                    Terminal del Usuario                   │
│  ┌─────────────────────────────────────────────────────┐ │
│  │              FocusBlock TUI (usuario)                │ │
│  │  Terminal.Gui v2 — menús, estado, métricas, config  │ │
│  │  Corre como usuario regular, SIN root                │ │
│  └──────────────────────┬──────────────────────────────┘ │
│                         │ IPC (Unix socket)              │
│                         ▼                                │
│  ┌─────────────────────────────────────────────────────┐ │
│  │           focusblock-daemon (root)                   │ │
│  │  Servicio systemd — monitorea /proc, mata procesos   │ │
│  │  Contenedor Docker (opcional) — entorno aislado      │ │
│  │  Posee DB SQLite, archivos config, chattr +i        │ │
│  └──────────────────────┬──────────────────────────────┘ │
│                         │                                │
│                         ▼                                │
│  ┌─────────────────────────────────────────────────────┐ │
│  │  Escaneo /proc · SIGTERM/SIGKILL · chattr +i        │ │
│  └─────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

**¿Por qué esta separación?** La TUI corre como usuario regular (seguro, fácil de desarrollar). El daemon corre como root via systemd (requerido para `/proc` y `chattr +i`). IPC los mantiene desacoplados. Docker aísla el daemon para testing y deployment.

---

## Estructura de Carpetas

> Estructura **OBJETIVO**. Los proyectos `Daemon` y `Contracts` aún no existen como código (ver `docs/handoff.md`).

```
FocusBlock/
├── AGENTS.md                          # Fuente de contexto estático para IA
├── README.md                          # Portafolio (inglés)
├── FocusBlock.slnx                    # Archivo solución (.NET 10)
├── .gitignore
├── .editorconfig
│
├── src/
│   ├── FocusBlock.Tui/                # App TUI (espacio de usuario)
│   │   ├── Program.cs                 # Entry point
│   │   ├── App.cs                     # Configuración Terminal.Gui
│   │   ├── Views/
│   │   │   ├── MainWindow.cs          # Hub de navegación
│   │   │   ├── StatusView.cs          # Display de bloqueos activos
│   │   │   ├── BlockListView.cs       # Lista de apps bloqueadas
│   │   │   └── AddBlockView.cs        # Formulario nuevo bloqueo
│   │   └── Services/
│   │       ├── IpcClient.cs           # Cliente Unix socket
│   │       └── ConfigService.cs       # Carga/guardado config JSON
│   │
│   ├── FocusBlock.Daemon/             # Daemon root (servicio systemd)
│   │   ├── Program.cs                 # Entry point
│   │   ├── Worker.cs                  # BackgroundService
│   │   └── Services/
│   │       ├── ProcessMonitor.cs      # Escaneo /proc
│   │       ├── BlockEnforcer.cs       # Matar procesos
│   │       └── IpcServer.cs           # Servidor Unix socket
│   │
│   └── FocusBlock.Contracts/          # Modelos IPC compartidos
│       ├── IpcProtocol.cs             # Tipos de mensaje
│       └── BlockStatus.cs             # Enum de estado
│
├── tests/
│   └── FocusBlock.Tests.Unit/
│       ├── FocusBlock.Tests.Unit.csproj
│       ├── BlockRuleTests.cs
│       └── ProcessMonitorTests.cs
│
├── config/
│   ├── focusblock.json                # Config por defecto
│   ├── focusblock-daemon.service      # Archivo systemd
│   └── docker/
│       ├── Dockerfile.daemon          # Multi-stage build
│       ├── Dockerfile.dev             # Hot-reload desarrollo
│       └── docker-compose.yml         # Entorno desarrollo
│
└── docs/
    ├── index.md                       # Mapa de navegación
    ├── vision.md                      # Visión y alcance global
    ├── plan-fases.md                  # Fases con scope y criterios de salida
    ├── handoff.md                     # Estado mutable (fase activa, próximo paso)
    ├── arquitectura.md                # Este archivo
    ├── plan-desarrollo.md             # Estrategia testing + Docker + deploy
    ├── aprendizaje/                   # Conceptos aprendidos por fase
    ├── progreso-log/                  # Log histórico por fase
    ├── decisiones/                    # ADRs (on-demand)
    └── diagramas/                     # Diagramas si es necesario
```

---

## Paquetes NuGet

| Paquete | Proyecto | Propósito |
|---------|----------|-----------|
| `Terminal.Gui` | Tui | Framework TUI |
| `Spectre.Console` | Tui | Charts, sparklines |
| `Microsoft.Data.Sqlite` | Daemon | Proveedor SQLite ADO.NET |
| `Dapper` | Daemon | Micro-ORM para SQL |
| `System.Text.Json` | Ambos | Serialización JSON |
| `Konscious.Security.Cryptography.Argon2` | Config | Hashing de contraseñas |
| `xunit` | Tests | Framework de testing |
| `Moq` | Tests | Librería de mocks |
| `FluentAssertions` | Tests | Librería de assertions |
| `Testcontainers.Containers.Sqlite` | Tests.Integration | SQLite para tests |

---

## Protocolo IPC

Comunicación via **Unix domain socket** en `/run/focusblock/focusblock.sock`. Mensajes JSON delimitados por newline.

```
TUI (cliente)                          Daemon (servidor)
    │                                      │
    ├─── {"type":"status"} ──────────────▶│
    │◀── {"type":"status_response",       │
    │      "active_blocks":[...],         │
    │      "daemon_uptime":3600} ─────────┤
    │                                      │
    ├─── {"type":"add_block",             │
    │     "app_name":"firefox",           │
    │     "schedule":"09:00-17:00"} ─────▶│
    │◀── {"type":"ok"} ──────────────────┤
    │                                      │
    ├─── {"type":"remove_block",          │
    │     "app_name":"firefox"} ─────────▶│
    │◀── {"type":"ok"} ──────────────────┤
```

### Tipos de Mensaje

```csharp
public enum MessageType
{
    Status, StatusResponse,
    AddBlock, RemoveBlock, ListBlocks,
    ForceStop, BlockStatus,
    Ok, Error
}

public record IpcMessage(
    MessageType Type,
    string? Payload = null,
    string? RequestId = null
);
```

---

## Esquema SQLite

```sql
-- Base de datos: /var/lib/focusblock/metrics.db

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
    blocked      INTEGER NOT NULL DEFAULT 1  -- 1=prevenido, 0=exitoso
);
```

---

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

---

## Decisiones de Arquitectura

Resumen de decisiones — el detalle (contexto, alternativas, consecuencias) vive en los ADRs (`docs/adr/`).

| Decisión | ADR |
|----------|-----|
| Framework TUI: Terminal.Gui v2 | `docs/adr/ADR-001-terminal-gui.md` |
| IPC: Unix domain socket | `docs/adr/ADR-002-ipc-unix-socket.md` |
| BD: SQLite + Dapper | `docs/adr/ADR-003-sqlite-dapper.md` |
| Hashing: Argon2id | `docs/adr/ADR-004-argon2id.md` |
| Formato config: JSON | `docs/adr/ADR-005-config-json.md` |
| Escaneo: `/proc` directo | `docs/adr/ADR-006-proc-scan.md` |
| Servicio: systemd | `docs/adr/ADR-007-systemd.md` |
| Testing: xUnit + Moq + FluentAssertions | `docs/adr/ADR-008-testing-stack.md` |
| Docker: multi-stage | `docs/adr/ADR-009-docker-multistage.md` |
| Target: .NET 10 | `docs/adr/ADR-010-dotnet-10-target.md` |
| Driver: DOTNET (workaround) | `docs/adr/ADR-011-dotnet-driver.md` |
