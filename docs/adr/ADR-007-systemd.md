# ADR-007: Servicio systemd

- **Estado**: Aceptado
- **Fecha**: 2026-09-02

## Contexto

El daemon root debe arrancar con el sistema, reiniciarse ante fallos y correr con los permisos correctos.

## Decisión

**systemd** (`config/focusblock-daemon.service`, `Restart=always`).

## Alternativas consideradas

- **Contenedor como servicio nativo**: rechazado — el daemon necesita el PID namespace del host y root.
- **Supervisor/otros**: no son el init nativo.

## Consecuencias

- Nativo de Arch, auto-reinicio, logs a journald.
- El daemon no corre en contenedor: necesita el PID namespace del host y root (ver `ADR-009`). Docker queda como extra de aprendizaje opcional.

## Referencias

- `docs/architecture.md` (Servicio systemd)