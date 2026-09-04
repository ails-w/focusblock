# AGENTS.md — FocusBlock

> Contexto ESTÁTICO del proyecto. Persona, tono y protocolos de memoria viven en el AGENTS.md global (`~/.config/opencode/AGENTS.md`); este archivo NO los repite.
> Lo mutable (fase activa, próximo paso, riesgos) vive SOLO en `docs/handoff.md`.

## Overview

FocusBlock es una app TUI en C#/.NET 10 para bloquear aplicaciones en Arch Linux. Un daemon root monitorea `/proc`, mata procesos bloqueados y cumple horarios. La TUI corre como usuario regular y se comunica con el daemon via Unix domain sockets. El dato arquitectónico que lo explica todo: TUI (usuario) ↔ daemon (root) vía IPC, desacoplados.

## Stack (fijado)

| Capa | Tecnología | Versión |
|---|---|---|
| Lenguaje | C# | 14 (SDK .NET 10.0.111) |
| Framework | Terminal.Gui (TUI) | v2.4.17 |
| Persistencia | SQLite + Dapper | — |
| Hashing | Argon2id (Konscious) | — |
| Testing | xUnit + Moq + FluentAssertions | 4.20.72 / 8.10.0 |
| Otro | System.Text.Json, Spectre.Console | — |

## Comandos

- Build: `dotnet build`
- Test: `dotnet test`
- Test individual: `dotnet test --filter "FullyQualifiedName~NombreTest"`
- Run TUI: `dotnet run --project src/FocusBlock.Tui`
- **NUNCA ejecutar**: `dotnet publish` — solo para deployment (ver `docs/development-plan.md`)

## Mapa del repo

Estructura completa (actual y objetivo) → `docs/architecture.md`. Los proyectos `Daemon` y `Contracts` son **estructura objetivo**, no código existente.

## Convenciones

- **Código en inglés, docs en español** — *Reason:* el código es artefacto técnico; los docs son para el estudiante.
- **PascalCase** clases, `_camelCase` campos privados, 4 espacios, 100 chars — *Reason:* estándar .NET.
- **Conventional commits**: `feat:`, `fix:`, `test:`, `docs:`, `refactor:` — *Reason:* historial legible y verificable.
- **Sin "Co-Authored-By" ni atribución IA** — *Reason:* regla global del usuario.

## TDD ESTRICTO (regla dura)

- NUNCA escribir implementación sin test previo. Ciclo: RED → GREEN → REFACTOR.
- Nombre de tests: `Method_Condition_ExpectedResult`.
- Si el usuario pide código sin test, primero escribir el test RED y luego el código mínimo para GREEN.
- Los tests son parte de la definición de "terminado" (DoD).

## Rol de aprendizaje (objetivo principal)

Este es un proyecto de **APRENDIZAJE**. El objetivo principal es que el estudiante aprenda, no solo que el código funcione.

- Explica el **PORQUÉ técnico** con detalle (concepto → problema que resuelve → cómo se aplica aquí).
- **Propón mejoras y alternativas** con trade-offs; no te limites a ejecutar.
- Usa **tono pedagógico** en cada explicación: enseña el concepto, no solo des el código.
- **EXCEPCIÓN al contrato de respuestas cortas del global**: en modo enseñanza, expande. En ejecución pura, conciso.

## Testing

- Framework: xUnit + Moq + FluentAssertions.
- Ubicación: `tests/FocusBlock.Tests.Unit/` (carpetas `Services/`, `Models/`).
- Pirámide: unit → integración (TestContainers) → funcional (Docker) — ver `docs/development-plan.md`.

## Límites / Do-nots

- NO tocar `bin/`, `obj/`, `packages.lock.json` a mano.
- NO escribir implementación sin test previo (TDD estricto).
- NO duplicar contenido entre archivos; si hay que copiar un párrafo, está mal ubicado.
- NO modificar `docs/handoff.md` salvo al iniciar/cerrar sesión.

## Gotchas conocidos

- **Driver ANSI de Terminal.Gui roto en Linux** (issues #4848, #4374) — *Reason:* la ventana renderiza vacía sin subviews; forzar `app.Init(DriverRegistry.Names.DOTNET)`.
- **API por instancia de Terminal.Gui v2** — *Reason:* la API estática legacy de v1 no compila contra v2.4.17 (CS0246/CS0117).
- **`.slnx` en .NET 10** — *Reason:* `dotnet new sln` genera el formato XML nuevo, no `.sln` clásico.
- **Case-sensitivity** — *Reason:* las propiedades de librerías son PascalCase (`Y`, no `y`).

## Git + Definition of Done

- Commits: conventional commits en inglés.
- DoD de una feature: test RED que pasa (GREEN) + refactor + aprendizaje documentado en `docs/learning/phase-NN-name.md` + `docs/handoff.md` actualizado.
- Nunca añadir "Co-Authored-By" ni atribución IA.

## Tabla de punteros

| Área | Documento |
|---|---|
| **Estado actual** (LEER al iniciar, ACTUALIZAR al cerrar) | `docs/handoff.md` |
| Mapa de navegación completo | `docs/index.md` |
| Plan de fases (scope + criterios de salida) | `docs/phase-plan.md` |

El resto de la documentación se navega desde `docs/index.md`.