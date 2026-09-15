# ADR-009: Testing por puntos de inyección (seams)

- **Estado**: Aceptado
- **Fecha**: 2026-09-15

## Contexto

El daemon de FocusBlock depende de features del **kernel del host**: enumera procesos vía `/proc`, envía
señales (`kill()`), aplica `chattr +i` (ioctl) y sirve IPC por Unix domain socket. Un contenedor comparte el
kernel pero **aísla el PID namespace**, así que no puede ver los procesos del host ni mandarles señales:
no sirve como runtime del daemon ni como entorno de test fiel. Empaquetar todo detrás de `pid: host` +
`privileged` rompe el aislamiento y de todos modos exige root.

## Decisión

Testear a través de **puntos de inyección (seams)**: la dependencia del SO se inyecta detrás de una
interfaz/parámetro y se reemplaza por un **fake** en los tests unitarios. El comportamiento que toca el
kernel de verdad se cubre con **tests de integración en el host real** (categoría `Integration`), no en
contenedores.

- **Unit**: fakes para `/proc`, señales, `chattr`, reloj, SQLite y sockets.
- **Integración**: se corre en el host, con privilegios cuando corresponda; se salta si faltan.
- **Sin Testcontainers**: se descarta como dependencia de desarrollo/CI.
- **Docker no es el runtime del daemon**: el daemon se despliega nativo con systemd (ADR-007).

## Alternativas consideradas

- **Docker con `pid: host` + `privileged`**: rechazada — rompe el aislamiento por cero ganancia y necesita
  root igual.
- **Testcontainers**: rechazada — comparte el kernel, agrega una dependencia de Docker a dev/CI, es más
  lento y flaky, y no simula las features del kernel (`/proc` del host, `kill`, `chattr`).
- **Bind-mount de `/proc` dentro del contenedor**: rechazada — frágil y los números de PID no mapean para
  `kill`.

## Consecuencias

- Los tests unitarios siguen **rápidos y deterministas** (sin Docker, sin root, sin kernel real).
- Los tests de integración corren **en el host** y están etiquetados por categoría; se saltan cuando faltan
  privilegios.
- El daemon se despliega **nativamente** vía systemd (ADR-007).
- Docker multi-stage queda como **extra de aprendizaje opcional**, no como estrategia de testing ni de
  runtime (`docs/extras/docker-multistage.md`).

## Referencias

- `docs/development-plan.md` (Puntos de inyección)
- `docs/extras/docker-multistage.md`
- `docs/adr/ADR-007-systemd.md`
