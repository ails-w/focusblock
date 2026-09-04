# ADR-009: Docker multi-stage

- **Estado**: Aceptado
- **Fecha**: 2026-09-02

## Contexto

El daemon necesita imágenes de producción pequeñas y un entorno aislado para testing en Docker.

## Decisión

**Docker multi-stage** (`Dockerfile.daemon`): build en imagen SDK, runtime en imagen slim.

## Alternativas consideradas

- **Single stage**: imágenes grandes con toolchain de build incluida.

## Consecuencias

- Imágenes de producción pequeñas y sin SDK.
- Uso por fases: desarrollo local (Fases 0-2), Docker para daemon (Fase 3+).

## Referencias

- `docs/development-plan.md` (Docker) · Fase 3