# Diagramas

Solo crear archivos aquí si es necesario documentar visualmente:
- Flujo de datos entre componentes
- Diagrama de secuencia (operaciones IPC)
- Diagrama de componentes (qué depende de qué)
- Flujo de bloqueo (detección → kill → métricas)

## Índice

| Fase | Archivo | Contenido |
|---|---|---|
| 0 — Setup | — | Sin diagrama: la fase no tiene flujo de runtime que valga la pena dibujar. |
| 1 — TUI | `phase-01-tui.md` | Estructura de archivos/conexiones y flujo de navegación del menú. |
| 2 — Config | `phase-02-config.md` | Stack de configuración (modelo → serializer → service → archivo) y ciclo Argon2id (hash/verify). |
| 3 — Daemon | `phase-03-daemon.md` | Componentes del daemon con sus seams y escalación SIGTERM → SIGKILL. |
| 4 — Bloqueador | `phase-04-blocker.md` | Una pasada de bloqueo (Worker → coordinator → enforcer) y el early stop TUI ↔ daemon. |

## Formatos

- **ASCII art** — Para diagramas simples en terminal/markdown
- **Mermaid** — Para diagramas más complejos (renderiza en GitHub)

## Regla

No crear diagramas por crear. Solo si algo es difícil de explicar con texto.
