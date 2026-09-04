# Documentación de FocusBlock

Mapa de navegación de la documentación del proyecto. Este es el **índice único** — los agentes IA y las personas lo usan para ubicarse.

## Docs

| Área | Documento | Contenido |
|------|-----------|-----------|
| **Estado actual** (mutable) | `docs/handoff.md` | Fase activa, próximo paso, riesgos |
| Visión | `docs/vision.md` | Alcance global, fuera-de-scope, competencias |
| Plan de fases | `docs/phase-plan.md` | Fases con scope, conceptos, criterio de salida |
| Aprendizaje | `docs/learning/` | Conceptos por fase (`phase-NN-name.md`) |
| Progreso (log) | `docs/progress-log/` | Historial por fase (`phase-NN-name.md`) |
| Decisiones | `docs/adr/` | ADRs (`ADR-0NN-*.md`) |
| Arquitectura | `docs/architecture.md` | Arquitectura, IPC, SQLite, systemd |
| Diagramas | `docs/diagrams/` | Diagramas ASCII/Mermaid |
| Desarrollo | `docs/development-plan.md` | Testing, Docker, deployment |

## Fuera de docs

| Documento | Contenido |
|-----------|-----------|
| `README.md` | Portafolio público (inglés) |
| `AGENTS.md` | Contexto estático para agentes IA |

## Reglas de docs

- Idioma: Español. Nombres de carpetas/archivos en inglés.
- Formato: Markdown.
- Mantener `handoff.md` actualizado al iniciar/cerrar sesión.
- Cada fase crea `learning/phase-NN-name.md` y `progress-log/phase-NN-name.md` al comenzar.
- ADRs: una decisión = un archivo en `docs/adr/`; nunca reescribir historial (superceder con ADR nuevo).