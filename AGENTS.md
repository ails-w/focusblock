# FocusBlock — Agent Context

## Project Overview

FocusBlock es una app TUI en C# para bloquear aplicaciones en Arch Linux. Un daemon root monitorea `/proc`, mata procesos bloqueados y cumple horarios. La TUI corre como usuario regular y se comunica con el daemon via Unix domain sockets.

**Stack:** C# 14 / .NET 10, Terminal.Gui v2, SQLite + Dapper, xUnit + Moq + FluentAssertions, Docker (daemon)

## Architecture

```
┌─────────────────────────────────────────────┐
│  TUI (user)          Unix socket            │
│  Terminal.Gui  ◄──────────────────────►     │
├─────────────────────────────────────────────┤
│  Daemon (root)      /proc scan, kill        │
│  systemd service    SQLite metrics          │
└─────────────────────────────────────────────┘
```

**Por qué esta separación?** La TUI corre como usuario regular (seguro, fácil de desarrollar). El daemon corre como root via systemd (requerido para `/proc` y `chattr +i`). IPC los mantiene desacoplados.

## Current Status

**Phase:** 0 — Setup & Hello World (Completada)
**Done:** Solución `FocusBlock.slnx`, TUI Hello World (Terminal.Gui v2 + driver DOTNET), tests xUnit + Moq + FluentAssertions
**Next:** Fase 1 — Esqueleto TUI: `FocusBlockApp`, `MainWindow` con menú

## Session Startup Checklist

1. `mem_context(project: "focusblock")` — historial reciente
2. Leer `progreso/FASE-{N}-{Name}.md` — estado de la fase actual
3. Leer `docs/referencia/phases.md` — qué features faltan
4. Leer `AGENTS.md` (este archivo) — contexto completo
5. Preguntar al usuario: "¿Qué hacemos hoy?"

## Development Rules

| Regla | Detalle |
|-------|---------|
| TDD | RED → GREEN → REFACTOR. Test ANTES de implementar |
| Test framework | xUnit + Moq + FluentAssertions |
| Test naming | `Method_Condition_ExpectedResult` |
| Code language | English (variables, classes, comments, commits) |
| Docs language | Spanish |
| Commits | Conventional commits in English |
| Format | 4 spaces indent, 100 char line limit |
| Naming | PascalCase classes, _camelCase private fields |

## Testing Strategy (Pyramid)

```
        ╱╲
       ╱  ╲      Functional (Docker compose, E2E — solo flujos críticos)
      ╱────╲
     ╱      ╲    Integration (TestContainers, límites entre componentes)
    ╱────────╲
   ╱          ╲  Unit (xUnit + Moq + FluentAssertions — base sólida)
```

**Regla TDD por feature:** Cada feature se desarrolla con:
1. Escribir tests unitarios (RED)
2. Implementar código mínimo (GREEN)
3. Refactorizar
4. Agregar tests de integración si toca límites externos
5. Test funcional solo si es flujos críticos

## Key Files Reference

| Archivo | Propósito |
|---------|-----------|
| `src/FocusBlock.Tui/Program.cs` | Entry point TUI |
| `src/FocusBlock.Tui/App.cs` | Configuración Terminal.Gui |
| `src/FocusBlock.Tui/Views/MainWindow.cs` | Hub de navegación |
| `src/FocusBlock.Daemon/Worker.cs` | BackgroundService |
| `src/FocusBlock.Daemon/Services/ProcessMonitor.cs` | Escaneo /proc |
| `src/FocusBlock.Daemon/Services/BlockEnforcer.cs` | Lógica SIGTERM/SIGKILL |
| `src/FocusBlock.Daemon/Services/IpcServer.cs` | Unix socket server |
| `src/FocusBlock.Contracts/IpcProtocol.cs` | Tipos de mensaje compartidos |
| `config/focusblock.json` | Configuración por defecto |
| `config/focusblock-daemon.service` | Archivo systemd |
| `config/docker/Dockerfile.daemon` | Multi-stage build daemon |
| `config/docker/docker-compose.yml` | Desarrollo con Docker |

## Conventions

### C# Naming

| Elemento | Convención | Ejemplo |
|----------|------------|---------|
| Clases | PascalCase | `BlockService` |
| Interfaces | I + PascalCase | `IBlockService` |
| Métodos | PascalCase | `AddBlockRule()` |
| Campos privados | _camelCase | `_repository` |
| Variables | camelCase | `blockRule` |
| Enums | PascalCase | `BlockType` |

### Git (Conventional Commits)

```
feat(tui): add block list view
fix(daemon): handle missing /proc entry
test: add unit tests for BlockEngine
docs: update architecture diagram
```

### Test Structure

```csharp
// Patrón: Method_Condition_ExpectedResult
[Fact]
public void BlockRule_WhenActive_ReturnsBlocked()

// Arrange → Act → Assert
public class BlockServiceTests
{
    private readonly Mock<IBlockRepository> _repoMock;
    private readonly BlockService _sut;

    public BlockServiceTests()
    {
        _repoMock = new Mock<IBlockRepository>();
        _sut = new BlockService(_repoMock.Object);
    }
}
```
