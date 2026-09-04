# ADR-006: Escaneo `/proc` directo

- **Estado**: Aceptado
- **Fecha**: 2026-09-02

## Contexto

El daemon debe detectar procesos bloqueados por nombre de forma confiable en Linux.

## Decisión

Escaneo directo de **`/proc`** (leer `/proc/[pid]/status` y extraer el nombre).

## Alternativas consideradas

- **`Process.GetProcesses()`**: puede fallar por permisos y no expone todo lo que `/proc` ofrece.

## Consecuencias

- Más confiable en Linux, con control fino.
- Reafirma que el proyecto es Linux-only (ver `docs/vision.md`).

## Referencias

- Fase 3 (`docs/learning/phase-03-daemon.md`)