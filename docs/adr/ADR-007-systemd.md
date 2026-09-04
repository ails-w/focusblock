# ADR-007: Servicio systemd

- **Estado**: Aceptado
- **Fecha**: 2026-09-02

## Contexto

El daemon root debe arrancar con el sistema, reiniciarse ante fallos y correr con los permisos correctos.

## Decisión

**systemd** (`config/focusblock-daemon.service`, `Restart=always`).

## Alternativas consideradas

- **Docker only**: útil para desarrollo/testing, no como servicio nativo de Arch.
- **Supervisor/otros**: no son el init nativo.

## Consecuencias

- Nativo de Arch, auto-reinicio, logs a journald.
- Docker queda como entorno opcional para tests (ver `ADR-009`).

## Referencias

- `docs/architecture.md` (Servicio systemd)