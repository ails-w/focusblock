# ADR-013: `FocusBlock.Core` y inversión de dependencias

- **Estado**: Aceptado
- **Fecha**: 2026-10-20

## Contexto

El daemon corre como root y necesita verificar la contraseña del early stop y cargar la
configuración al arrancar. Esas clases (`AuthService`, `ConfigService`) vivían en
`FocusBlock.Tui`, el proyecto de la app de usuario. El daemon no podía usarlas sin
depender de la TUI.

## Decisión

Extraer `FocusBlock.Core` con la lógica compartida entre TUI y daemon:

- `IPasswordVerifier` como puerto (lo que el dominio necesita).
- `AuthService` como adaptador (Argon2id + `FixedTimeEquals`).
- `ConfigService` con la carga y el guardado asíncrono.

`Core` referencia `Contracts`. TUI y Daemon referencian `Core`; ninguno referencia al otro.
El daemon registra el puerto en DI: `AddSingleton<IPasswordVerifier, AuthService>()`.

## Alternativas consideradas

- **Copiar `AuthService` en el daemon**: rechazada — dos implementaciones de Argon2id pueden
  divergir en parámetros y en correcciones de seguridad.
- **Que el daemon referencie la TUI**: rechazada — arrastra Terminal.Gui y todo el stack de
  UI a un servicio root, e invierte la dirección natural de la dependencia.
- **Validar la contraseña en la TUI**: rechazada — el lado usuario no es confiable; el gate
  debe vivir en el proceso root.

## Consecuencias

- El daemon verifica la contraseña sin conocer la UI y sin duplicar el algoritmo.
- `BlockEngine` depende de `IPasswordVerifier`, así que se puede testear con un doble.
- Aparece un proyecto más (`Core`) y mover clases exige actualizar namespaces.
- `Core` no debe contener nada que dependa de Terminal.Gui, `/proc` o el host: eso pertenece
  a TUI o Daemon.

## Referencias

- `src/FocusBlock.Core/`
- `docs/learning/phase-04-blocker.md` (inversión de dependencias)
- `docs/architecture.md` · `docs/adr/ADR-009-testing-seams.md`
