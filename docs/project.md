# Proyecto — FocusBlock

> Qué es, qué alcance tiene, cómo se trabaja y cómo se prueba.
> Estado actual → `handoff.md` · Fases → `phase-plan.md` · Arquitectura → `architecture.md`

## Qué es

FocusBlock es un proyecto educativo para dominar C#/.NET 10 construyendo una herramienta
real de bloqueo de aplicaciones en Arch Linux. Una TUI corre como usuario regular y se
comunica por Unix socket con un daemon root que vigila procesos, aplica horarios y registra
métricas.

## Resultado buscado

Evidencia práctica de dominio en C# moderno, arquitectura cliente-servidor con IPC,
programación de sistemas Linux, persistencia con SQLite, testing por seams y seguridad
básica de contraseñas.

## Alcance funcional

| Módulo | Propósito | Aprendizaje |
|---|---|---|
| TUI | Panel de control en terminal | Terminal.Gui v2, navegación, formularios |
| Config | Reglas y horarios persistentes | Modelos, JSON, async, hashing |
| Daemon | Monitoreo y ejecución | `BackgroundService`, `/proc`, P/Invoke |
| Blocker | Decisión, cooldown y early stop | Concurrencia, thread-safety |
| Anti-bypass | Protección contra desactivación | `chattr +i`, archivos inmutables |
| Métricas | Estadísticas de uso | SQLite, Dapper, queries |

## Fuera de alcance

- Interfaz gráfica.
- Microservicios o arquitectura distribuida.
- Soporte multi-plataforma: solo Linux/Arch.
- Optimización de rendimiento antes de tener el flujo completo.
- Autenticación de red o multi-usuario.

Primero se domina el monolito modular TUI + daemon. Después se evalúa crecer.

## Competencias

| Área | Qué se practica |
|---|---|
| C# / .NET | Records, LINQ, async/await, DI, SOLID, nullable reference types |
| Linux | `/proc`, señales por P/Invoke, `chattr +i`, systemd, Unix sockets |
| Datos | SQLite, Dapper, queries, UPSERT, índices |
| Calidad | TDD estricto, pirámide de testing, seams, Clean Code |
| DevOps | Git con conventional commits, .NET CLI, CI |

## Método de trabajo

- **TDD estricto:** RED → GREEN → REFACTOR. Nunca implementación sin test previo.
- **Un commit por feature** en `dev`. Las PRs `dev → main` se abren al cerrar la fase, una
  de código y otra de docs. CI (`Lint`, `Build`, `Test`) es obligatorio.
- **Merge** con `--merge` o `--squash`; nunca `--rebase` si la historia divergió.
- **Nombres de test:** `Method_Condition_ExpectedResult`.
- **Ubicación:** `tests/FocusBlock.Tests.Unit/`, layout plano, un archivo por clase bajo test.

## Testing

La pirámide prioriza tests rápidos y aislados; solo lo que toca el kernel va al host real.

- **Unitarios:** muchos, deterministas, con fakes en los seams.
- **Integración:** host real para `/proc`, sockets, SQLite y `chattr`; categoría `Integration`
  y se saltan cuando faltan privilegios.
- **Funcionales:** solo flujos críticos, como el ciclo de vida del daemon.

| Feature | Unitario | Integración | Funcional |
|---|---|---|---|
| Carga/guardado de config | JSON round-trip | Archivo real | — |
| Reglas de bloqueo | Todas las rutas lógicas | — | — |
| Monitor de procesos | Parseo de `/proc` con fuente fake | `/proc` real | Daemon en host |
| Protocolo IPC | Serialización | Socket real | Daemon completo |
| Anti-bypass | Hashing | `chattr +i` | Flujo completo |
| Métricas | Queries SQLite | DB real | Pipeline completo |

### Puntos de inyección (seams)

Las dependencias del sistema operativo (filesystem, `/proc`, señales, `chattr`, reloj,
sockets, SQLite) no se filtran a los tests unitarios. Se aíslan detrás de una interfaz o
parámetro, y el comportamiento real se cubre en integración.

| Componente | Dependencia | Seam | Doble (unit) | Test real |
|---|---|---|---|---|
| `ConfigService` | Filesystem | Ruta por constructor | Ruta temporal | Archivo real |
| `AuthService` | CSPRNG | Sal inyectada | Sal fija | — |
| `ProcessMonitor` | `/proc` | `IProcessSource` | Fuente en memoria | `/proc` real |
| `BlockEnforcer` | `kill()` | `ISignalSender` | Sender que registra | Proceso `sleep` |
| `IpcServer` | Unix socket | Ruta + `IIpcRequestHandler` | Path temporal + handler fake | Cliente/servidor real |
| `BlockEngine` | Reloj | `now` por parámetro | — | — |
| `CooldownManager` | Reloj | `now` por parámetro | — | — |
| `FileProtector` | `chattr +i` | `IFileAttributes` | Fake de flags | FS real con root |
| `MetricsCollector` | SQLite | Connection string | SQLite temporal | Archivo real |
| `IpcClient` | Socket | Ruta + `IIpcClient` | Servidor fake | Daemon real |

## Deployment

Solo para instalación; no se ejecuta durante el desarrollo.

```bash
# Build release
dotnet publish -c Release -r linux-x64 --self-contained

# Daemon
sudo cp src/FocusBlock.Daemon/bin/Release/net10.0/linux-x64/publish/FocusBlock.Daemon /opt/focusblock/
sudo cp config/focusblock-daemon.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now focusblock

# TUI
sudo cp src/FocusBlock.Tui/bin/Release/net10.0/linux-x64/publish/FocusBlock.Tui /usr/local/bin/

# Directorios requeridos
sudo mkdir -p /etc/focusblock /var/lib/focusblock /run/focusblock
sudo cp config/focusblock.json /etc/focusblock/
sudo chown -R root:root /etc/focusblock /var/lib/focusblock
```

La TUI se ejecuta como usuario regular: `focusblock`.
