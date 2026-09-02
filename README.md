# FocusBlock

Bloqueador de aplicaciones TUI para Arch Linux.

## ¿Qué es FocusBlock?

FocusBlock es una herramienta de terminal que te permite bloquear aplicaciones específicas durante períodos de tiempo definidos. Incluye daemon en segundo plano, métricas de uso y protección anti-bypass.

## Características

- Bloqueo de aplicaciones por nombre de proceso
- Programación temporal de bloqueos (rango horario por día)
- Anti-bypass: protección con contraseña, `chattr +i`, cooldown
- Métricas de uso con gráficos ASCII en terminal
- Daemon systemd con auto-reinicio

## Instalación

**Requisitos:** .NET 10 SDK (Arch: `sudo pacman -S dotnet-sdk`)

```bash
# Clonar repositorio
git clone https://github.com/tu-usuario/focusblock.git
cd focusblock

# Build
dotnet build

# Test
dotnet test
```

## Uso

```bash
# Iniciar daemon
sudo systemctl start focusblock

# Ejecutar TUI
focusblock
```

## Documentación

- [Arquitectura](docs/referencia/arquitectura.md)
- [Comandos](docs/referencia/comandos.md)
- [Guía de Setup](docs/guias/setup.md)

## Desarrollo

Ver `AGENTS.md` para convenciones, flujos de trabajo y contexto del proyecto.

## Licencia

MIT
