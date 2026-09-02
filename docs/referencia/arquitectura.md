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

```
FocusBlock/
├── AGENTS.md                          # Fuente de contexto para IA
├── CONTEXT.md                         # Bootstrap de sesión
├── README.md                          # Visión general del proyecto
├── .gitignore
├── .editorconfig
│
├── src/
│   ├── FocusBlock.sln                 # Archivo solución
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
├── docs/
│   ├── README.md                      # Índice de documentación
│   ├── aprendizaje/                   # Diario de aprendizaje
│   ├── referencia/                    # Referencia técnica
│   │   ├── arquitectura.md            # Este archivo
│   │   ├── plan-desarrollo.md         # Estrategia testing + Docker
│   │   └── phases.md                  # Fases con checkboxes
│   └── diagramas/                     # Diagramas si es necesario
│
└── progreso/                          # Tracking por fase
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

| Decisión | Elección | Alternativas | Por qué |
|----------|----------|-------------|---------|
| Framework TUI | Terminal.Gui v2 | Spectre.Console only | Widget set completo, nativo .NET |
| IPC | Unix domain socket | D-Bus, HTTP | Rápido, sin dependencias, nativo Linux |
| BD | SQLite + Dapper | EF Core, ADO.NET crudo | Simple, rápido, sin migraciones |
| Hashing contraseñas | Argon2id | bcrypt, PBKDF2 | Memory-hard, estándar moderno |
| Formato config | JSON | YAML, TOML | System.Text.Json integrado |
| Escaneo procesos | /proc directo | Process.GetProcesses() | Más confiable en Linux |
| Servicio | systemd | Docker only | Nativo de Arch |
| Testing | xUnit + Moq + FluentAssertions | NUnit, MSTest | Estándar industria, mejor mocking |
| Docker | Multi-stage build | Single stage | Imágenes producción pequeñas |
