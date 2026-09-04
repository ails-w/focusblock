# Fase 1: Esqueleto TUI — Log

> Log HISTÓRICO de la fase. Se acumula, no se borra.
> Estado actual → `docs/handoff.md` · Conceptos → `docs/learning/phase-01-tui.md`

## Estado

**Estado**: Completada
**Última Actualización**: 2026-09-04

## Objetivos

- Construir la estructura de la TUI: orquestación (`FocusBlockApp`), ventana principal con menú y navegación entre vistas placeholder.
- Lograr que toda la orquestación y las vistas sean testeables (unit tests).

## Progreso

- [x] Feature 1.1: Orquestación de la App (2026-09-04)
- [x] Feature 1.2: Ventana Principal con Menú (2026-09-04)
- [x] Feature 1.3: Navegación entre Vistas (2026-09-04)
- [x] Feature 1.4: Vista de Estado (2026-09-04)
- [x] Feature 1.5: Lista de Bloqueos (2026-09-04)
- [x] Feature 1.6: Agregar Bloqueo (Placeholder) (2026-09-04)

## Tareas Completadas

### 2026-09-04 — Features 1.1 a 1.6
- **Descripción**: La TUI pasó de Hello World a esqueleto navegable completo: `FocusBlockApp` (orquestación con DI), `MainWindow` (menú + status + navegación), `StatusView` (estado), `BlockListView` (lista de apps) y `AddBlockView` (formulario placeholder).
- **Archivos**:
  - `src/FocusBlock.Tui/App.cs` — `FocusBlockApp` con `IApplication` inyectado
  - `src/FocusBlock.Tui/Program.cs` — Composition Root (4 líneas)
  - `src/FocusBlock.Tui/Views/MainWindow.cs` — hub + `ShowView` + layout
  - `src/FocusBlock.Tui/Views/StatusView.cs` — Label + `RefreshStatus(DaemonStatus)`
  - `src/FocusBlock.Tui/Views/BlockListView.cs` — `ListView` + `ObservableCollection` + `ShowApps`
  - `src/FocusBlock.Tui/Views/AddBlockView.cs` — formulario (TextField + Button)
  - `src/FocusBlock.Tui/Models/DaemonStatus.cs` — record DTO
  - `tests/FocusBlock.Tests.Unit/*.cs` — 6 tests (5 archivos de test)
- **Tests**: 6 verdes (orquestación, estructura MainWindow, navegación, StatusView, BlockListView, AddBlockView).
- **Decisión**: `RefreshStatus` recibe el estado por parámetro (no provider) porque el daemon no existe hasta Fase 3 → vista presentacional y testeable.

## Decisiones

1. **DI + Composition Root** — `FocusBlockApp` recibe `IApplication` por constructor; `Program.cs` arma el grafo. *por qué*: testabilidad (Moq en tests, app real en producción).
2. **Navegación en `MainWindow`, no en `FocusBlockApp`** — el plan fijó `MainWindow_MenuNavigatesToViews`; mantener `FocusBlockApp` liviano. Tradeoff aceptado: `MainWindow` deja de ser 100% presentacional.
3. **Exponer "mirillas" para tests** — `StatusText`, `Apps`, `AppNameField`, etc. permiten verificar comportamiento sin romper encapsulación.
4. **UI copy en inglés** — consistente con los menús (`Daemon: running`, `Add Block`).

## Problemas

1. **`MenuItem(string, Action)` no existe en 2.4.17** aunque la doc oficial lo muestre — *solución*: usar object-initializer `new MenuItem { Title = "_X", Action = () => { } }`.
2. **`StatusItem` no existe** — `StatusBar` toma `Shortcut` (la doc v1 mentía). *solución*: verificar el API contra la DLL antes de codificar.
3. **Case-sensitive** — `y` minúscula vs `Y`; se reforzó la verificación del API.
4. **5 commits de la fase sin pushear** — quedaron locales en `dev` (04bb175, 31447bf, 5fb9f40, d8b1175, a8483fc).

## Métricas

- Tests escritos: 6 (todos verdes)
- Tests pasando: 6/6 (100%)
- Cobertura: N/A
- Commits: 5 (uno por feature)

## Pendientes

- Pushear los 5 commits de la Fase 1 a `dev` y avanzar el PR/merge de docs (PR #1).
- Cierre de Fase 1 en `docs/phase-plan.md` (checkboxes) — seguimiento en `docs/handoff.md`.
- Fase 2 — Sistema de Configuración.