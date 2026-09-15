# Decisiones de Arquitectura (ADR)

Los ADR (*Architecture Decision Records*) registran las decisiones técnicas importantes y su **PORQUÉ**, para que código y contexto no se pierdan entre sesiones. Formato inspirado en Michael Nygard.

## Archivos

| ADR | Decisión |
|-----|----------|
| `ADR-001-terminal-gui.md` | Framework TUI: Terminal.Gui v2 |
| `ADR-002-ipc-unix-socket.md` | IPC por Unix domain socket |
| `ADR-003-sqlite-dapper.md` | Persistencia SQLite + Dapper |
| `ADR-004-argon2id.md` | Hashing Argon2id |
| `ADR-005-config-json.md` | Config en JSON |
| `ADR-006-proc-scan.md` | Escaneo `/proc` directo |
| `ADR-007-systemd.md` | Servicio systemd |
| `ADR-008-testing-stack.md` | Stack de testing |
| `ADR-009-testing-seams.md` | Testing por puntos de inyección (seams) |
| `ADR-010-dotnet-10-target.md` | Target .NET 10 |
| `ADR-011-dotnet-driver.md` | Driver DOTNET de Terminal.Gui |
| `ADR-012-blocking-strategies.md` | Estrategias de bloqueo: Schedule-only por ahora |

## Cuándo escribir un ADR

- Decisión técnica con impacto duradero (framework, IPC, base de datos, seguridad).
- Cuando hay alternativas reales que descartamos.
- Cuando un futuro cambio podría "deshacer" la decisión sin contexto.

## Reglas

- Una decisión = un archivo `ADR-0NN-nombre.md`.
- Un ADR refleja la decisión **vigente hoy**: cuando una decisión cambia, el ADR se actualiza en el lugar (la historia vive en git, no duplicada en docs).
- Estados: `Propuesto` → `Aceptado` → `Obsoleto`.
- Usar `template-adr.md`.

## Resumen en architecture

`docs/architecture.md` mantiene una tabla resumen con links a estos ADRs. El detalle vive aquí.