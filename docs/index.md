# Documentación de FocusBlock

Mapa de navegación de la documentación del proyecto. Este es el **índice único** — los agentes IA y las personas lo usan para ubicarse.

## Docs

| Área | Documento | Contenido |
|------|-----------|-----------|
| **Estado actual** (mutable) | `docs/handoff.md` | Fase activa, próximo paso, riesgos |
| **Proyecto** | `docs/project.md` | Alcance, competencias, método, testing y deployment |
| Plan de fases | `docs/phase-plan.md` | Fases con scope, conceptos, criterio de salida |
| **Aprendizaje** | `docs/learning/` | Conceptos por fase (`phase-NN-name.md`) + glosario |
| **Decisiones** | `docs/adr/` | ADRs (`ADR-0NN-*.md`) |
| Arquitectura | `docs/architecture.md` | Arquitectura, IPC, SQLite, systemd |

## Fuera de docs

| Documento | Contenido |
|-----------|-----------|
| `README.md` | Portafolio público (inglés) |
| `AGENTS.md` | Contexto estático para agentes IA |

## Reglas de docs

- Idioma: español neutro; nombres de carpetas y archivos en inglés.
- Formato: Markdown, con bloques de código `cs`.
- Cada documento tiene una sola responsabilidad; el índice no repite contenido.
- **Protocolo de actualización** (qué se toca y cuándo) → `AGENTS.md`.
- ADRs: una decisión = un archivo; cada ADR refleja la decisión vigente.