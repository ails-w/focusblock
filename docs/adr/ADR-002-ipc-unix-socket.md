# ADR-002: IPC por Unix domain socket

- **Estado**: Aceptado
- **Fecha**: 2026-09-02

## Contexto

La TUI corre como usuario regular y el daemon como root. Necesitan comunicarse de forma desacoplada y segura en el mismo host Linux.

## Decisión

IPC por **Unix domain socket** en `/run/focusblock/focusblock.sock`, mensajes JSON delimitados por newline.

## Alternativas consideradas

- **D-Bus**: potente pero con curva y dependencias de bus de sistema.
- **HTTP localhost**: añade superficie de autenticación sin ganancia en local.
- **Named pipes**: menos idiomático en Linux.

## Consecuencias

- Rápido, sin dependencias, nativo de Linux/Arch.
- Sin capa de red: no escala a multi-host (no es necesario).

## Referencias

- `docs/architecture.md` (Protocolo IPC)