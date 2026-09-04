# Fase 0: Setup del Proyecto — Log

> Log HISTÓRICO de la fase. Se acumula, no se borra.
> Estado actual → `docs/handoff.md` · Conceptos → `docs/learning/phase-00-setup.md`

## Estado

**Estado**: Completada
**Última Actualización**: 2026-09-02

## Objetivos

- Crear la solución .NET y los proyectos base del monorepo (`src/`, `tests/`).
- Lograr un Hello World TUI funcional con Terminal.Gui v2.
- Dejar lista la infraestructura de tests (xUnit + Moq + FluentAssertions) para las fases siguientes.

## Progreso

- [x] Crear solución: `dotnet new sln -n FocusBlock` (2026-09-02)
- [x] Crear proyecto TUI: `dotnet new console -n FocusBlock.Tui -o src/FocusBlock.Tui` (2026-09-02)
- [x] Agregar a solución: `dotnet sln add src/FocusBlock.Tui` (2026-09-02)
- [x] Agregar paquete Terminal.Gui: `dotnet add package Terminal.Gui` (2026-09-02)
- [x] Crear `Program.cs` con Hello World (2026-09-02)
- [x] Ejecutar y verificar: `dotnet run` muestra ventana TUI (2026-09-02)
- [x] Crear carpeta `tests/FocusBlock.Tests.Unit/` (2026-09-02)
- [x] Crear carpetas `docs/` y `progreso/` (2026-09-02)

## Tareas Completadas

### 2026-09-02 — Scaffolding y Hello World
- **Descripción**: Solución .NET 10, proyecto TUI con Terminal.Gui v2 (Hello World verificado), proyecto de tests xUnit + Moq + FluentAssertions, documentación de aprendizaje de la fase.
- **Archivos**:
  - `FocusBlock.slnx` — solución en formato XML (.NET 10)
  - `src/FocusBlock.Tui/` — TUI con Hello World (Window + Label centrado)
  - `tests/FocusBlock.Tests.Unit/` — tests xUnit + Moq 4.20.72 + FluentAssertions 8.10.0
  - `docs/learning/phase-00-setup.md` — conceptos de la fase
- **Tests**: Ninguno propio aún (placeholder de plantilla). El TDD real arranca en Fase 1.
- **Decisión**: Target .NET 10 (no .NET 8). Driver `DOTNET` de Terminal.Gui como workaround al bug del driver ANSI en Linux.

## Decisiones

1. **Target .NET 10 en lugar de .NET 8.** Solo está instalado el SDK 10.0.111. El código C# 12/13 funciona igual. — *por qué*: no había SDK 8 disponible en el entorno.
2. **Driver `DOTNET` en Terminal.Gui (workaround).** El driver ANSI por defecto en Linux tiene un bug de rendering que deja la ventana sin subviews (issues upstream #4848 y #4374). — *por qué*: sin el workaround la app no renderiza.
3. **API por instancia de Terminal.Gui v2.** Se adopta el modelo nuevo (`IApplication`) en lugar de la API estática legacy, que está obsoleta.

## Problemas

1. **API legacy v1 no compila contra v2.** Los primeros errores (CS0246, CS0117) vinieron de escribir código de v1: en v2.4.17 los tipos están en sub-namespaces (`Terminal.Gui.App`, `Terminal.Gui.Views`, `Terminal.Gui.ViewBase`) y `Label` recibe el texto por propiedad `Text`, no por constructor. — *solución*: adoptar los usings de sub-namespace y el inicializador de propiedades.
2. **Ventana vacía (Label no se dibuja).** El código era idéntico al ejemplo oficial, pero el driver ANSI en Linux no renderiza las subviews. — *solución*: forzar el driver `DOTNET` en `Init()`.
3. **Error case-sensitive.** `y` minúscula no existe; la propiedad es `Y` (PascalCase). — *solución*: corregir la grafía.

## Métricas

- Tests escritos: 0 (placeholder de plantilla; TDD empieza en Fase 1)
- Tests pasando: N/A
- Cobertura: N/A
- Archivos creados: 5 (1 solución + 2 csproj + 2 archivos .cs)

## Pendientes

- Verificar si el driver ANSI ya está estable en una versión futura de Terminal.Gui (para quitar el workaround si conviene). → seguimiento en `docs/handoff.md`