# Fase 03 — Daemon: Ciclo de Vida, Procesos, Señales e IPC

> Qué se aprende: cómo se construye un servicio de larga vida que observa el sistema operativo,
> actúa sobre él (mata procesos) y expone un canal de comunicación local.
> Log de la fase → `docs/progress-log/phase-03-daemon.md`
> Fase anterior → `docs/learning/phase-02-config.md`
> ADRs relacionados → `docs/adr/ADR-006-proc-scan.md`, `ADR-007-systemd.md`, `ADR-009-testing-seams.md`

## Glosario de la fase

| Término | Qué significa (en una línea) |
|---|---|
| Daemon | Proceso de larga vida que corre en segundo plano, sin interfaz. |
| Generic Host | Infraestructura de .NET que hospeda servicios y gestiona su ciclo de vida. |
| `BackgroundService` | Clase base de .NET para una tarea de larga duración dentro del host. |
| Hilo (thread) | Unidad de ejecución que el SO planifica en un núcleo. |
| `Task` / `await` | Operación asincrónica que libera el hilo mientras espera. |
| `CancellationToken` | Señal cooperativa para pedir que una operación se detenga. |
| `/proc` | Pseudo-filesystem del kernel que expone procesos y estado del sistema. |
| PID | Identificador numérico único de un proceso mientras vive. |
| P/Invoke | Mecanismo para llamar funciones nativas (C) desde .NET administrado. |
| Señal (signal) | Notificación asincrónica que el SO envía a un proceso. |
| `SIGTERM` (15) | "Terminá ordenadamente": el proceso puede capturarla y limpiar. |
| `SIGKILL` (9) | "Morí ya": no se puede capturar ni ignorar. |
| Escalación | Pasar de una señal educada a una terminal tras un período de gracia. |
| Unix domain socket | Canal de IPC local, direccionado por un path del filesystem. |
| Framing | Definir dónde empieza y termina cada mensaje dentro de un stream de bytes. |
| Newline-delimited JSON | Framing que usa `\n` como límite de mensaje. |
| Seam | Punto de inyección que aísla una dependencia del SO para poder testearla. |

## Mapa de conceptos

```text
BackgroundService (Worker)
        │  corre un bucle hasta que lo cancelan
        ▼
  ¿Qué procesos hay? ──▶ ProcessMonitor ──▶ IProcessSource ──▶ /proc
        │
        ▼
  ¿A quién mato? ──▶ BlockEnforcer ──▶ ISignalSender ──▶ kill() [SIGTERM→SIGKILL]
        │
        ▼
  ¿Quién me habla? ──▶ IpcServer ──▶ Unix socket ──▶ IIpcRequestHandler
```

Los cuatro conceptos son las cuatro responsabilidades del daemon: **orquestar**, **observar**,
**actuar** y **comunicar**. Cada una tiene su propio seam.

## Puntos de inyección de la fase

| Componente | Seam | Doble (unit) | Test real |
|---|---|---|---|
| `ProcessMonitor` | `IProcessSource` | fuente fake en memoria | `/proc` real |
| `BlockEnforcer` | `ISignalSender` | sender fake que registra señales | proceso `sleep` real |
| `IpcServer` | ruta del socket + `IIpcRequestHandler` | path temporal + handler fake | cliente/servidor real |
| `Worker` | `Func<CancellationToken, Task>` (tick) | delegate que cuenta llamadas | — |

---

## BackgroundService y ciclo de vida del worker

### En una frase

Un servicio que corre un bucle en segundo plano mientras nadie pida detenerlo, con un arranque y
un apagado ordenados que el host de .NET controla.

### Fundamentos previos

**¿Qué es un daemon?** Un proceso sin interfaz que corre en segundo plano. En FocusBlock es quien
vigila y bloquea; la TUI es solo el panel de control.

**¿Qué es el Generic Host?** Cuando hacés `Host.CreateApplicationBuilder(args)` y `host.Run()`, .NET
arma un contenedor con: configuración, logging, inyección de dependencias y **gestión del ciclo de
vida**. El host arranca tus servicios, espera, y los apaga ordenadamente cuando llega SIGTERM.

**¿Por qué un servicio y no un `while (true)` en `Main`?** Porque el host te da gratis: arranque y
apagado ordenados, cancelación en cascada, y la posibilidad de correr varios servicios en paralelo.
Un `while (true)` suelto no sabe apagarse.

**¿Qué es `BackgroundService`?** La clase base de .NET para trabajo de larga duración. Vos
sobreescribís `ExecuteAsync`; el host llama `StartAsync` y, al apagar, `StopAsync`.

### Qué es

`Worker : BackgroundService` es el bucle principal del daemon: cada `intervalo` ejecuta un "tick" de
trabajo y vuelve a dormir, hasta que el `stoppingToken` se cancela.

### Qué problema resuelve

Da un lugar único y ordenado donde vive la lógica periódica del daemon, y un apagado limpio: al
detener el servicio no quedan tareas colgadas.

### Cómo funciona paso a paso

1. El host llama `StartAsync`, que lanza `ExecuteAsync` en background y **devuelve el control**.
2. `ExecuteAsync` marca `Started` (para que los tests no hagan *race*) y entra al bucle.
3. Cada vuelta: `await _tick(stoppingToken)` y `await Task.Delay(_interval, stoppingToken)`.
4. Al llegar SIGTERM, el host llama `StopAsync`, que cancela el token interno.
5. El `await` en curso lanza `OperationCanceledException`, que el `catch` absorbe: el bucle termina
   sin error.

### Qué se rompería sin esto en FocusBlock

Sin un servicio hospedado, el daemon no arrancaría como unidad de systemd ni se reiniciaría ante
fallos; y el apagado dejaría trabajo a medias.

### Para qué sirve en este proyecto

Es el orquestador futuro: en Fase 4 su tick evaluará reglas de bloqueo y disparará el enforcement.
Hoy el tick es inyectable y vacío por defecto.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Daemon/Worker.cs (extracto)
public Worker(TimeSpan? interval = null, Func<CancellationToken, Task>? tick = null)
{
    _interval = interval ?? TimeSpan.FromSeconds(5);
    _tick = tick ?? (_ => Task.CompletedTask);
}

protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _started.TrySetResult();
    try
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _tick(stoppingToken);
            await Task.Delay(_interval, stoppingToken);
        }
    }
    catch (OperationCanceledException)
    {
        // Expected on shutdown: StopAsync cancels the loop.
    }
}
```

### Punto de inyección (si aplica)

El trabajo por tick entra como `Func<CancellationToken, Task>`. Eso permite testear que el bucle
**invoca el tick, más de una vez, y deja de invocarlo tras `StopAsync`**, sin depender del reloj
real ni de trabajo concreto.

### Error común

1. Usar `Thread.Sleep` en vez de `Task.Delay` — bloquea el hilo en vez de liberarlo.
2. Tragarse la `OperationCanceledException` sin entender por qué: es el **camino normal** de
   apagado, no un error.
3. Testear con `StopAsync(tokenYaCancelado)`: el token de `StopAsync` es para *esperar*, no para
   *cancelar*. El apagado se pide cancelando el token del host (o con `CancellationToken.None`).

### Para profundizar

- [BackgroundService (Microsoft Learn)](https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.backgroundservice)
- Código: `src/FocusBlock.Daemon/Worker.cs` · Tests: `tests/FocusBlock.Tests.Unit/WorkerTests.cs`.

---

## Escaneo de `/proc`

### En una frase

En Linux no hay que "preguntar" por los procesos: cada proceso vivo **es un directorio** dentro de
`/proc`.

### Fundamentos previos

**¿Qué es `/proc`?** Un pseudo-filesystem: no está en disco, lo genera el kernel en memoria. Es una
**ventana** al estado interno del sistema. Podés leerlo como archivos comunes.

**¿Qué es un PID?** El número que identifica a un proceso mientras vive. El kernel lo reutiliza
cuando un proceso muere (esto va a importar en Fase 4).

**¿Qué hay en `/proc/<pid>/`?** Archivos con metadata: `status` (nombre, estado, memoria),
`cmdline`, `stat`, y más. `status` es texto plano con líneas `Clave:\tvalor`.

### Qué es

`ProcessMonitor` recorre `/proc`, filtra los directorios cuyo nombre es numérico (son PIDs), lee
`status` y extrae el nombre del proceso.

### Qué problema resuelve

Obtener la lista de procesos **confiable y sin depender de APIs que pueden fallar por permisos**
(ese fue el motivo del ADR-006).

### Cómo funciona paso a paso

1. `GetProcessIds()` enumera los directorios de `/proc` y descarta los no numéricos
   (`/proc/cpuinfo`, `/proc/self`, etc. no son procesos).
2. Por cada PID, se lee `/proc/<pid>/status`.
3. Si el archivo no existe o no se puede leer, se **saltea** el proceso: pudo morir a mitad del scan.
4. Se busca la línea que empieza con `Name:` y se corta el prefijo.
5. `yield return` emite el nombre de a uno: el escaneo es **lazy** (no materializa toda la lista).

### Qué se rompería sin esto en FocusBlock

Sin el escaneo no hay forma de saber si `firefox` está corriendo, así que no habría nada que
bloquear. Y sin tolerar procesos que mueren a mitad del scan, el daemon podría crashear solo.

### Para qué sirve en este proyecto

Es el "sentido" del daemon: `ProcessMonitor` responde *qué* está corriendo. En Fase 4, el motor
cruzará esa lista con las reglas de configuración.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Daemon/Services/ProcessMonitor.cs (extracto)
public IEnumerable<string> GetRunningProcesses()
{
    foreach (int pid in _source.GetProcessIds())
    {
        string? status = _source.ReadStatus(pid);
        if (status is null)
        {
            continue;   // el proceso murió a mitad del scan
        }

        string? processName = ExtractProcessName(status);
        if (!string.IsNullOrEmpty(processName))
        {
            yield return processName;
        }
    }
}

// src/FocusBlock.Daemon/Services/ProcProcessSource.cs (extracto)
public string? ReadStatus(int pid)
{
    try
    {
        return File.ReadAllText(Path.Combine("/proc", pid.ToString(), "status"));
    }
    catch (IOException) { return null; }                 // murió entre el listado y la lectura
    catch (UnauthorizedAccessException) { return null; }  // no legible por este usuario
}
```

### Punto de inyección (si aplica)

`IProcessSource` (`GetProcessIds`, `ReadStatus`, `Exists`) aísla el acceso a `/proc`. Los tests
unitarios usan una fuente en memoria; un test de integración marca `[Trait("Category","Integration")]`
verifica contra `/proc` real.

### Error común

1. Probar contra `/proc` real en un test unitario: es **no determinista** (depende de qué corre en
   tu máquina). De ahí la fuente fake.
2. Asumir que el archivo siempre se puede leer: un proceso puede morir entre el listado y la
   lectura.
3. Parsear `status` a mano con `Split(' ')` — las líneas usan **tabulador**, no espacio; conviene
   cortar el prefijo (`line["Name:".Length..]`) y `Trim()`.

### Para profundizar

- [`proc(5)` — man page](https://man7.org/linux/man-pages/man5/proc.5.html)
- `docs/adr/ADR-006-proc-scan.md` · Código: `src/FocusBlock.Daemon/Services/`.

---

## P/Invoke y señales (SIGTERM → SIGKILL)

### En una frase

.NET no sabe "matar procesos"; sabe **llamar a la función `kill()` del sistema operativo**, y hay
que hacerlo en dos pasos: pedir educadamente, después forzar.

### Fundamentos previos

**¿Qué es código nativo vs administrado?** C# corre sobre el runtime administrado (.NET), con
recolector de basura y tipos seguros. `kill()` vive en `libc` (código C nativo). Para cruzar esa
frontera se usa **P/Invoke** (Platform Invocation).

**¿Qué es una señal?** Una notificación asincrónica que el SO entrega a un proceso. Es el mecanismo
estándar de Unix para decirle "terminá", "pausá", "recargá", etc.

**¿Por qué dos señales?** Porque hay dos intenciones distintas:
- `SIGTERM` (15) pide terminar **ordenadamente**: el proceso puede capturarla, guardar y salir.
- `SIGKILL` (9) **no se puede capturar ni ignorar**: el kernel lo mata. Garantiza, pero no da chance.

**¿Por qué no usar SIGKILL directo?** Porque no deja al proceso guardar estado (para un editor, eso
es perder trabajo). Se usa solo como último recurso.

### Qué es

`BlockEnforcer` implementa la **escalación**: manda `SIGTERM`, espera un período de gracia, y si el
proceso sigue vivo manda `SIGKILL`.

### Qué problema resuelve

Garantizar que el proceso bloqueado muera, sin ser innecesariamente brusco: se le da la chance de
cerrar bien antes de forzarlo.

### Cómo funciona paso a paso

1. `_sender.Send(pid, Signal.Term)` → `kill(pid, 15)` en libc.
2. `await Task.Delay(_gracePeriod, ct)` → se espera (por defecto 2 s).
3. Se verifica si el proceso sigue existiendo (`IProcessSource.Exists(pid)`).
4. Si sigue vivo, `_sender.Send(pid, Signal.Kill)` → `kill(pid, 9)`.
5. Si ya murió, no se hace nada: escalar sería tirar una señal a un PID que quizá ya es de otro.

### Qué se rompería sin esto en FocusBlock

Sin P/Invoke no hay bloqueo real: FocusBlock sería solo un visor de procesos. Sin escalación, un
proceso que ignora SIGTERM (o tarda en cerrar) sobreviviría al bloqueo.

### Para qué sirve en este proyecto

Es el "brazo ejecutor" del anti-bypass. En Fase 4 el motor decidirá *a quién*; esta clase ejecuta.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Daemon/Services/LibcSignalSender.cs (archivo completo)
using System.Runtime.InteropServices;

public class LibcSignalSender : ISignalSender
{
    [DllImport("libc", SetLastError = true)]
    private static extern int kill(int pid, int sig);

    public bool Send(int pid, Signal signal) => kill(pid, (int)signal) == 0;
}

// src/FocusBlock.Daemon/Services/BlockEnforcer.cs (extracto)
public async Task KillProcessAsync(int pid, CancellationToken ct = default)
{
    _sender.Send(pid, Signal.Term);
    await Task.Delay(_gracePeriod, ct);

    if (_processSource.Exists(pid))
    {
        _sender.Send(pid, Signal.Kill);
    }
}
```

### Punto de inyección (si aplica)

`ISignalSender` aísla el syscall. Los tests unitarios usan un sender fake que **registra el orden**
de las señales (`[Term]` vs `[Term, Kill]`); un test de integración lanza un `sleep 60` real y
verifica que muere.

### Error común

1. Mandar `SIGKILL` sin `SIGTERM`: se pierde la chance de un cierre limpio.
2. **Asumir que el PID sigue siendo el mismo proceso.** Entre el `SIGTERM` y el chequeo, el PID
   puede haberse reciclado y estar apuntando a **otro** proceso. Mitigación completa: comparar el
   start-time de `/proc/<pid>/stat`. *(Límite conocido, documentado en `BlockEnforcer`.)*
3. Ignorar el valor de retorno de `kill()`: devuelve `0` si anduvo, `-1` si falló (por ejemplo,
   `ESRCH` = no existe, `EPERM` = sin permiso).

### Para profundizar

- [`kill(2)` — man page](https://man7.org/linux/man-pages/man2/kill.2.html)
- [P/Invoke (Microsoft Learn)](https://learn.microsoft.com/dotnet/standard/native-interop/pinvoke)
- Código: `src/FocusBlock.Daemon/Services/BlockEnforcer.cs`.

---

## Unix domain sockets (servidor)

### En una frase

Un canal de comunicación local entre procesos, direccionado por un archivo especial, donde un
servidor escucha y varios clientes se conectan.

### Fundamentos previos

**¿Qué es un socket?** Un endpoint de comunicación entre dos procesos. Un socket de red usa
IP+puerto; un **Unix domain socket** usa un **path del filesystem** (ej. `/run/focusblock.sock`).

**¿Por qué Unix socket y no TCP localhost?** Porque no pasa por la pila de red, y su acceso se
controla con **permisos de archivo**. Para un daemon root que habla con la TUI del usuario, eso es
justo lo que se quiere (ADR-002).

**¿Qué es framing?** Un socket de stream (**SOCK_STREAM**) entrega **bytes**, no mensajes: no hay
fronteras. Si mandás dos mensajes seguidos, el receptor ve un chorro continuo. El **framing** define
dónde termina cada mensaje. Acá: **una línea JSON por mensaje** (`\n` como delimitador).

**¿Qué es un accept loop?** El servidor hace `Listen` y luego, en un bucle, `Accept`: cada `Accept`
devuelve una conexión nueva.

### Qué es

`IpcServer` escucha en un Unix socket, lee líneas JSON, las deserializa a `IpcMessage`, las rutea a un
`IIpcRequestHandler` y escribe la respuesta.

### Qué problema resuelve

Da el canal TUI ↔ daemon que sostiene toda la arquitectura: la TUI corre como usuario, el daemon como
root, y el socket los desacopla.

### Cómo funciona paso a paso

1. `StartAsync` borra un socket viejo si quedó (tras un crash), crea el socket AF_UNIX, hace `Bind`
   y `Listen`, y lanza el accept loop. **Vuelve cuando ya está escuchando.**
2. Cada `AcceptAsync` entrega una conexión, que se atiende en su **propia task** (un cliente malo no
   puede tumbar el servidor).
3. Por conexión: `StreamReader.ReadLineAsync` lee una línea = un mensaje.
4. Se deserializa; si el JSON es inválido, se responde `Error` **sin cerrar la conexión**.
5. Se llama al handler y se escribe la respuesta con `\n`, con flush.
6. `StopAsync` cancela el bucle, cierra el listener y **borra el archivo del socket**.

### Qué se rompería sin esto en FocusBlock

La TUI no tendría cómo preguntar el estado ni pedir bloqueos: el daemon quedaría incomunicado, y la
separación de privilegios (usuario/root) que es el corazón del diseño, sin canal.

### Para qué sirve en este proyecto

Es el plano de control del sistema. El contrato (`IpcMessage`) vive en `FocusBlock.Contracts`
precisamente para que ambos lados usen el mismo tipo.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Daemon/Services/IpcServer.cs (extractos)
private static readonly JsonSerializerOptions JsonOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
};

_listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
_listener.Bind(new UnixDomainSocketEndPoint(_socketPath));
_listener.Listen(1);

// por conexión:
string? line = await reader.ReadLineAsync(ct);
IpcMessage response = await ProcessLineAsync(line, ct);
await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonOptions));
```

El wire format queda: `{"type":"add_block","app_name":"firefox","schedule":"09:00-17:00"}`.

### Punto de inyección (si aplica)

Dos seams: la **ruta del socket** (los tests usan un path temporal) y el **`IIpcRequestHandler`**
(los tests usan un handler fake). Así el transporte se prueba completo sin necesitar el BlockEngine.

### Error común

1. **`ProtocolType.Unix` no existe.** Para `AddressFamily.Unix` con `SocketType.Stream` se usa
   `ProtocolType.Unspecified`. (Verificado empíricamente en .NET 10: `CS0117`.)
2. Asumir que un `Read` devuelve un mensaje completo. Los sockets de stream **no conservan
   fronteras**: de ahí el framing por `\n`.
3. No borrar el archivo del socket al cerrar: el próximo arranque falla al hacer `Bind` porque el
   path ya existe.
4. Cerrar la conexión ante un mensaje inválido: es mejor responder `Error` y seguir sirviendo.

### Para profundizar

- [`unix(7)` — man page](https://man7.org/linux/man-pages/man7/unix.7.html)
- `docs/adr/ADR-002-ipc-unix-socket.md` · `docs/architecture.md` (Protocolo IPC)
- Código: `src/FocusBlock.Daemon/Services/IpcServer.cs` · `src/FocusBlock.Contracts/IpcProtocol.cs`.

---

## Relación entre estos conceptos

| Rol del daemon | Concepto | Seam |
|---|---|---|
| Orquestar | `BackgroundService` | tick inyectable |
| Observar | escaneo de `/proc` | `IProcessSource` |
| Actuar | P/Invoke + señales | `ISignalSender` |
| Comunicar | Unix domain socket | ruta + `IIpcRequestHandler` |

El patrón se repite: **cada dependencia del sistema operativo vive detrás de un seam.** Eso es lo que
permite que un daemon que toca kernel, procesos y señales tenga tests unitarios rápidos y
deterministas, con la parte "real" aislada en tests de integración marcados por categoría.

## Diferido a Fase 4 (cableado)

Los cuatro componentes existen y están testeados, pero **el daemon todavía no los usa en runtime**:

- `Program.cs` registra solo `Worker`, con un tick vacío.
- No hay mapeo **nombre → PID** (el monitor devuelve nombres; el enforcer necesita PIDs).
- `IpcServer` no tiene un `IIpcRequestHandler` real.

El motivo no es un olvido: "¿qué bloqueo y cuándo?" es exactamente el `BlockEngine` de Fase 4. El
cableado end-to-end pertenece a esa fase.

## Convención

- **Archivo**: `docs/learning/phase-NN-name.md`, uno por fase.
- **Título**: `# Fase NN — Tema`.
- **Código**: bloques `cs`; líneas de ~100 caracteres.
- **Secciones por concepto**: ver `docs/learning/template-phase.md`.
