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
- Los parámetros de costo (`MemorySize`, `Iterations`, `DegreeOfParallelism`) son constantes de código y **no se persisten** junto al hash: subirlos invalida los hashes existentes (la verificación recomputa con los parámetros nuevos).
- Un string en formato PHC (`$argon2id$v=19$m=...,t=...,p=...$<salt>$<hash>`) permitiría que el costo evolucione de forma transparente.

## Referencias

- `docs/vision.md` (Seguridad) · Fase 2 (`docs/learning/phase-02-config.md`)