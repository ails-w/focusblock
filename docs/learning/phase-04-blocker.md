# Fase 04 — Núcleo del Bloqueador: Reglas, Cooldown y Early Stop

> Qué se aprende: cómo una regla de configuración se convierte en una decisión de bloqueo,
> cómo esa decisión convive con procesos reales y ventanas de gracia, y cómo el usuario
> recupera el control pagando fricción + contraseña.
> Log de la fase → `docs/progress-log/phase-04-blocker.md`
> Diagramas → `docs/diagrams/phase-04-blocker.md`
> Fase anterior → `docs/learning/phase-03-daemon.md`
> ADRs relacionados → `docs/adr/ADR-009-testing-seams.md`, `docs/adr/ADR-002-ipc-unix-socket.md`,
> `docs/adr/ADR-004-argon2id.md`

## Glosario de la fase

| Término | Qué significa (en una línea) |
|---|---|
| Función pura | Función cuyo resultado depende solo de sus parámetros: sin reloj, disco ni estado oculto. |
| Ventana semiabierta | Intervalo `[inicio, fin)`: incluye el inicio, excluye el fin. |
| Wrap de medianoche | Ventana que cruza el día (ej. 22:00–06:00) y está activa a ambos lados de la frontera. |
| Ventana degenerada | `StartTime == EndTime`: una ventana vacía que nunca debe bloquear. |
| Race condition | Dos hilos tocan el mismo dato y el resultado depende del orden de acceso. |
| Colección concurrente | Estructura de datos diseñada para que varios hilos la usen sin corromperla. |
| Last-writer-wins | Política donde la última escritura pisa a la anterior, sin error ni merge. |
| `ConcurrentDictionary` | Diccionario thread-safe de .NET: sus operaciones atómicas no necesitan lock externo. |
| Cooldown | Ventana de gracia durante la cual una app no vuelve a bloquearse. |
| `comm` | Nombre corto del proceso que el kernel expone en `/proc/<pid>/status` y trunca a 15 caracteres. |
| PID | Identificador del proceso mientras vive; el kernel puede reciclarlo. |
| Early stop | Terminar un bloqueo antes de que la ventana termine, con fricción y contraseña. |
| Fricción | Costo deliberado (tipear 4 palabras) que obliga a decidir con intención. |
| CSPRNG | Generador aleatorio criptográficamente seguro (ej. `RandomNumberGenerator`). |
| Argon2id | Algoritmo de hashing de contraseñas memory-hard. |
| `FixedTimeEquals` | Comparación de bytes en tiempo constante: no filtra cuántos bytes coinciden. |
| Wire format | La representación serializada de los mensajes que viajan por el socket. |
| Framing | Definir dónde termina un mensaje dentro de un stream de bytes (acá: `\n`). |
| Inversión de dependencias | Módulos de alto nivel dependen de abstracciones, no de implementaciones. |
| Puerto / adaptador | Interfaz que declara lo que se necesita / clase concreta que lo implementa. |
| Seam | Punto de inyección que aísla una dependencia para poder testearla. |

## Mapa de conceptos

```text
  Inversión de dependencias (FocusBlock.Core + IPasswordVerifier)
        │  habilita verificar la contraseña en el daemon
        ▼
  Evaluación de reglas (BlockEngine.Evaluate) ── decide ──▶ qué apps bloquear
        │
        ▼
  Bucle de bloqueo (Worker → BlockCoordinator) ── ejecuta ──▶ ProcessMonitor → /proc
        │                                                        │
        │                                    Thread-safety ──▶ CooldownManager
        │                                                        │
        ▼                                                        ▼
  Early stop (fricción + contraseña) ──▶ Contrato IPC (ForceStop) ──▶ cooldown
        │
        ▼
  La TUI solo aporta fricción y transporte; la decisión vive en el daemon.
```

## Puntos de inyección de la fase

| Componente | Seam | Doble (unit) | Test real |
|---|---|---|---|
| `BlockEngine` | ninguno: `now` entra por parámetro (función pura) | — | — |
| `CooldownManager` | ninguno: `now` entra por parámetro | — | — |
| `BlockCoordinator` | `TimeProvider` + seams heredados (`IProcessSource`, `ISignalSender`) | reloj fijo + fuente/sender fake | `/proc` real (tests de Fase 3) |
| `DaemonRequestHandler` | `IPasswordVerifier` + `TimeProvider` | `AuthService` real con hash de test | — |
| `EarlyStopService` | `IIpcClient` + `IEarlyStopPrompt` | client y prompt fake | `IpcClientTests` (socket real, path temporal) |
| `ChallengeSystem` | sin seam: usa el CSPRNG del SO | — (se testean propiedades) | — |
| `IpcClient` | ruta del socket | servidor `IpcServer` con path temporal | smoke test del daemon |

---

## Evaluación de reglas de dominio

### En una frase

Decidir si una regla de bloqueo aplica es una cuenta de intervalos, no una consulta al reloj:
si el "ahora" entra por parámetro, la decisión es pura y testeable.

### Fundamentos previos

**¿Qué es `TimeOnly`?** Una hora del día sin fecha: de `00:00` a `23:59:59.999...`. A diferencia de
`DateTime`, no tiene día, mes ni zona. Es exactamente lo que guarda `BlockRuleConfig.StartTime` y
`EndTime`, porque una regla es "todos los días de 09:00 a 17:00", no "el 15 de enero".

**¿Qué es un intervalo semiabierto?** Un rango `[inicio, fin)` incluye el inicio y excluye el fin.
Con `[09:00, 17:00)`: a las 09:00 bloquea, a las 17:00 no. Es la convención estándar para ventanas de
tiempo porque permite encadenar rangos sin solapamientos: `[09:00, 12:00)` y `[12:00, 17:00)` cubren
el día sin que las 12:00 se cuente dos veces.

**¿Qué es una función pura?** Una función que cumple dos condiciones: (1) mismo input → mismo output,
siempre; (2) no tiene efectos secundarios (no escribe archivos, no manda señales, no lee el reloj).
Ventaja: se testea con valores y se razona sin contexto. `Evaluate(rule, now)` es pura.

**¿Qué es una dependencia oculta del reloj?** Si el motor hiciera `DateTime.Now` internamente, el
resultado dependería de *cuándo* corre el test. Eso es una dependencia oculta: no aparece en la firma,
pero cambia el comportamiento. Se elimina recibiendo `now` por parámetro (o un `TimeProvider`).

**¿Qué es el wrap de medianoche?** Una ventana como 22:00–06:00 cruza la frontera del día: no se
puede expresar con `Start < now < End` porque `22:00 < 06:00` es falso. Hay que partir la condición:
"después de las 22:00 **o** antes de las 06:00".

### Qué es

`BlockEngine.Evaluate(rule, now)` responde una sola pregunta: *¿esta regla está activa en este
instante?* `GetAppsToBlock(config, now)` aplica esa pregunta a todas las reglas y devuelve los nombres
de las apps que deben bloquearse.

### Qué problema resuelve

Concentra la política de horarios en un único lugar determinista. Sin él, la lógica de "¿está en
ventana?" estaría repetida en el bucle de bloqueo, en la TUI y en cualquier consumidor futuro, con
bordes distintos cada vez (¿17:00 bloquea? ¿y 22:00–06:00?).

### Cómo funciona paso a paso

1. **¿Regla deshabilitada?** `if (!rule.Enabled) return false;`. Una regla apagada nunca bloquea,
   aunque su horario contenga el instante.
2. **¿Ventana degenerada?** `if (rule.StartTime == rule.EndTime) return false;`. Una ventana vacía no
   bloquea nunca. Este chequeo es **crítico**: sin él, la rama de wrap sería `now >= X || now < X`,
   que es una tautología y bloquearía las 24 horas.
3. **¿Ventana normal?** Si `Start < End`, la condición es `Start <= now && now < End`. El `<=` del
   inicio y el `<` del fin son la ventana semiabierta.
4. **¿Cruza medianoche?** Si `Start > End`, la condición es `now >= Start || now < End`: activa desde
   el inicio hasta la medianoche y desde la medianoche hasta el fin.
5. `GetAppsToBlock` filtra con `Where(Evaluate)` y proyecta `AppName` con `Select`.

### Qué se rompería sin esto en FocusBlock

- Si el motor leyera el reloj internamente, los tests del wrap dependerían de correr a las 23:00:
  *flaky* e imposibles de reproducir. La inyección de `now` es lo que hace que los 13 tests de
  `BlockEngineTests` sean deterministas.
- Sin el chequeo de degenerada, un `StartTime == EndTime` (fácil de escribir en un formulario)
  bloquearía la app todo el día.
- Sin la ventana semiabierta, dos reglas contiguas (09:00–17:00 y 17:00–22:00) se pisarían en el
  instante exacto de las 17:00.

### Para qué sirve en este proyecto

Es el corazón de la decisión. `BlockCoordinator.RunOnceAsync` llama `GetAppsToBlock` una vez por tick
y usa el resultado como lista de trabajo. La TUI construye las reglas (`AddBlockView`), pero **no
decide**: la decisión vive en el daemon, que es el lado confiable.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Daemon/Services/BlockEngine.cs:33-50
public bool Evaluate(BlockRuleConfig rule, TimeOnly now)
{
    if (!rule.Enabled || rule.StartTime == rule.EndTime)
    {
        return false;
    }

    return rule.StartTime < rule.EndTime
        ? rule.StartTime <= now && now < rule.EndTime     // normal window
        : now >= rule.StartTime || now < rule.EndTime;    // crosses midnight
}

public IReadOnlyList<string> GetAppsToBlock(AppConfig config, TimeOnly now) =>
    config.BlockRules.Where(rule => Evaluate(rule, now)).Select(rule => rule.AppName).ToList();
```

### Error común

1. **Usar `DateTime.Now` dentro del motor.** El test pasa a depender de la hora real. Detección:
   el motor no debería tener `using System;` para leer el reloj ni campos de tiempo.
2. **Olvidar la ventana degenerada.** Síntoma: una app bloqueada 24/7 sin regla que lo explique.
   Detección: `Evaluate(rule con Start == End, cualquier hora)` debe ser `false`.
3. **Confundir `TimeOnly` con `TimeSpan`.** `DateTimeOffset.TimeOfDay` devuelve `TimeSpan`, no
   `TimeOnly` (bug real de la fase). Se resuelve con `TimeOnly.FromDateTime(offset.DateTime)`.
4. **Asumir ventana cerrada.** Si pensás que 17:00 bloquea, el `EndTime` se te corre una unidad.
   Detección: el test de borde `ReturnsNoBlock_OnEndBoundary`.
5. **Evaluar el wrap con una sola comparación.** `22:00 <= now && now < 06:00` es imposible
   (ninguna hora es simultáneamente mayor a 22 y menor a 6). El `||` no es opcional.

### Para profundizar

- [`TimeOnly` (Microsoft Learn)](https://learn.microsoft.com/dotnet/api/system.timeonly)
- [Funciones puras (concepto)](https://en.wikipedia.org/wiki/Pure_function)
- Código: `src/FocusBlock.Daemon/Services/BlockEngine.cs` ·
  Tests: `tests/FocusBlock.Tests.Unit/BlockEngineTests.cs`.

---

## Thread-safety y colecciones concurrentes

### En una frase

Cuando dos hilos tocan el mismo diccionario a la vez, el problema no es solo "quién gana": la
estructura interna puede quedar corrupta; `ConcurrentDictionary` cambia esa historia.

### Fundamentos previos

**¿Qué es un hilo (thread)?** La unidad que el sistema operativo planifica en un núcleo. Varios
hilos comparten la memoria del proceso, así que pueden tocar los mismos objetos al mismo tiempo.
En .NET, `Task`/`async` multiplexa trabajo sobre hilos del *thread pool*.

**¿Qué es una race condition?** Ocurre cuando el resultado depende del orden en que dos hilos
ejecutan operaciones sobre un dato compartido. Ejemplo: el Worker lee "¿firefox está en cooldown?"
mientras el handler IPC escribe "firefox entra en cooldown". Si la lectura gana la carrera, el
Worker mata firefox justo después de que el usuario lo desbloqueó.

**¿Qué es atomicidad?** Una operación atómica se ejecuta completa o no se ejecuta; no se puede
observar a la mitad. `_expiries[app] = value` en un `ConcurrentDictionary` es atómico.

**¿Por qué un `Dictionary<K,V>` normal no es thread-safe?** Sus operaciones asumen acceso exclusivo.
Si dos hilos escriben a la vez y el diccionario necesita **redimensionar** (rehash), uno puede ver
el array interno a medio construir: entradas perdidas, lecturas incorrectas o bucles infinitos.
No es un "quién gana": es corrupción.

**¿Qué es `ConcurrentDictionary`?** La versión thread-safe de .NET. Internamente usa operaciones
atómicas y *lock striping*: no lockea todo el mapa en cada acceso, así que la contención es baja.
Para escribir una clave nueva o pisar una existente alcanza con el indexador.

**¿Qué es `StringComparer.Ordinal`?** Un comparador byte a byte, independiente de la cultura. Sin
él, `"firefox"` y `"Firefox"` podrían considerarse iguales o no según el locale de la máquina.
Acá las claves son nombres de proceso del kernel: comparación exacta.

**¿Por qué hay concurrencia en FocusBlock?** El `Worker` corre su tick en un hilo del pool; el
`IpcServer` atiende cada conexión en su propia task. Ambos tocan el mismo `CooldownManager`: el
tick lee, el handler IPC escribe.

### Qué es

`CooldownManager` guarda un mapa `nombre de app → instante de expiración` y responde si una app está
en cooldown. Es la memoria de gracia del daemon.

### Qué problema resuelve

Evita que el bloqueo pise la decisión del usuario. Después de un early stop, la app entra en cooldown
y el tick siguiente la saltea. Y al ser thread-safe, la lectura del Worker y la escritura del handler
IPC pueden coincidir sin corromper el mapa.

### Cómo funciona paso a paso

1. `StartCooldown(app, now, duration)` escribe `_expiries[app] = now + duration`. Si la app ya tenía
   expiración, la nueva la pisa: la política es **last-writer-wins** (extender el cooldown es
   intencional, no un error).
2. `IsOnCooldown(app, now)` hace `TryGetValue` y compara `now < expiry`.
3. El instante entra por parámetro, igual que en `BlockEngine`: sin reloj oculto, determinista.
4. En `now == expiry` ya **no** está en cooldown: la ventana también es semiabierta.
5. El test `IsThreadSafe_UnderConcurrentAccess` lanza `Parallel.For(0, 200, ...)` escribiendo una
   clave compartida y 200 claves distintas: si el mapa no fuera concurrente, fallaría o corrompería.

### Qué se rompería sin esto en FocusBlock

- Con un `Dictionary` normal, el daemon podría crashear o perder entradas justo cuando el usuario
  pide un early stop, que es el peor momento posible.
- Sin el manager, el early stop duraría un tick (5 segundos): el Worker volvería a matar la app que
  el usuario acaba de desbloquear.
- Sin `now` por parámetro, el test de expiración tendría que dormir minutos reales.

### Para qué sirve en este proyecto

`BlockCoordinator` consulta `IsOnCooldown` antes de matar cada app; `DaemonRequestHandler` llama
`StartCooldown` cuando la contraseña es correcta. Es el puente de estado entre el flujo IPC y el
bucle de bloqueo.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Daemon/Services/CooldownManager.cs (archivo completo)
public class CooldownManager
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _expiries =
        new(StringComparer.Ordinal);

    public void StartCooldown(string appName, DateTimeOffset now, TimeSpan duration) =>
        _expiries[appName] = now + duration;

    public bool IsOnCooldown(string appName, DateTimeOffset now) =>
        _expiries.TryGetValue(appName, out DateTimeOffset expiry) && now < expiry;
}

// src/FocusBlock.Daemon/Services/BlockCoordinator.cs:57-60
if (_cooldowns.IsOnCooldown(app, nowUtc))
{
    continue;
}
```

### Error común

1. **"Con un `lock` alcanza".** Funciona, pero bloquea todo el mapa en cada operación;
   `ConcurrentDictionary` está diseñado para esto y evita el lock global.
2. **Creer que thread-safe = transaccional.** `IsOnCooldown` y `StartCooldown` son atómicas por
   separado; un "si no está en cooldown, matalo" no es una operación atómica (check-then-act).
   Acá el riesgo es aceptable: el peor caso es un kill de más o de menos en un tick.
3. **Comparar claves con la cultura por defecto.** `Dictionary` sin comparador usa
   `EqualityComparer<string>.Default`, que es sensible a la cultura en algunos escenarios. El
   `StringComparer.Ordinal` explícito alinea el cooldown con el matching de procesos (`Ordinal`).
4. **Iterar y mutar a la vez sin entender el enumerador.** El enumerador de
   `ConcurrentDictionary` es seguro (no lanza), pero refleja un estado instantáneo, no un snapshot
   transaccional.

### Para profundizar

- [Colecciones thread-safe (Microsoft Learn)](https://learn.microsoft.com/dotnet/standard/collections/thread-safe/)
- [`ConcurrentDictionary<TKey,TValue>`](https://learn.microsoft.com/dotnet/api/system.collections.concurrent.concurrentdictionary-2)
- Código: `src/FocusBlock.Daemon/Services/CooldownManager.cs` ·
  Tests: `tests/FocusBlock.Tests.Unit/CooldownManagerTests.cs`.

---

## El bucle de bloqueo

### En una frase

Un tick cada 5 segundos pregunta "¿qué está bloqueado ahora?", lee `/proc` una vez, descarta lo que
está en cooldown y mata cada PID que corresponda.

### Fundamentos previos

**Repaso de Fase 3:** el `Worker` es un `BackgroundService` con un tick inyectable; `ProcessMonitor`
lee `/proc`; `BlockEnforcer` escala SIGTERM → SIGKILL. La pieza que faltaba era **unir nombre y PID**:
el monitor devolvía nombres, el enforcer necesitaba PIDs.

**¿Qué es un `record struct`?** Un tipo de valor con igualdad estructural (dos `ProcessInfo` son
iguales si su `Pid` y `Name` lo son). `ProcessInfo(int Pid, string Name)` es el par que viaja del
monitor al coordinador.

**¿Qué es un orquestador?** Un componente que coordina a otros sin contener la política. El
`BlockCoordinator` no decide *cuándo* bloquear (eso es `BlockEngine`) ni *cómo* matar (eso es
`BlockEnforcer`): decide *en qué orden* se consultan y se ejecutan.

**¿Qué es `TimeProvider`?** La abstracción del reloj de .NET 8+. `TimeProvider.System` usa el reloj
real; un doble (`FixedTimeProvider`) devuelve siempre el mismo instante y una zona local fija. Es el
seam del reloj del coordinador.

**¿Por qué una sola lectura de `/proc`?** `GetProcesses()` es lazy (`yield return`): si no se
materializa, cada iteración del `foreach` externo volvería a recorrer los directorios. Con
`.ToList()` se congela una foto por pasada.

**¿Qué es el dedup de PIDs?** `HashSet<int>.Add(pid)` devuelve `false` si el PID ya estaba. Sirve
para no mandar dos SIGTERM al mismo proceso cuando dos reglas activas apuntan a la misma app.

### Qué es

El bucle de bloqueo son cuatro piezas conectadas: `Worker` (período) → `BlockCoordinator.RunOnceAsync`
(orquestación) → `ProcessMonitor.GetProcesses()` (nombre + PID) → `BlockEnforcer` (señales), con
`CooldownManager` y `BlockEngine` como consultas.

### Qué problema resuelve

Cierra el cableado end-to-end que Fase 3 dejó pendiente: convierte reglas y procesos en kills
reales, una vez por tick, sin releer `/proc` de más ni volver a matar lo que está en gracia.

### Cómo funciona paso a paso

1. El host arranca el `Worker` con `TimeSpan.FromSeconds(5)` y un tick que llama
   `BlockCoordinator.RunOnceAsync(ct)` (`Program.cs:30-32`).
2. `RunOnceAsync` lee el reloj **una sola vez**: `nowUtc` para cooldowns y
   `nowLocal = TimeOnly.FromDateTime(_time.GetLocalNow().DateTime)` para las reglas. Todas las apps
   de la pasada se evalúan contra el mismo instante.
3. `_engine.GetAppsToBlock(_config, nowLocal)`: si no hay nada que bloquear, **early return**. No se
   toca `/proc`.
4. `_monitor.GetProcesses().ToList()`: una única lectura de `/proc` por pasada, materializada.
5. Por cada app: si `IsOnCooldown(app, nowUtc)` → `continue` (el early stop manda).
6. Por cada proceso: si `process.Name == app` (comparación `Ordinal`) y el PID no fue matado ya
   (`killedPids.Add`) → `KillProcessAsync(pid, ct)`.
7. `BlockEnforcer` hace SIGTERM → espera el período de gracia → SIGKILL si sigue vivo (Fase 3).
8. Al apagar el host, el token cancela el `Task.Delay` del Worker y el bucle termina sin error.

### Qué se rompería sin esto en FocusBlock

- Sin el tick real, `BlockEngine` y `BlockEnforcer` existirían pero nadie los llamaría: el daemon
  escanearía y no bloquearía nada.
- Sin el early return, cada tick leería `/proc` (cientos de archivos) aunque no hubiera reglas
  activas.
- Sin dedup, dos reglas solapadas para `firefox` mandarían dos SIGTERM al mismo PID en la misma
  pasada.
- Sin cooldown, el early stop duraría hasta el próximo tick.

### Para qué sirve en este proyecto

Es el runtime del bloqueador. El smoke test de la fase levantó el daemon real y verificó que
responde IPC, con este bucle registrado en DI.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Daemon/Program.cs:30-32
builder.Services.AddHostedService(sp => new Worker(
    TimeSpan.FromSeconds(5),
    ct => sp.GetRequiredService<BlockCoordinator>().RunOnceAsync(ct)));

// src/FocusBlock.Daemon/Services/BlockCoordinator.cs:41-71 (extracto)
public async Task RunOnceAsync(CancellationToken ct = default)
{
    DateTimeOffset nowUtc = _time.GetUtcNow();
    TimeOnly nowLocal = TimeOnly.FromDateTime(_time.GetLocalNow().DateTime);

    IReadOnlyList<string> appsToBlock = _engine.GetAppsToBlock(_config, nowLocal);
    if (appsToBlock.Count == 0)
    {
        return;
    }

    List<ProcessInfo> processes = _monitor.GetProcesses().ToList();
    var killedPids = new HashSet<int>();

    foreach (string app in appsToBlock)
    {
        if (_cooldowns.IsOnCooldown(app, nowUtc))
        {
            continue;
        }

        foreach (ProcessInfo process in processes)
        {
            if (!string.Equals(process.Name, app, StringComparison.Ordinal)
                || !killedPids.Add(process.Pid))
            {
                continue;
            }

            await _enforcer.KillProcessAsync(process.Pid, ct);
        }
    }
}

// src/FocusBlock.Daemon/Services/ProcessInfo.cs (archivo completo)
public readonly record struct ProcessInfo(int Pid, string Name);
```

### Punto de inyección (si aplica)

El reloj es `TimeProvider` (`TimeProvider.System` en producción; `FixedTimeProvider` en tests, que
además fija `LocalTimeZone = UTC` para que `nowLocal` sea determinista). Los seams de Fase 3
(`IProcessSource`, `ISignalSender`) se heredan a través del monitor y del enforcer.

### Error común

1. **El gotcha del `comm`:** `Name:` en `/proc/<pid>/status` es el **`comm` del kernel, limitado a
   15 caracteres**. Un `AppName` más largo nunca matchea: `google-chrome-stable` aparece como
   `google-chrome-s`. Es una limitación conocida; la mitigación completa (leer `cmdline` o
   `exe`) queda para una fase futura.
2. **`DateTimeOffset.TimeOfDay` es `TimeSpan`, no `TimeOnly`.** Es el bug de spec real de la fase:
   el código no compila si intentás pasar `TimeOfDay` a `Evaluate`. Se resuelve con
   `TimeOnly.FromDateTime(offset.DateTime)`.
3. **Releer `/proc` por app** (N apps × M procesos). Una lectura por pasada alcanza: la foto dura
   milisegundos.
4. **Leer el reloj dos veces** y evaluar reglas y cooldowns contra instantes distintos. En el borde
   exacto de una ventana, el resultado sería inconsistente.
5. **Asumir que el PID sigue siendo el mismo proceso** entre el scan y el kill: el kernel puede
   reciclarlo. Race conocida y documentada en `BlockEnforcer`.

### Para profundizar

- [`proc(5)` — man page](https://man7.org/linux/man-pages/man5/proc.5.html)
- [`TimeProvider` (Microsoft Learn)](https://learn.microsoft.com/dotnet/api/system.timeprovider)
- Diagrama: `docs/diagrams/phase-04-blocker.md` (una pasada de bloqueo, paso a paso).
- Código: `src/FocusBlock.Daemon/Services/BlockCoordinator.cs`, `ProcessMonitor.cs`,
  `ProcessInfo.cs` · Tests: `tests/FocusBlock.Tests.Unit/BlockCoordinatorTests.cs`.

---

## Early stop: fricción vs seguridad

### En una frase

Para cortar un bloqueo antes de tiempo hay dos cerrojos distintos: uno de **fricción** (tipear 4
palabras) que frena el impulso, y uno de **seguridad** (contraseña Argon2id) que frena la decisión.

### Fundamentos previos

**¿Qué es la fricción en UX?** Un costo deliberado que se interpone entre la intención y la acción.
No busca impedir: busca que la acción sea consciente. Tipear `cobalt-otter-lantern-drift` toma unos
segundos y obliga a mirar la pantalla.

**Repaso de Fase 2:** Argon2id es *memory-hard* (cada verificación reserva 16 MB) y
`FixedTimeEquals` compara en tiempo constante. La sal es única y no secreta; el hash se recalcula,
nunca se descifra.

**¿Qué es un secreto y qué no?** El challenge **no es secreto**: se muestra en la pantalla y el
usuario lo copia. La contraseña **sí es secreta**: se tipea en un campo enmascarado y solo el daemon
la conoce.

**¿Qué es un timing attack?** Medir cuánto tarda una comparación para deducir cuántos bytes
coinciden al principio. Solo aplica a secretos: no tiene sentido proteger el tiempo de comparación
de un texto que está impreso en pantalla.

**Diálogos modales en Terminal.Gui v2:** `_app.Run(dialog)` bloquea hasta que el diálogo se cierra;
`Dialog.Result` es el índice (base 0) del botón presionado, o `null` si se cerró con Escape. Ambos
diálogos agregan OK primero, así que `Result == 0` significa aceptar.

### Qué es

El early stop es el camino inverso del bloqueo: `ChallengeSystem` genera la fricción (y expone
`Matches` para comparar), `ChallengeDialog` la pide y la valida en el flujo productivo,
`PasswordDialog` pide la contraseña, `EarlyStopService` orquesta el flujo y `TerminalEarlyStopPrompt`
es el adaptador real de UI. Del lado del daemon, `BlockEngine.TryEarlyStop` verifica la contraseña y
`DaemonRequestHandler` arranca el cooldown.

### Qué problema resuelve

Un early stop sin fricción es un botón de "desbloquear" que se aprieta por impulso; una fricción sin
contraseña es teatro. Los dos juntos hacen que recuperar el control sea **deliberado y autorizado**,
sin volver imposible el caso legítimo.

### Cómo funciona paso a paso

1. `EarlyStopService.RequestEarlyStopAsync(appName)` genera un challenge:
   `ChallengeSystem.GenerateChallenge()` elige 4 palabras de 12 con `RandomNumberGenerator.GetInt32`
   y las une con `-` (espacio de 12⁴ = 20 736 combinaciones).
2. `_prompt.AskForPassword(challenge)` corre el `ChallengeDialog` modal. Si `Result != 0` (canceló)
   o `ValidateInput()` no matchea → devuelve `null` y **no se envía nada**.
3. Si pasa, corre el `PasswordDialog` modal con `Secret = true`; devuelve el texto o `null` si está
   vacío.
4. `EarlyStopService` manda `IpcMessage(ForceStop, AppName, Password)` por el socket.
5. El daemon valida y verifica con `TryEarlyStop` → `IPasswordVerifier.VerifyPassword` (Argon2id +
   `FixedTimeEquals`).
6. Si la contraseña es correcta: `StartCooldown(app, now, CooldownMinutes)` y responde `Ok`; si no,
   `Error` sin tocar el cooldown.
7. La TUI traduce la respuesta a un mensaje y lo muestra con `MessageBox.Query` **marshalado al hilo
   de UI** con `_app.Invoke(...)`.
8. **Por qué el challenge no usa `FixedTimeEquals`:** no es secreto. Un atacante que mida tiempos ya
   ve el challenge en pantalla; la comparación es `string.Equals(..., Ordinal)`.
9. **Por qué la contraseña sí:** es secreta y el hash vive en disco; comparar con `==` filtraría
   cuántos bytes iniciales coinciden. `FixedTimeEquals` recorre siempre el mismo trabajo.

### Qué se rompería sin esto en FocusBlock

- Sin challenge, el early stop sería un clic: la app "anti-impulso" fallaría en su propósito.
- Sin contraseña, cualquiera con acceso a la TUI desbloquearía.
- Si la contraseña se validara en la TUI (lado usuario), el gate sería modificable: la decisión
  **tiene** que vivir en el daemon root.
- Si `VerifyPassword` usara `==`, la comparación filtraría información por timing.
- Sin `Dialog.Result`, no habría forma de distinguir OK de Cancel en un diálogo modal.

### Para qué sirve en este proyecto

Es la salida de emergencia controlada del sistema. `BlockEngine.TryEarlyStop` es el gate;
`DaemonRequestHandler` lo llama desde el handler IPC real; la TUI aporta fricción y transporte.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Tui/Services/ChallengeSystem.cs:31-47 (extracto)
public string GenerateChallenge()
{
    var words = new string[WordCount];
    for (int i = 0; i < WordCount; i++)
    {
        words[i] = Words[RandomNumberGenerator.GetInt32(Words.Length)];
    }

    return string.Join('-', words);
}

public bool Matches(string input, string challenge) =>
    string.Equals(input.Trim(), challenge, StringComparison.Ordinal);

// src/FocusBlock.Tui/Services/EarlyStopService.cs:33-49 (extracto)
string challenge = _challenges.GenerateChallenge();
string? password = _prompt.AskForPassword(challenge);
if (password is null)
{
    return false;
}

try
{
    IpcMessage response = await _client.SendAsync(
        new IpcMessage(MessageType.ForceStop, AppName: appName, Password: password), ct);
    return response.Type == MessageType.Ok;
}
catch (IOException)
{
    return false;   // daemon down or connection dropped: degrade gracefully
}

// src/FocusBlock.Daemon/Services/BlockEngine.cs:22-23
public bool TryEarlyStop(string password, SecurityConfig security) =>
    _verifier.VerifyPassword(password, security.PasswordHash, security.PasswordSalt);
```

### Punto de inyección (si aplica)

`IIpcClient` y `IEarlyStopPrompt` aíslan el socket y la UI: `EarlyStopServiceTests` usa un client
fake y un prompt fake, sin abrir Terminal.Gui. `ChallengeSystem` no tiene seam porque usa el CSPRNG
del SO; se testean propiedades (4 palabras, dos llamadas distintas) en lugar de valores exactos.

### Error común

1. **Usar `System.Random` para el challenge.** Es predecible con la semilla; para aleatoriedad
   criptográfica va `RandomNumberGenerator` (CSPRNG). Detección: buscar `new Random(`.
2. **Usar `FixedTimeEquals` para el challenge.** No aporta (no es secreto) y complica el tipo.
   Simétrico: usar `==` para la contraseña **sí** es un bug de seguridad.
3. **Validar la contraseña en la TUI.** El lado usuario no es confiable; `TryEarlyStop` vive en el
   daemon justamente por eso.
4. **Confundir `Dialog.Result`.** Es el índice del botón: OK = 0 porque se agregó primero; `null` =
   Escape. Un `Result != 0` bien usado evita aceptar un diálogo cancelado.
5. **Asumir que `SocketException` deriva de `IOException`.** No deriva (gotcha real de la fase).
   `EarlyStopService` catchea ambas por separado para degradar a `false` cuando el daemon no está.
6. **Duplicar la comparación del challenge.** Hoy `ChallengeSystem.Matches` y
   `ChallengeDialog.ValidateInput` implementan el mismo `string.Equals` ordinal; el flujo productivo
   usa la del diálogo. No es un bug, pero es una fuente de divergencia si una de las dos cambia.

### Para profundizar

- [CSPRNG y `RandomNumberGenerator` (Microsoft Learn)](https://learn.microsoft.com/dotnet/api/system.security.cryptography.randomnumbergenerator)
- `docs/learning/phase-02-config.md` (Argon2id, salt, timing attacks)
- Diagrama: `docs/diagrams/phase-04-blocker.md` (early stop TUI ↔ daemon, paso a paso).
- Código: `src/FocusBlock.Tui/Services/`, `src/FocusBlock.Tui/Views/ChallengeDialog.cs`,
  `PasswordDialog.cs` · Tests: `ChallengeSystemTests`, `EarlyStopServiceTests`, `ChallengeDialogTests`.

---

## El contrato IPC y su evolución

### En una frase

El contrato es el vocabulario compartido entre TUI y daemon; agregar `ForceStop` y el campo
`Password` es una evolución compatible, siempre que el formato (snake_case, enums string) sea
idéntico de ambos lados.

### Fundamentos previos

**Repaso de Fase 3:** el transporte es un Unix domain socket con framing newline-delimited: una
línea JSON = un mensaje. `IpcMessage` es el record que viaja en ambas direcciones.

**¿Qué es un wire format?** La representación serializada concreta de un mensaje: nombres de
propiedades, formato de enums, codificación. El wire format es un contrato entre procesos que pueden
estar compilados en momentos distintos.

**¿Por qué enums como string?** Si el enum viaja como número, agregar o reordenar un valor cambia
el significado de los mensajes ya enviados. Como string (`"force_stop"`), el nombre es el contrato y
el orden interno deja de importar.

**¿Por qué compartir `IpcJson.Options`?** `JsonSerializerOptions` cachea metadata de tipos en su
primer uso; además, dos copias de las opciones pueden divergir (una con snake_case, otra sin).
`IpcJson.Options` es una única instancia estática en `FocusBlock.Contracts` que ambos lados usan.

**¿Qué es compatibilidad hacia atrás?** Agregar un campo **opcional** (`Password = null`) no rompe a
un lector viejo: JSON desconocido se ignora. Renombrar o volver obligatorio un campo sí rompe.

**¿Qué es la validación del lado confiable?** El daemon no asume que la TUI mandó datos válidos:
valida `app_name` no vacío y `password` presente antes de verificar nada.

### Qué es

El contrato son `MessageType` + `IpcMessage` + `IpcJson.Options` en `FocusBlock.Contracts`, y su
implementación de servidor es `DaemonRequestHandler`, que rutea `status` y `force_stop`.

### Qué problema resuelve

Sin contrato compartido, cada lado inventaría su JSON y el `snake_case` de uno no matchearía el
PascalCase del otro. El record posicional con campos opcionales mantiene el tipado, permite
evolucionar sin romper y documenta qué campos usa cada mensaje.

### Cómo funciona paso a paso

1. La TUI construye `new IpcMessage(MessageType.ForceStop, AppName: app, Password: pw)`.
2. `IpcClient` serializa con `IpcJson.Options` y escribe una línea:
   `{"type":"force_stop","app_name":"firefox","password":"..."}\n`.
3. `IpcServer` lee una línea con `ReadLineAsync`, deserializa con **las mismas** opciones y llama al
   `IIpcRequestHandler` real.
4. `DaemonRequestHandler` valida los campos: si falta `app_name` o `password` → `Error` con detalle.
5. Verifica con `TryEarlyStop`; si falla → `Error` (`"invalid password"`), sin tocar cooldown.
6. Si acierta → `StartCooldown` con `CooldownMinutes` de la config y responde `Ok`.
7. La respuesta se serializa con las mismas opciones y vuelve por la misma conexión. **La contraseña
   nunca se hace eco** ni se loguea.
8. `IpcClient` cierra la conexión después de cada request (connect-per-request): simple y suficiente
   para un canal de baja frecuencia.

### Qué se rompería sin esto en FocusBlock

- Si un lado usara opciones propias (PascalCase), el otro no encontraría `type` y todo `force_stop`
  fallaría con error de parseo.
- Si los enums viajaran como número, un reordenamiento futuro del enum cambiaría el protocolo en
  silencio.
- Si el handler no validara, un `force_stop` sin app llegaría a `StartCooldown` con clave vacía y un
  `force_stop` sin password llegaría a Argon2id con `null`.
- Si la respuesta hiciera eco de la contraseña, el secreto viajaría de vuelta y podría quedar en logs.

### Para qué sirve en este proyecto

Es el plano de control que hace posible el early stop remoto: la TUI (usuario) pide, el daemon
(root) decide. Es también la evolución del contrato de Fase 3: `ForceStop` y el campo `Password` se
**agregaron en esta fase** (el enum original no los tenía) sin romper los mensajes existentes,
porque se sumaron como valor nuevo y campo opcional.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Contracts/IpcProtocol.cs:21-25, 43-48
/// <summary>
/// Request: stop blocking an app early. The daemon verifies the attached
/// <see cref="IpcMessage.Password"/> before granting the early stop.
/// </summary>
ForceStop,

public record IpcMessage(
    MessageType Type,
    string? AppName = null,
    string? Schedule = null,
    string? Detail = null,
    string? Password = null);

// src/FocusBlock.Contracts/IpcJson.cs (archivo completo)
public static class IpcJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };
}

// src/FocusBlock.Daemon/Services/DaemonRequestHandler.cs:35-42, 52-70 (extracto)
IpcMessage response = request.Type switch
{
    MessageType.Status => new IpcMessage(MessageType.StatusResponse, Detail: "daemon running"),
    MessageType.ForceStop => HandleForceStop(request),
    _ => new IpcMessage(MessageType.Error, Detail: $"Unsupported message type: {request.Type}"),
};

private IpcMessage HandleForceStop(IpcMessage request)
{
    if (string.IsNullOrWhiteSpace(request.AppName) || request.Password is null)
    {
        return new IpcMessage(
            MessageType.Error, Detail: "force_stop requires app_name and password");
    }

    if (!_engine.TryEarlyStop(request.Password, _config.Security))
    {
        return new IpcMessage(MessageType.Error, Detail: "invalid password");
    }

    _cooldowns.StartCooldown(
        request.AppName, _time.GetUtcNow(),
        TimeSpan.FromMinutes(_config.Security.CooldownMinutes));

    return new IpcMessage(MessageType.Ok);
}
```

### Punto de inyección (si aplica)

Dos seams ya existentes de Fase 3: la **ruta del socket** (los tests usan un path temporal) y el
**`IIpcRequestHandler`** (el servidor se testea con un handler fake). Del lado TUI, `IIpcClient`
permite testear `EarlyStopService` sin socket. El round-trip real se cubre en `IpcClientTests`
(cliente y servidor reales, path temporal) y en el smoke test manual del daemon.

### Error común

1. **Duplicar las `JsonSerializerOptions` en cada lado.** Divergen con el tiempo; por eso se
   extrajo `IpcJson.Options` (commit `018cb5e`).
2. **Loguear o devolver la contraseña.** El secreto entra al daemon y no sale; las respuestas solo
   dicen `Ok` o `Error`.
3. **Agregar un campo obligatorio al record.** Rompe a los clientes viejos; los campos nuevos se
   agregan con default (`null`).
4. **Confiar en que la TUI validó.** El daemon valida siempre: la TUI corre como usuario y no es
   confiable.
5. **Olvidar que el framing es por `\n`.** Un mensaje sin newline deja al servidor esperando; el
   `WriteLineAsync` del cliente es lo que cierra el mensaje.

### Para profundizar

- `docs/architecture.md` (Protocolo IPC) · `docs/adr/ADR-002-ipc-unix-socket.md`
- [System.Text.Json (Microsoft Learn)](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/)
- Código: `src/FocusBlock.Contracts/IpcProtocol.cs`, `IpcJson.cs`,
  `src/FocusBlock.Daemon/Services/DaemonRequestHandler.cs`,
  `src/FocusBlock.Tui/Services/IpcClient.cs` · Tests: `DaemonRequestHandlerTests`, `IpcClientTests`.

---

## Inversión de dependencias en la práctica

### En una frase

El daemon (root) necesitaba verificar la contraseña y leer la config, pero esas clases vivían en la
TUI (usuario); extraer `FocusBlock.Core` y exponer `IPasswordVerifier` resolvió el problema sin que
el daemon dependa de la TUI.

### Fundamentos previos

**¿Qué es la inversión de dependencias (DIP)?** El principio de que los módulos de alto nivel no
dependan de los de bajo nivel; ambos dependen de abstracciones. Y las abstracciones no dependen de
los detalles: los detalles dependen de las abstracciones.

**¿Qué es un puerto y qué un adaptador?** En arquitectura hexagonal, el **puerto** es la interfaz que
declara lo que el dominio necesita (`IPasswordVerifier`); el **adaptador** es la implementación
concreta que lo cumple (`AuthService`, con Argon2id).

**¿Qué es la dirección de una dependencia en .NET?** Un `ProjectReference` es dirigido: si
`Daemon → Tui`, compilar el daemon arrastra Terminal.Gui y todo el stack de UI. Si `Daemon → Core`
y `Tui → Core`, ambos comparten lógica sin arrastrarse entre sí.

**¿Por qué no copiar el código?** Dos copias de `AuthService` pueden divergir (parámetros de Argon2id
distintos, un fix aplicado solo en un lado). El hash de la TUI y la verificación del daemon **deben**
ser el mismo algoritmo.

**¿Qué es registrar un puerto en DI?** `AddSingleton<IPasswordVerifier, AuthService>()` le dice al
contenedor: "cuando alguien pida `IPasswordVerifier`, entregá un `AuthService`". Quien consume pide la
abstracción, no la clase.

### Qué es

`FocusBlock.Core` es un proyecto nuevo con la lógica compartida entre TUI y daemon:
`IPasswordVerifier` (puerto), `AuthService` (adaptador Argon2id) y `ConfigService` (carga/guardado de
config). TUI y Daemon lo referencian; ninguno referencia al otro.

### Qué problema resuelve

Elimina la dependencia "daemon → TUI" y la duplicación de lógica. Antes de la extracción, el daemon
no tenía forma limpia de verificar una contraseña: `AuthService` y `ConfigService` eran de la TUI.

### Cómo funciona paso a paso

1. **Antes:** `AuthService` y `ConfigService` vivían en `FocusBlock.Tui/Services/`.
2. **Necesidad:** el daemon debe verificar la contraseña (early stop) y cargar la config al arrancar
   (`Program.cs`).
3. **Opción mala A — copiar:** duplica Argon2id; dos implementaciones que se desincronizan.
4. **Opción mala B — que el daemon referencie la TUI:** arrastra Terminal.Gui a un servicio root y
   crea una dependencia circular conceptual (el servicio depende de la app que controla).
5. **Opción elegida:** extraer `FocusBlock.Core`; declarar `IPasswordVerifier` como puerto;
   `AuthService` lo implementa; ambos proyectos referencian Core (y Core referencia Contracts).
6. **DI:** `builder.Services.AddSingleton<IPasswordVerifier, AuthService>()`; `BlockEngine` recibe
   la interfaz y nunca conoce la implementación.
7. **Refactor con red:** `AuthServiceTests` y `ConfigServiceTests` solo cambiaron de namespace
   (`FocusBlock.Tui.Services` → `FocusBlock.Core`); la lógica no se tocó y los tests siguieron
   verdes.

### Qué se rompería sin esto en FocusBlock

- El daemon no podría verificar la contraseña: el early stop sería imposible (o se validaría en la
  TUI, que es el lado no confiable).
- La config del daemon y la de la TUI podrían leerse con serializadores distintos y divergir.
- La dirección de dependencia quedaría invertida: un servicio de sistema (root) acoplado a una app
  de usuario.

### Para qué sirve en este proyecto

`BlockEngine` depende de `IPasswordVerifier` (no de `AuthService`); `DaemonRequestHandler` verifica
en el lado confiable; `Program.cs` del daemon carga la config con `ConfigService`. La extracción es
la que hizo posible el gate de seguridad del early stop.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Core/IPasswordVerifier.cs (archivo completo)
public interface IPasswordVerifier
{
    bool VerifyPassword(string password, string hash, string salt);
}

// src/FocusBlock.Core/AuthService.cs:20-26
public bool VerifyPassword(string password, string hash, string salt)
{
    byte[] expected = Convert.FromBase64String(hash);
    byte[] saltBytes = Convert.FromBase64String(salt);
    byte[] computed = ComputeArgon2id(password, saltBytes);
    return CryptographicOperations.FixedTimeEquals(computed, expected);
}

// src/FocusBlock.Daemon/Program.cs:19, 22
builder.Services.AddSingleton<IPasswordVerifier, AuthService>();
builder.Services.AddSingleton<BlockEngine>();
```

### Error común

1. **Poner el puerto en la TUI y que el daemon la referencie.** El puerto debe vivir en un proyecto
   neutral (`Core`); el consumidor (daemon) lo referencia sin arrastrar la UI.
2. **Mover clases a mano y olvidar namespaces/usings.** El compilador lo grita, pero conviene
   hacerlo como refactor con tests verdes: acá los tests solo cambiaron de namespace.
3. **Registrar `AuthService` concreto en vez del puerto.** Funciona, pero pierde el seam: `BlockEngine`
   queda atado a la implementación y no se le puede inyectar un doble.
4. **Creer que "Core" es una capa de utilidades.** Es el dominio compartido: modelos, puertos y
   adaptadores que no dependen de UI ni del host. Si algo necesita Terminal.Gui o `/proc`, no va acá.

### Para profundizar

- [Inversión de dependencias (Martin Fowler)](https://martinfowler.com/articles/injection.html)
- `docs/development-plan.md` (Puntos de inyección) · `docs/adr/ADR-009-testing-seams.md`
- Código: `src/FocusBlock.Core/` · Tests: `AuthServiceTests`, `ConfigServiceTests`.

---

## Nota honesta: el `Channel<T>` del plan no aplicó

El plan de la fase declaró "Patrón producer/consumer con `Channel<T>`" como concepto de aprendizaje.
**No aplicó.** El `Worker` ejecuta un tick y después espera con `Task.Delay`: no hay productor y
consumidor concurrentes, ni necesidad de buffer, backpressure ni completar el canal. Meter un
`Channel<T>` habría agregado complejidad (quién completa el canal, cómo se propagan excepciones,
cuánto buffer) sin resolver ningún problema real.

Queda como concepto para cuando exista un flujo genuinamente productor/consumidor. El candidato
natural es Fase 6 (métricas): el tick **produce** eventos de bloqueo y un consumidor los **persiste**
en SQLite, probablemente sin bloquear el bucle.

---

## Relación entre estos conceptos

| Paso | Concepto | Por qué importa el orden |
|---|---|---|
| Habilitar | Inversión de dependencias (`Core`) | Sin el puerto `IPasswordVerifier`, el daemon no puede verificar la contraseña. |
| Decidir | Evaluación de reglas (`BlockEngine`) | Sin la decisión no hay nada que bloquear; es el input del bucle. |
| Modular | Thread-safety (`CooldownManager`) | Modula la decisión: el early stop gana sobre el horario durante la gracia. |
| Ejecutar | Bucle de bloqueo (`BlockCoordinator`) | Convierte la decisión en kills reales, una vez por tick. |
| Recuperar | Early stop (fricción + contraseña) | Usa el contrato IPC y el puerto de verificación; escribe el cooldown. |
| Comunicar | Contrato IPC | Transporta la intención del usuario hasta el lado confiable. |

El orden no es casual: **primero se habilita el gate de seguridad** (Core), **después se decide**
(reglas), **después se modula** (cooldown), **después se ejecuta** (bucle) y **al final se abre la
válvula de escape** (early stop), que reutiliza todo lo anterior. Si el early stop se hubiera
construido antes que el cooldown, el daemon volvería a bloquear la app cinco segundos después de
desbloquearla.

---

## Convención

- **Archivo**: `docs/learning/phase-NN-name.md`, uno por fase.
- **Título**: `# Fase NN — Tema`.
- **Código**: bloques `cs`; líneas de ~100 caracteres.
- **Secciones por concepto**: ver `docs/learning/template-phase.md`.
