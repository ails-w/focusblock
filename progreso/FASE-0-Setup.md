# Fase 0: Setup del Proyecto

## Estado
**Estado**: Completada
**Última Actualización**: 2026-09-02

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

### 2026-09-02 - Scaffolding y Hello World
- **Descripción**: Solución .NET 10, proyecto TUI con Terminal.Gui v2 (Hello World verificado), proyecto de tests xUnit + Moq + FluentAssertions, documentación de aprendizaje de la fase.
- **Archivos**:
  - `FocusBlock.slnx` — solución en formato XML (.NET 10)
  - `src/FocusBlock.Tui/` — TUI con Hello World (Window + Label centrado)
  - `tests/FocusBlock.Tests.Unit/` — tests xUnit + Moq 4.20.72 + FluentAssertions 8.10.0
  - `docs/aprendizaje/FASE-0-Setup.md` — aprendizaje de la fase
- **Tests**: Ninguno propio aún (placeholder de plantilla). El TDD real arranca en Fase 1.
- **Decisión**: Target .NET 10 (no .NET 8). Driver `DOTNET` de Terminal.Gui como workaround al bug del driver ANSI en Linux.

## Métricas
- Tests escritos: 0 (TDD inicia en Fase 1)
- Tests pasando: N/A
- Cobertura: N/A