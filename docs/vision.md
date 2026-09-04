# Visión del Proyecto — FocusBlock

FocusBlock es un proyecto educativo profesional para dominar C#/.NET 10 construyendo una herramienta real de bloqueo de aplicaciones en Arch Linux: una TUI que se comunica con un daemon root vía IPC para monitorear procesos, aplicar reglas de bloqueo y registrar métricas.

## Resultado buscado

Al finalizar el proyecto, debe servir como evidencia práctica de dominio en:

- C# moderno (.NET 10 / C# 14) aplicado a un producto real.
- Arquitectura cliente-servidor con IPC por Unix domain socket.
- Programación de sistemas en Linux (/proc, señales, chattr +i, systemd).
- Persistencia con SQLite + Dapper.
- Testing profesional (TDD estricto, xUnit + Moq + FluentAssertions).
- Contenerización con Docker multi-stage.
- Seguridad básica (hashing Argon2, anti-bypass).

## Alcance funcional

| Módulo | Propósito de negocio | Propósito de aprendizaje |
|---|---|---|
| TUI | Interfaz de usuario en terminal | Terminal.Gui v2, navegación, formularios |
| Config | Reglas de bloqueo y horarios | Modelos, JSON, async/await, hashing |
| Daemon | Monitoreo y ejecución de bloqueos | BackgroundService, /proc, P/Invoke |
| Blocker | Motor de reglas, cooldown, challenges | Concurrencia, thread-safety |
| Anti-bypass | Protección contra desactivación | chattr +i, seguridad de configuración |
| Métricas | Estadísticas de uso | SQLite, Dapper, queries |

## Fuera de alcance inicial

Estos temas son valiosos pero NO entran al inicio:

- Interfaz gráfica (GUI).
- Microservicios / arquitectura distribuida.
- Soporte multi-plataforma (solo Linux/Arch).
- Optimización prematura de rendimiento.
- Autenticación de red / multi-usuario.

Primero se domina el monolito modular TUI + daemon. Después se evalúa crecer.

## Competencias técnicas

### C# / .NET

- OOP, colecciones, LINQ.
- Records, pattern matching, nullable reference types.
- Async/await, concurrencia, colecciones concurrentes.
- Dependency Injection, SOLID.

### Programación de sistemas Linux

- Escaneo de `/proc`.
- Señales SIGTERM/SIGKILL (P/Invoke).
- `chattr +i` (ioctl).
- Servicio systemd.
- Unix domain sockets.

### Datos

- SQLite (Microsoft.Data.Sqlite).
- Dapper (micro-ORM).
- Queries, UPSERT, índices.

### Calidad

- Clean Code, SOLID aplicado con criterio.
- Arquitectura cliente-servidor desacoplada.
- TDD estricto (RED → GREEN → REFACTOR).
- Pirámide de testing (unit → integración → funcional).

### DevOps y herramientas

- Git con conventional commits.
- Docker multi-stage + Docker Compose.
- .NET CLI (build, test, publish).

## Principio de aprendizaje

La prioridad siempre es **entender el concepto antes de escribir la solución**. Código sin concepto es ruido. Cada fase declara sus conceptos de aprendizaje en `docs/phase-plan.md` y se documentan en `docs/learning/`. El TDD es estricto: test primero, implementación después.