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

- Idioma: Español. Nombres de carpetas/archivos en inglés.
- Formato: Markdown.
- Mantener `handoff.md` actualizado al iniciar/cerrar sesión.
- Al **cerrar** cada fase se crean o actualizan: `learning/phase-NN-name.md` (conceptos), `learning/glossary.md` (términos nuevos), `phase-plan.md` (estado y features) y `handoff.md`. Si hubo una decisión arquitectónica, se escribe o actualiza un ADR.
- ADRs: una decisión = un archivo en `docs/adr/`; cada ADR refleja la decisión vigente y se actualiza cuando la decisión cambia.