# ADR-004: Argon2id para hashing de contraseñas

- **Estado**: Aceptado
- **Fecha**: 2026-09-02

## Contexto

El anti-bypass necesita proteger la contraseña de desbloqueo temprano almacenada en config.

## Decisión

**Argon2id** vía `Konscious.Security.Cryptography.Argon2`, con salt único por hash.

## Alternativas consideradas

- **bcrypt**: amplio pero con límites de memoria.
- **PBKDF2**: estándar pero no memory-hard.

## Consecuencias

- Memory-hard: resistente a ataques con GPU/ASIC.
- Estándar moderno de hashing de contraseñas.

## Referencias

- `docs/vision.md` (Seguridad) · Fase 2 (`docs/learning/phase-02-config.md`)