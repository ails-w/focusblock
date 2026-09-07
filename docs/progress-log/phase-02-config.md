# Fase 2: Sistema de Configuración — Log

> Log HISTÓRICO de la fase. Se acumula, no se borra.
> Estado actual → `docs/handoff.md` · Conceptos → `docs/learning/phase-02-config.md`

## Estado

**Estado**: Completada
**Última Actualización**: 2026-09-07

## Objetivos

- Modelos de configuración, serialización JSON y hashing de contraseñas.
- ConfigService con carga/guardado async (defaults si falta archivo).
- AuthService con Argon2id (hash + verify).

## Progreso

- [x] Feature 2.1: Modelos de Configuración (2026-09-07)
- [x] Feature 2.2: Serialización JSON (2026-09-07)
- [x] Feature 2.3: Servicio de Configuración (2026-09-07)
- [x] Feature 2.4: Hashing de Contraseñas (2026-09-07)

## Tareas Completadas

### 2026-09-07 — Features 2.1 a 2.4
- **Descripción**: La app aprendió a definir, guardar, cargar y proteger su configuración. Nació el proyecto `FocusBlock.Contracts` (capa compartida TUI/Daemon).
- **Archivos**:
  - `src/FocusBlock.Contracts/` — proyecto + `AppConfig`, `BlockRuleConfig`, `SecurityConfig`, `ConfigSerializer`
  - `src/FocusBlock.Tui/Services/ConfigService.cs` — carga/guardado async
  - `src/FocusBlock.Tui/Services/AuthService.cs` — Argon2id hash/verify
  - `tests/.../AppConfigTests.cs`, `ConfigServiceTests.cs`, `AuthServiceTests.cs`
- **Tests**: 13 verdes.
- **Decisión**: Estrategia de bloqueo **Schedule-only** por ahora (ver `docs/adr/ADR-012-blocking-strategies.md`).

## Decisiones

1. **Solo estrategia Schedule (por ahora)** — mantener el proyecto simple y terminarlo antes. Las estrategias Racha/Tope se agregan después con un campo `Strategy` de default `Schedule` → retrocompatible, cero migración. Ver `docs/adr/ADR-012-blocking-strategies.md`.
2. **`ConfigSerializer` en Contracts** — la serialización del contrato pertenece al contrato; TUI y Daemon la reutilizarán.
3. **Params Argon2id: 16 MB / 3 iteraciones / 4 hilos** — balance seguridad/velocidad; costo ajustable.

## Problemas

1. **`TimeOnly` en JSON** — se temía que necesitara converter; en .NET 10 serializa **nativo** (desde .NET 7). *Solución*: ninguno, se verificó con el round-trip test.
2. **AGENTS.md desactualizado** — decía que `Contracts` era estructura objetivo. *Solución*: corregido (solo `Daemon` es objetivo) + se agregaron las reglas "commit por feature" y "bloques `cs`".

## Métricas

- Tests escritos: 13 (todos verdes)
- Tests pasando: 13/13 (100%)
- Cobertura: N/A
- Commits: 4 de features + 1 de reglas AGENTS

## Pendientes

- Pushear commits de la Fase 2 a `dev` y la PR a `main`.
- Fase 3 — Daemon y Monitor de Procesos.