# Glosario

> Un término entra aquí si es prerrequisito o si se usa en más de una fase. Si se usa una sola
> vez, se define en el concepto donde aparece.

## Fase 00 — Setup

| Término | Qué es | Dónde |
|---|---|---|
| TUI | Interfaz dibujada con caracteres dentro de una terminal. | [Fase 00](phase-00-setup.md) |
| Terminal | Emulador que mantiene una grilla de celdas y dibuja texto. | [Fase 00](phase-00-setup.md) |
| Driver de rendering | Capa que traduce la UI a secuencias de escape. | [Driver](phase-00-setup.md#3-driver-dotnet) |
| .NET SDK | Herramientas `dotnet` para compilar, testear y ejecutar. | [Fase 00](phase-00-setup.md) |
| Solución | Archivo que agrupa proyectos para operarlos juntos. | [`.slnx`](phase-00-setup.md#2-solución-slnx) |
| Proyecto | Unidad de compilación descrita por un `.csproj`. | [Fase 00](phase-00-setup.md) |
| API por instancia | La app es un objeto con estado propio, no un global. | [API](phase-00-setup.md#1-api-por-instancia) |
| Case-sensitive | Distingue mayúsculas: `Y` y `y` son nombres distintos. | [Case](phase-00-setup.md#4-case-sensitivity) |
| Workaround | Solución temporal a un problema ajeno al propio código. | [Driver](phase-00-setup.md#3-driver-dotnet) |

## Fase 01 — TUI

| Término | Qué es | Dónde |
|---|---|---|
| Árbol de vistas | Jerarquía padre-hijo que determina qué se dibuja. | [Composición](phase-01-tui.md#1-composición-y-layout) |
| Layout declarativo | Posiciones y tamaños expresados como relaciones. | [Layout](phase-01-tui.md#1-composición-y-layout) |
| z-order | Orden de apilado: lo último agregado se dibuja encima. | [Composición](phase-01-tui.md#1-composición-y-layout) |
| Idempotencia | Ejecutar dos veces produce el mismo resultado que una. | [Navegación](phase-01-tui.md#2-navegación) |
| DI | Recibir las dependencias por constructor, no crearlas dentro. | [DI](phase-01-tui.md#3-di-y-roles) |
| Composition Root | Único lugar donde se construye el grafo de dependencias. | [DI](phase-01-tui.md#3-di-y-roles) |
| Container / Presentational | Separar quien orquesta de quien solo muestra. | [Fase 01](phase-01-tui.md) |
| DTO | Objeto que solo transporta datos, sin lógica de negocio. | [Datos vivos](phase-01-tui.md#4-datos-vivos) |
| `record` | Tipo con igualdad por valor e inmutabilidad por defecto. | [Datos vivos](phase-01-tui.md#4-datos-vivos) |
| `ObservableCollection` | Colección que notifica cambios a quien la observa. | [Datos vivos](phase-01-tui.md#4-datos-vivos) |
| Test double | Objeto falso que reemplaza una dependencia en un test. | [Tests](phase-01-tui.md#5-tests-aislados) |
| Mock | Doble con expectativas que se pueden verificar. | [Tests](phase-01-tui.md#5-tests-aislados) |
| AAA | Arrange, Act, Assert: las tres fases de un test. | [Tests](phase-01-tui.md#5-tests-aislados) |

## Fase 02 — Configuración

| Término | Qué es | Dónde |
|---|---|---|
| Modelo / POCO | Clase sin lógica que describe la forma de los datos. | [Modelo](phase-02-config.md#1-modelo-y-defaults) |
| Default | Valor que toma un campo al construir el objeto. | [Modelo](phase-02-config.md#1-modelo-y-defaults) |
| `TimeOnly` | Hora del día sin fecha ni zona horaria. | [Modelo](phase-02-config.md#1-modelo-y-defaults) |
| Serializar | Convertir un objeto en texto o bytes. | [JSON](phase-02-config.md#2-json-round-trip) |
| Round-trip | Objeto → texto → objeto conservando los mismos datos. | [JSON](phase-02-config.md#2-json-round-trip) |
| `async` / `await` | Esperar una operación sin bloquear el hilo. | [Async](phase-02-config.md#3-async-io) |
| `Task<T>` | Promesa de un valor que estará disponible después. | [Async](phase-02-config.md#3-async-io) |
| `CancellationToken` | Señal cooperativa para pedir que algo se detenga. | [Async](phase-02-config.md#3-async-io) |
| Hash | Huella de un solo sentido, de tamaño fijo. | [Hashing](phase-02-config.md#4-hashing-seguro) |
| Sal (salt) | Bytes únicos que se mezclan con la contraseña antes de hashear. | [Hashing](phase-02-config.md#4-hashing-seguro) |
| Argon2id | Hashing de contraseñas memory-hard. | [Hashing](phase-02-config.md#4-hashing-seguro) |
| CSPRNG | Generador aleatorio criptográficamente seguro. | [Hashing](phase-02-config.md#4-hashing-seguro) |
| Memory-hard | Cada intento exige mucha memoria, lo que frena GPUs. | [Costo](phase-02-config.md#4-hashing-seguro) |
| Timing attack | Deducir información midiendo cuánto tarda una operación. | [Costo](phase-02-config.md#4-hashing-seguro) |
| Tiempo constante | Comparar sin revelar cuántos bytes coinciden. | [Costo](phase-02-config.md#4-hashing-seguro) |

## Fase 03 — Daemon

| Término | Qué es | Dónde |
|---|---|---|
| Daemon | Proceso de larga vida que corre en segundo plano. | [Fase 03](phase-03-daemon.md) |
| `BackgroundService` | Clase base para trabajo de larga duración en el host. | [Worker](phase-03-daemon.md#1-backgroundservice) |
| `/proc` | Pseudo-filesystem del kernel que expone procesos. | [`/proc`](phase-03-daemon.md#2-escaneo-de-proc) |
| PID | Identificador de un proceso mientras vive. | [`/proc`](phase-03-daemon.md#2-escaneo-de-proc) |
| P/Invoke | Llamar funciones nativas (C) desde .NET. | [Señales](phase-03-daemon.md#3-señales) |
| `SIGTERM` | Pide un cierre ordenado; el proceso puede capturarla. | [Señales](phase-03-daemon.md#3-señales) |
| `SIGKILL` | Termina sin posibilidad de captura ni limpieza. | [Señales](phase-03-daemon.md#3-señales) |
| Escalación | Pasar de una señal educada a una terminal. | [Señales](phase-03-daemon.md#3-señales) |
| Unix domain socket | Canal local direccionado por un path del filesystem. | [Socket](phase-03-daemon.md#4-unix-socket) |
| Framing | Definir dónde termina cada mensaje en un stream de bytes. | [Socket](phase-03-daemon.md#4-unix-socket) |
| Seam | Punto de inyección que aísla una dependencia para testearla. | [Fase 03](phase-03-daemon.md) |

## Fase 04 — Bloqueador

| Término | Qué es | Dónde |
|---|---|---|
| Ventana semiabierta | Intervalo `[inicio, fin)`: incluye el inicio, excluye el fin. | [Reglas](phase-04-blocker.md#1-evaluar-reglas) |
| Wrap de medianoche | Ventana que cruza el día, como 22:00-06:00. | [Reglas](phase-04-blocker.md#1-evaluar-reglas) |
| Race condition | Dos hilos tocan el mismo dato y el resultado depende del orden. | [Cooldown](phase-04-blocker.md#2-cooldown) |
| `ConcurrentDictionary` | Diccionario thread-safe con operaciones atómicas. | [Cooldown](phase-04-blocker.md#2-cooldown) |
| Cooldown | Ventana de gracia durante la cual la app no se vuelve a bloquear. | [Cooldown](phase-04-blocker.md#2-cooldown) |
| Last-writer-wins | La última escritura pisa a la anterior, sin error. | [Cooldown](phase-04-blocker.md#2-cooldown) |
| Bucle de bloqueo | Tick que convierte reglas en kills reales. | [Bucle](phase-04-blocker.md#3-bucle-de-bloqueo) |
| Contrato / wire format | Representación serializada que ambos procesos comparten. | [IPC](phase-04-blocker.md#4-early-stop-y-contrato) |
| Early stop | Cortar un bloqueo antes de tiempo con fricción y contraseña. | [Early stop](phase-04-blocker.md#4-early-stop-y-contrato) |
| Fricción | Costo deliberado que obliga a decidir con intención. | [Early stop](phase-04-blocker.md#4-early-stop-y-contrato) |
| Puerto | Interfaz que declara lo que el dominio necesita. | [DIP](phase-04-blocker.md#5-inversión-de-dependencias) |
| Adaptador | Implementación concreta de un puerto. | [DIP](phase-04-blocker.md#5-inversión-de-dependencias) |
| Inversión de dependencias | Alto y bajo nivel dependen de abstracciones, no entre sí. | [DIP](phase-04-blocker.md#5-inversión-de-dependencias) |
