# Fase 0: Setup del Proyecto

**Estado**: Completada
**Última Actualización**: 2026-09-02

## Objetivos

- Crear la solución .NET y los proyectos base del monorepo (`src/`, `tests/`).
- Lograr un Hello World TUI funcional con Terminal.Gui v2.
- Dejar lista la infraestructura de tests (xUnit + Moq + FluentAssertions) para las fases siguientes.

## Tareas Completadas

### 2026-09-02 — Scaffolding y Hello World
- **Descripción**: Creación de solución, proyecto TUI, paquete Terminal.Gui, Hello World con ventana + label, y proyecto de tests.
- **Archivos**:
  - `FocusBlock.slnx` — solución en formato XML nuevo de .NET 10
  - `src/FocusBlock.Tui/FocusBlock.Tui.csproj` — proyecto TUI (`net10.0`)
  - `src/FocusBlock.Tui/Program.cs` — Hello World TUI (Window + Label centrado)
  - `tests/FocusBlock.Tests.Unit/FocusBlock.Tests.Unit.csproj` — tests xUnit + Moq 4.20.72 + FluentAssertions 8.10.0
  - `tests/FocusBlock.Tests.Unit/UnitTest1.cs` — test placeholder de plantilla
- **Tests**: Ninguno propio aún (el placeholder es de plantilla). El TDD real arranca en Fase 1.

## Decisiones

1. **Target .NET 10 en lugar de .NET 8.** Solo está instalado el SDK 10.0.111. El código C# 12/13 funciona igual. *Pendiente: actualizar `AGENTS.md` y `README.md`, que aún mencionan .NET 8.*
2. **Driver `DOTNET` en Terminal.Gui (workaround).** El driver ANSI por defecto en Linux tiene un bug de rendering que deja la ventana sin subviews (issues upstream #4848 y #4374). Forzar `app.Init(DriverRegistry.Names.DOTNET)` lo resuelve. Revisar cuando el driver ANSI se estabilice.
3. **API por instancia de Terminal.Gui v2.** Se adopta el modelo nuevo (`IApplication`) en lugar de la API estática legacy, que está obsoleta.

## Problemas

1. **API legacy v1 no compila contra v2.** Los primeros errores (CS0246, CS0117) vinieron de escribir código de v1: en v2.4.17 los tipos están en sub-namespaces (`Terminal.Gui.App`, `Terminal.Gui.Views`, `Terminal.Gui.ViewBase`) y `Label` recibe el texto por propiedad `Text`, no por constructor. *Solución*: adoptar los usings de sub-namespace y el inicializador de propiedades.
2. **Ventana vacía (Label no se dibuja).** El código era idéntico al ejemplo oficial, pero el driver ANSI en Linux no renderiza las subviews. *Solución*: forzar el driver `DOTNET` en `Init()`.
3. **Error case-sensitive.** `y` minúscula no existe; la propiedad es `Y` (PascalCase).

## Aprendizajes

- **Terminal.Gui v2**:
  - Modelo por instancia: `Application.Create()` → `IApplication`; ya no hay objeto estático global.
  - `app.Run(window)` bloquea hasta que la ventana se cierra (Esc / `RequestStop()`).
  - El *alternate screen buffer* toma control de toda la terminal ("pantalla negra" mientras corre).
  - Los textos van en propiedades (`Text`), no en constructores.
- **.NET 10**: `dotnet new sln` genera el formato `.slnx` (XML) en lugar del `.sln` clásico.
- **Linux/Arch**: el "1: FocusBlock" en la parte superior es la tab bar de Zellij, no output de la app.
- **C#**: el case-sensitivity aplica también a propiedades de librerías.

## Métricas

- Tests escritos: 0 (placeholder de plantilla; TDD empieza en Fase 1)
- Tests pasando: N/A
- Cobertura: N/A
- Archivos creados: 5 (1 solución + 2 csproj + 2 archivos .cs)

## Pendientes

- Actualizar `AGENTS.md` y `README.md`: stack pasa de .NET 8 a .NET 10.
- Verificar si el driver ANSI ya está estable en una versión futura de Terminal.Gui (para quitar el workaround si conviene).