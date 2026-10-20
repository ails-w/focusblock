# Fase 03 — Daemon: ciclo de vida, procesos y sockets

> Qué construye: el proceso de larga vida que observa el sistema, actúa sobre él y expone un
> canal local para la TUI.
> Antes: [Fase 02](phase-02-config.md) · Después: [Fase 04](phase-04-blocker.md) ·
> [Glosario](glossary.md)

## Dónde estamos

La config existe, pero nadie la usa: no hay proceso que vigile ni canal de comunicación. Esta
fase crea las cuatro piezas del daemon. El cableado entre ellas llega en la fase 4.

```text
systemd ──▶ Host ──▶ Worker (bucle cada 5 s)
                        ├── ProcessMonitor ──▶ /proc
                        ├── BlockEnforcer ──▶ kill() SIGTERM → SIGKILL
                        └── IpcServer ──▶ /run/focusblock/focusblock.sock
                                                   ▲
                                                  TUI
```

## Qué construye esta fase

Un `BackgroundService` con apagado ordenado, un monitor de procesos que lee `/proc`, un
ejecutor con escalación de señales y un servidor de Unix socket. Cada dependencia del sistema
operativo queda detrás de un punto de inyección para poder testearla.

## El recorrido de una acción

1. systemd arranca el daemon como root.
2. El host inicia `Worker` y este ejecuta un tick cada 5 segundos.
3. `ProcessMonitor` enumera `/proc` y obtiene pares `(pid, nombre)`.
4. `BlockEnforcer` manda `SIGTERM`, espera 2 segundos y manda `SIGKILL` si el proceso vive.
5. `IpcServer` escucha en el socket: la TUI conecta y envía una línea JSON.
6. El handler responde por la misma conexión; el servidor sigue aceptando clientes.

## Los conceptos

| # | Concepto | Idea en una línea | Código |
|---|---|---|---|
| 1 | `BackgroundService` | Un bucle con arranque y apagado que controla el host | `Worker.cs` |
| 2 | Escaneo de `/proc` | Cada proceso es un directorio; se lee y se tolera que muera | `ProcessMonitor.cs` |
| 3 | Señales | `SIGTERM` pide; `SIGKILL` obliga | `BlockEnforcer.cs` |
| 4 | Unix socket | Canal local con mensajes delimitados por `\n` | `IpcServer.cs` |

---

## 1. BackgroundService

**Idea.** Es un trabajo de larga duración que corre dentro del host de .NET. El host lo
arranca, le da un token de cancelación y lo apaga de forma ordenada. En esta fase el tick
está vacío.

**Analogía.** Es un turno de vigilancia: el edificio (host) te dice cuándo empezar y cuándo
terminar. No decides tú cuándo cierra el edificio; solo haces tu ronda hasta que suena la
señal.

**Cómo funciona.** 1) El host llama `StartAsync` y lanza `ExecuteAsync` en segundo plano. 2)
`Worker` marca `Started` para que los tests no compitan con el arranque. 3) Cada vuelta
ejecuta el tick y espera con `Task.Delay`. 4) Al apagar, el token se cancela; el `catch`
absorbe la excepción: es el camino normal.

```cs
// src/FocusBlock.Daemon/Worker.cs
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
        // Apagado normal.
    }
}
```

**Trampa.** Usar `Thread.Sleep`: bloquea el hilo en vez de liberarlo. Y no confundir el token
de `StopAsync` (sirve para esperar) con el token que cancela el bucle.

<details><summary>Verifica</summary>

1. ¿Quién llama a `ExecuteAsync` y cuándo termina?
2. ¿Por qué `OperationCanceledException` no es un error aquí?
3. ¿Qué ventaja tiene el tick inyectable?

</details>

---

## 2. Escaneo de `/proc`

**Idea.** En Linux los procesos vivos son directorios dentro de `/proc`. Se enumeran, se lee
su estado y se tolera que desaparezcan a mitad del escaneo.

**Analogía.** Es el tablero de un aeropuerto: cada vuelo tiene su fila. Si un vuelo despega
mientras lees el tablero, esa fila desaparece; no se cae el tablero por eso.

**Cómo funciona.**

1. Se enumeran los directorios de `/proc` y se descartan los no numéricos.
2. Por cada PID se lee `/proc/<pid>/status`.
3. Si el archivo no existe o no se puede leer, se saltea el proceso.
4. Se busca la línea `Name:` y se corta el prefijo.
5. `yield return` emite de a uno: el escaneo es lazy, no materializa toda la lista.

**En el proyecto.** Es el "sentido" del daemon: responde qué está corriendo. Un proceso puede
morir entre el listado y la lectura, y el daemon no debe caerse por eso.

```cs
// src/FocusBlock.Daemon/Services/ProcessMonitor.cs
public IEnumerable<ProcessInfo> GetProcesses()
{
    foreach (int pid in _source.GetProcessIds())
    {
        string? status = _source.ReadStatus(pid);
        if (status is null) continue;          // murió a mitad del scan

        string? name = ExtractProcessName(status);
        if (!string.IsNullOrEmpty(name)) yield return new ProcessInfo(pid, name);
    }
}
```

**Trampa.** Testear contra `/proc` real: depende de qué corre en tu máquina. Y parsear con
`Split(' ')`: las líneas de `status` usan tabulador. Corta el prefijo y aplica `Trim()`.

<details><summary>Verifica</summary>

1. ¿Por qué se descartan los directorios no numéricos?
2. ¿Qué significa que el escaneo sea lazy?
3. ¿Por qué un proceso puede no tener `status` legible?

</details>

---

## 3. Señales

**Idea.** .NET no "mata procesos": llama a `kill()` del sistema operativo. Se pide con
`SIGTERM`, se espera, y si el proceso sigue vivo se fuerza con `SIGKILL`.

**Analogía.** Es desalojar una casa: primero llamas a la puerta y avisas (`SIGTERM`); si nadie
sale, entras por la fuerza (`SIGKILL`). La segunda opción funciona siempre, pero no deja
recoger las cosas.

**Cómo funciona.**

1. `kill(pid, SIGTERM)` pide un cierre ordenado.
2. Se espera el período de gracia (2 segundos).
3. Se comprueba si el PID sigue existiendo.
4. Si sigue, `kill(pid, SIGKILL)` lo termina sin posibilidad de captura.
5. Si ya murió, no se hace nada.

**En el proyecto.** Es el brazo ejecutor del bloqueo. `SIGKILL` directo perdería el trabajo del
usuario; la escalación da una oportunidad de cerrar bien.

```cs
// src/FocusBlock.Daemon/Services/LibcSignalSender.cs
[DllImport("libc", SetLastError = true)]
private static extern int kill(int pid, int sig);

public bool Send(int pid, Signal signal) => kill(pid, (int)signal) == 0;

// src/FocusBlock.Daemon/Services/BlockEnforcer.cs
_sender.Send(pid, Signal.Term);
await Task.Delay(_gracePeriod, ct);
if (_processSource.Exists(pid)) _sender.Send(pid, Signal.Kill);
```

**Trampa.** Asumir que el PID sigue siendo el mismo proceso: entre el `SIGTERM` y la
comprobación, el kernel puede reutilizarlo. La mitigación completa compara el start-time de
`/proc/<pid>/stat`.

<details><summary>Verifica</summary>

1. ¿Por qué se usa `SIGTERM` antes de `SIGKILL`?
2. ¿Qué puede salir mal si el PID se reutiliza?
3. ¿Qué devuelve `kill()` y qué significa cada valor?

</details>

---

## 4. Unix socket

**Idea.** Es un canal de comunicación local direccionado por un archivo. El servidor escucha,
acepta conexiones y lee mensajes delimitados por `\n`.

**Analogía.** Es una ventanilla de atención: hay una fila (accept loop), cada persona es
atendida en su propia conversación y una persona maleducada no detiene la ventanilla.

**Cómo funciona.**

1. `StartAsync` borra un socket viejo, hace `Bind`, `Listen` y lanza el accept loop.
2. Cada `AcceptAsync` entrega una conexión que se atiende en su propia tarea.
3. `ReadLineAsync` lee una línea: ese es el límite del mensaje (framing).
4. Se deserializa y se rutea al handler; un JSON inválido responde `Error` sin cerrar.
5. `StopAsync` cancela, cierra el listener y borra el archivo del socket.

**En el proyecto.** Es el plano de control TUI ↔ daemon. El socket desacopla privilegios: la
TUI corre como usuario y el daemon como root. El contrato vive en `Contracts`.

```cs
// src/FocusBlock.Daemon/Services/IpcServer.cs
_listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
_listener.Bind(new UnixDomainSocketEndPoint(_socketPath));
_listener.Listen(1);
_acceptLoop = AcceptLoopAsync(_listener, _cts.Token);

// por conexión:
string? line = await reader.ReadLineAsync(ct);
IpcMessage response = await ProcessLineAsync(line, ct);
await writer.WriteLineAsync(JsonSerializer.Serialize(response, IpcJson.Options));
```

**Trampa.** `ProtocolType.Unix` no existe: para `AddressFamily.Unix` se usa
`ProtocolType.Unspecified`. Y si no borras el archivo al cerrar, el próximo `Bind` falla.

<details><summary>Verifica</summary>

1. ¿Por qué un socket de stream necesita framing?
2. ¿Qué pasa si un cliente manda dos mensajes seguidos?
3. ¿Por qué cada conexión se atiende en su propia tarea?

</details>

---

## Práctica

### Mezcla

1. Si el daemon recibe `SIGTERM` de systemd, ¿qué hace `Worker` y qué token se cancela?
2. ¿Por qué el test de `ProcessMonitor` usa una fuente fake y no `/proc`?
3. Un cliente envía `{"type":"status"}` sin `\n` y no cierra: ¿qué ve el servidor?
4. ¿Por qué el socket es mejor que TCP localhost para este caso?

<details><summary>Respuestas</summary>

1. El token del host se cancela; `Task.Delay` lanza `OperationCanceledException` y el bucle
   termina sin error.
2. Porque `/proc` real no es determinista: depende de los procesos de la máquina.
3. Nada: `ReadLineAsync` espera el delimitador. El mensaje no existe hasta el `\n`.
4. No pasa por la pila de red y sus permisos son de archivo, justo lo que separa usuario y root.

</details>

### En código

1. Predice el orden de señales si el proceso ignora `SIGTERM`.
2. Escribe un test con `IProcessSource` fake para un proceso que muere a mitad del escaneo.
3. Conecta al socket con `socat` y envía una línea JSON. ¿Qué responde?

### Antes de cerrar

- [ ] Puedo explicar las cuatro responsabilidades del daemon.
- [ ] Sé qué señal se manda primero y por qué.
- [ ] Puedo explicar por qué cada dependencia del SO tiene un seam.

## Trampas reales de la fase

- **`ProtocolType.Unix` no existe** (`CS0117`): se usa `ProtocolType.Unspecified`.
- **Socket viejo tras un crash**: hay que borrar el archivo antes de `Bind`.
- **Tests contra `/proc` real**: no deterministas; se reescribieron con `IProcessSource` fake.
- **`FINALNEWLINE` de `dotnet format`**: 13 archivos rompían el gate de CI.

## Para profundizar

- [`ADR-006`](../adr/ADR-006-proc-scan.md) — por qué `/proc`.
- [`ADR-002`](../adr/ADR-002-ipc-unix-socket.md) — por qué Unix socket.
- [`ADR-009`](../adr/ADR-009-testing-seams.md) — puntos de inyección.
