# ADR-011: Driver DOTNET de Terminal.Gui (workaround)

- **Estado**: Aceptado (workaround activo — re-evaluar)
- **Fecha**: 2026-09-02

## Contexto

El driver ANSI (por defecto en Linux) de Terminal.Gui renderiza la ventana **vacía sin subviews**, incluso con el ejemplo oficial. Bugs upstream: [issue #4848](https://github.com/gui-cs/Terminal.Gui/issues/4848) (display garbled) y [#4374](https://github.com/gui-cs/Terminal.Gui/issues/4374) (`Application.Screen` vacío al `Init`).

## Decisión

Forzar el driver **DOTNET** en el arranque:

```csharp
app.Init(DriverRegistry.Names.DOTNET);
```

## Alternativas consideradas

- **Driver ANSI**: es el default y el que debería usarse; roto en Linux en esta versión.
- **Driver WINDOWS**: solo aplica a Windows.

## Consecuencias

- La TUI renderiza correctamente en Linux/Arch (incluido Zellij).
- Se pierden features avanzadas del driver ANSI (negociación de capacidades, teclado Kitty, true color fino) — innecesarias para menús/listas/charts ASCII.
- **Seguimiento**: revisar cuando el driver ANSI se estabilice (ver `docs/handoff.md`).

## Referencias

- `docs/learning/phase-00-setup.md` · `docs/handoff.md` (riesgo activo)