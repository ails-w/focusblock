# AGENTS.md — FocusBlock

> Contexto ESTÁTICO del proyecto. Persona, tono y protocolos de memoria viven en el AGENTS.md
> global (`~/.config/opencode/AGENTS.md`); este archivo NO los repite.
> Lo mutable (fase activa, próximo paso, riesgos) vive SOLO en `docs/handoff.md`.

## Overview

FocusBlock es una app TUI en C#/.NET 10 para bloquear aplicaciones en Arch Linux. Un daemon
root monitorea `/proc`, mata procesos bloqueados y cumple horarios. La TUI corre como usuario
regular y se comunica con el daemon por Unix domain socket. El dato arquitectónico que lo
explica todo: TUI (usuario) ↔ daemon (root) vía IPC, desacoplados.

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
- **NUNCA ejecutar**: `dotnet publish` — solo para deployment (ver `docs/project.md`)

## Mapa del repo

- Sistema y árbol de carpetas → `docs/architecture.md`.
- Alcance, método, testing y deployment → `docs/project.md`.
- Fases y criterios de salida → `docs/phase-plan.md`.
- Navegación completa → `docs/index.md`.

## Convenciones

- **Código en inglés, docs en español neutro** — *Reason:* el código es artefacto técnico;
  los docs son para el estudiante.
- **Español neutro:** imperativo estándar ("usa", "escribe", "verifica"). Sin voseo ni
  regionalismos. Código, identificadores, UI y commits en inglés.
- **PascalCase** clases, `_camelCase` campos privados, 4 espacios, 100 chars — *Reason:*
  estándar .NET.
- **Conventional commits**: `feat:`, `fix:`, `test:`, `docs:`, `refactor:` — *Reason:*
  historial legible y verificable.
- **Sin "Co-Authored-By" ni atribución IA** — *Reason:* regla global del usuario.
- **Docs: bloques de código con cabecera `cs`** — *Reason:* convención de render/Obsidian.

## TDD ESTRICTO (regla dura)

- NUNCA escribir implementación sin test previo. Ciclo: RED → GREEN → REFACTOR.
- Nombre de tests: `Method_Condition_ExpectedResult`.
- Si el usuario pide código sin test, primero escribir el test RED y luego el código mínimo
  para GREEN.
- Los tests son parte de la definición de "terminado" (DoD).

## Rol de aprendizaje (objetivo principal)

Este es un proyecto de **APRENDIZAJE**. El objetivo principal es que el estudiante aprenda,
no solo que el código funcione.

- Explica el **PORQUÉ técnico** con detalle (concepto → problema que resuelve → cómo se aplica).
- **Propón mejoras y alternativas** con trade-offs; no te limites a ejecutar.
- Usa **tono pedagógico** en cada explicación.
- **EXCEPCIÓN al contrato de respuestas cortas del global:** en modo enseñanza, expande. En
  ejecución pura, conciso.

## Límites / Do-nots

- NO tocar `bin/`, `obj/`, `packages.lock.json` a mano.
- NO escribir implementación sin test previo (TDD estricto).
- NO duplicar contenido entre archivos; si hay que copiar un párrafo, está mal ubicado.
- NO modificar `docs/handoff.md` salvo al cerrar sesión. Al iniciar, solo se lee.
- NO actualizar la documentación de una fase al inicio: se escribe al cerrarla.

## Gotchas conocidos

- **Driver ANSI de Terminal.Gui roto en Linux** (issues #4848, #4374) — *Reason:* la ventana
  renderiza vacía sin subviews; forzar `app.Init(DriverRegistry.Names.DOTNET)`.
- **API por instancia de Terminal.Gui v2** — *Reason:* la API estática legacy de v1 no compila
  contra v2.4.17 (CS0246/CS0117).
- **`.slnx` en .NET 10** — *Reason:* `dotnet new sln` genera el formato XML nuevo, no `.sln`.
- **Case-sensitivity** — *Reason:* las propiedades de librerías son PascalCase (`Y`, no `y`).

## Git + Definition of Done

- Commits: conventional commits en inglés.
- **Un commit por feature**: test RED → GREEN → refactor → commit en `dev`.
- **PR por fase**: al cerrar la fase se abren dos PRs `dev → main`, una de código y otra de
  docs. CI (`Lint`/`Build`/`Test`) es obligatorio.
- DoD de una feature: test RED que pasa (GREEN) + refactor + commit convencional en `dev`.
- Nunca añadir "Co-Authored-By" ni atribución IA.

## Protocolo de documentación (autoritativo)

La documentación de una fase se escribe **al cerrarla**, con lo que realmente se aprendió.
Al inicio de la fase solo se lee: no se crean archivos vacíos ni se especula con apuntes.

| Momento | Acción | Archivos |
|---|---|---|
| Inicio de fase | Solo lectura | `handoff.md`, `phase-plan.md`, learning previo |
| Durante la fase | Código y tests; sin docs de fase | — |
| Cierre de sesión | Sobrescribir el estado mutable | `handoff.md` |
| Cierre de fase | Documentar lo aprendido y el nuevo estado | `learning/phase-NN-name.md`, `glossary.md`, `phase-plan.md`, `handoff.md` |
| Decisión arquitectónica | Un ADR nuevo o actualizar el vigente | `docs/adr/` |
| Bug o gotcha | Sumarlo a las trampas de la fase; si sigue activo, también a `handoff.md` | learning, handoff |

Reglas:

- **No hay logs de progreso.** El historial vive en git; los bugs reales viven en "Trampas
  reales" de cada fase.
- **Una sola fuente por tema:** método y testing en `project.md`, sistema y árbol en
  `architecture.md`, decisiones en ADRs, conceptos en `learning/`, estado en `handoff.md`.
- **El glosario crece:** un término entra si es prerrequisito o se usa en 2+ fases.
- **Presupuesto:** cada fase de learning ≤ 280 líneas y 4-5 conceptos; el concepto ≤ 40.
- **Checklist de cierre de fase:**
  - [ ] `learning/phase-NN-name.md` escrito y enlazado desde `phase-plan.md`.
  - [ ] Términos nuevos en `glossary.md`.
  - [ ] `phase-plan.md` marca la fase como cerrada.
  - [ ] `handoff.md` apunta a la fase siguiente.
  - [ ] ADR creado o actualizado si hubo decisión.
  - [ ] Enlaces relativos verificados y PR de docs abierta.

## Tabla de punteros

| Área | Documento |
|---|---|
| **Estado actual** (LEER al iniciar, ACTUALIZAR al cerrar sesión) | `docs/handoff.md` |
| Proyecto: alcance, método, testing y deployment | `docs/project.md` |
| Sistema y estructura de carpetas | `docs/architecture.md` |
| Plan de fases y criterios de salida | `docs/phase-plan.md` |
| Método de estudio y prompt socrático | `docs/learning/README.md` |
| Glosario acumulativo | `docs/learning/glossary.md` |
| Decisiones | `docs/adr/README.md` |
| Navegación completa | `docs/index.md` |
