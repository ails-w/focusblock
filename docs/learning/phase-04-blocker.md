# Fase 04 — Bloqueador: reglas, cooldown y early stop

> Qué construye: el daemon decide qué bloquear, lo ejecuta cada 5 segundos y permite una
> salida de emergencia con fricción y contraseña.
> Antes: [Fase 03](phase-03-daemon.md) · Después: Fase 05 (pendiente) ·
> [Glosario](glossary.md)

## Dónde estamos

Las piezas del daemon existen, pero nadie las conecta: no hay decisión de qué bloquear ni
forma de desbloquear antes de tiempo. Esta fase cierra el circuito completo.

```text
Worker (5 s) ──▶ BlockCoordinator ──▶ BlockEngine (reglas)
                    │                     │
                    ├── CooldownManager ◀─┘
                    ├── ProcessMonitor ──▶ /proc
                    └── BlockEnforcer ──▶ SIGTERM → SIGKILL

TUI ── force_stop ──▶ DaemonRequestHandler ──▶ password ──▶ cooldown
```

## Qué construye esta fase

Un motor de reglas puro, un gestor de cooldown thread-safe, el bucle de bloqueo real y el
flujo de early stop de punta a punta. El daemon pasa de escanear procesos a bloquearlos de
verdad, y la TUI obtiene una salida controlada.

## El recorrido de una acción

1. El `Worker` ejecuta un tick cada 5 segundos.
2. `BlockEngine` evalúa las reglas contra una sola lectura del reloj.
3. Si `firefox` está en horario, `BlockCoordinator` busca su PID en `/proc`.
4. `BlockEnforcer` manda `SIGTERM` y escala a `SIGKILL` si sigue vivo.
5. El usuario pide early stop: escribe 4 palabras y su contraseña.
6. El daemon verifica, arranca un cooldown y el siguiente tick saltea `firefox`.

## Los conceptos

| # | Concepto | Idea en una línea | Código |
|---|---|---|---|
| 1 | Evaluar reglas | Decidir es comparar horas, sin leer el reloj | `BlockEngine.cs` |
| 2 | Cooldown | La gracia del usuario gana sobre el horario | `CooldownManager.cs` |
| 3 | Bucle de bloqueo | Convertir reglas en kills, una vez por tick | `BlockCoordinator.cs` |
| 4 | Early stop y contrato | Fricción más contraseña, verificada del lado root | `EarlyStopService.cs` |
| 5 | Inversión de dependencias | El daemon usa la lógica sin depender de la TUI | `FocusBlock.Core/` |

---

## 1. Evaluar reglas

**Idea.** Decidir si una regla bloquea es comparar el "ahora" con dos horas. Si el "ahora"
entra por parámetro, la decisión es pura y testeable.

**Analogía.** Es un portero con una lista de turnos: no mira su reloj, le preguntas "¿son las
14:00?" y consulta la lista. Misma pregunta, misma respuesta, siempre.

**Cómo funciona.** 1) Regla apagada: no bloquea. 2) Inicio igual a fin: ventana vacía, no
bloquea. 3) Inicio menor que fin: `inicio <= ahora < fin`. 4) Inicio mayor que fin: cruza
medianoche, `ahora >= inicio || ahora < fin`.

```cs
// src/FocusBlock.Daemon/Services/BlockEngine.cs
public bool Evaluate(BlockRuleConfig rule, TimeOnly now)
{
    if (!rule.Enabled || rule.StartTime == rule.EndTime) return false;

    return rule.StartTime < rule.EndTime
        ? rule.StartTime <= now && now < rule.EndTime     // normal
        : now >= rule.StartTime || now < rule.EndTime;    // medianoche
}
```

**Trampa.** Si el motor leyera el reloj por dentro, el test del turno nocturno solo pasaría de
noche. Y sin el chequeo de ventana vacía, el caso de medianoche bloquearía las 24 horas.

<details><summary>Verifica</summary>
1. ¿Por qué la ventana es semiabierta y no cerrada?
2. ¿Qué pasa si `StartTime == EndTime` y por qué es crítico?
3. Escribe el caso 22:00-06:00: ¿por qué `&&` no funciona?
</details>

---

## 2. Cooldown

**Idea.** Después de un early stop, la app entra en una ventana de gracia. El mapa que la
guarda se lee desde el tick y se escribe desde el handler IPC, así que debe ser thread-safe.

**Analogía.** Es una amnistía temporal: el portero tiene una lista de "estos no se tocan hasta
las 14:10". Esa lista la leen y escriben dos personas a la vez sin romperla.

**Cómo funciona.** 1) `StartCooldown` escribe `expiración = ahora + duración`; si ya existía,
la pisa. 2) `IsOnCooldown` compara `ahora < expiración`. 3) `ConcurrentDictionary` hace
atómicas esas operaciones. 4) `StringComparer.Ordinal` compara nombres byte a byte.

```cs
// src/FocusBlock.Daemon/Services/CooldownManager.cs
private readonly ConcurrentDictionary<string, DateTimeOffset> _expiries =
    new(StringComparer.Ordinal);

public void StartCooldown(string appName, DateTimeOffset now, TimeSpan duration) =>
    _expiries[appName] = now + duration;

public bool IsOnCooldown(string appName, DateTimeOffset now) =>
    _expiries.TryGetValue(appName, out DateTimeOffset expiry) && now < expiry;
```

**Trampa.** Thread-safe no significa transaccional: "si no está en cooldown, matalo" son dos
operaciones separadas. Y un `Dictionary` normal puede corromperse, no solo perder una entrada.

<details><summary>Verifica</summary>
1. ¿Qué race condition evita el cooldown?
2. ¿Por qué no alcanza con un `lock` global?
3. ¿Qué significa last-writer-wins aquí y por qué es intencional?
</details>

---

## 3. Bucle de bloqueo

**Idea.** Un tick pregunta qué está bloqueado, lee `/proc` una vez, descarta el cooldown y
mata cada PID que corresponda.

**Analogía.** Es un inspector que hace una ronda: mira la lista de infracciones, revisa la
planta una sola vez y sanciona solo a quienes no tienen permiso temporal.

**Cómo funciona.** 1) El host arranca `Worker` con 5 segundos y un tick que llama
`RunOnceAsync`. 2) Se lee el reloj una vez: `nowUtc` para cooldown y `nowLocal` para reglas.
3) Si no hay apps que bloquear, retorna sin tocar `/proc`. 4) `GetProcesses().ToList()`
congela una foto. 5) Por app: si está en cooldown se saltea; si no, mata sus PIDs sin repetir.

```cs
// src/FocusBlock.Daemon/Services/BlockCoordinator.cs
DateTimeOffset nowUtc = _time.GetUtcNow();
TimeOnly nowLocal = TimeOnly.FromDateTime(_time.GetLocalNow().DateTime);

var apps = _engine.GetAppsToBlock(_config, nowLocal);
if (apps.Count == 0) return;

var processes = _monitor.GetProcesses().ToList();
var killedPids = new HashSet<int>();
```

**Trampa.** `Name:` en `/proc/<pid>/status` está truncado a 15 caracteres:
`google-chrome-stable` aparece como `google-chrome-s` y nunca coincide. Y leer el reloj dos
veces evalúa la regla y el cooldown contra instantes distintos.

<details><summary>Verifica</summary>
1. ¿Por qué se lee `/proc` una sola vez por pasada?
2. ¿Qué evita el `HashSet` de PIDs?
3. ¿Por qué el motor usa hora local y el cooldown UTC?
</details>

---

## 4. Early stop y contrato

**Idea.** Cortar un bloqueo exige fricción (4 palabras) y contraseña. La verificación ocurre
en el daemon, y el pedido viaja por un contrato compartido.

**Analogía.** Es la puerta de emergencia de un avión: una manija incómoda a propósito
(fricción) y una llave que solo el piloto tiene (contraseña). No impide salir; impide salir
sin pensar.

**Cómo funciona.** 1) `ChallengeSystem` genera 4 palabras con un CSPRNG. 2) El diálogo las
pide; si se cancela, no se envía nada. 3) Se pide la contraseña y se envía `force_stop` como
una línea JSON en `snake_case`. 4) El daemon valida, verifica y arranca el cooldown. 5) La
respuesta nunca hace eco de la contraseña.

```cs
// src/FocusBlock.Contracts/IpcProtocol.cs
public record IpcMessage(MessageType Type, string? AppName = null,
    string? Schedule = null, string? Detail = null, string? Password = null);

// src/FocusBlock.Daemon/Services/DaemonRequestHandler.cs
if (!_engine.TryEarlyStop(request.Password, _config.Security))
    return new IpcMessage(MessageType.Error, Detail: "invalid password");

_cooldowns.StartCooldown(request.AppName, _time.GetUtcNow(),
    TimeSpan.FromMinutes(_config.Security.CooldownMinutes));
```

**Trampa.** El challenge no es secreto: compararlo en tiempo constante no aporta. La
contraseña sí. Y `SocketException` no deriva de `IOException`: hay que capturar ambas.

<details><summary>Verifica</summary>
1. ¿Por qué la fricción y la contraseña protegen cosas distintas?
2. ¿Por qué el daemon valida aunque la TUI ya lo haya hecho?
3. ¿Qué hace compatible un campo nuevo en el contrato?
</details>

---

## 5. Inversión de dependencias

**Idea.** El daemon necesitaba verificar contraseñas y leer config, pero esas clases vivían en
la TUI. Se extrajo `FocusBlock.Core` con un puerto y su adaptador.

**Analogía.** En vez de que el portero dependa de la casa del vecino para consultar la lista,
la lista se muda a una oficina neutral a la que ambos tienen acceso.

**Cómo funciona.** 1) `IPasswordVerifier` declara lo que el dominio necesita (puerto). 2)
`AuthService` lo implementa con Argon2id (adaptador). 3) `Core` referencia `Contracts`; TUI y
Daemon referencian `Core`, no entre sí. 4) DI registra el puerto.

```cs
// src/FocusBlock.Core/IPasswordVerifier.cs
public interface IPasswordVerifier
{
    bool VerifyPassword(string password, string hash, string salt);
}

// src/FocusBlock.Daemon/Program.cs
builder.Services.AddSingleton<IPasswordVerifier, AuthService>();
builder.Services.AddSingleton<BlockEngine>();
```

**Trampa.** Poner el puerto en la TUI y hacer que el daemon la referencie: el puerto debe vivir
en un proyecto neutral. Y registrar la clase concreta en vez del puerto pierde el seam.

<details><summary>Verifica</summary>
1. ¿Qué diferencia hay entre puerto y adaptador?
2. ¿Por qué no copiar `AuthService` en el daemon?
3. ¿Qué se rompe si el daemon referencia la TUI?
</details>

---

## Práctica

### Mezcla

1. Una regla de 22:00 a 06:00: ¿qué condición evalúa a las 23:00 y a las 05:00?
2. ¿Qué pasa si el early stop no arranca cooldown?
3. ¿Por qué el contrato usa `snake_case` y enums string?
4. Si el daemon referenciara la TUI, ¿qué arrastraría al servicio root?

<details><summary>Respuestas</summary>
1. `ahora >= 22:00` a las 23:00 y `ahora < 06:00` a las 05:00: la rama del wrap.
2. El siguiente tick vuelve a matar la app: la gracia no existiría.
3. Para que el nombre sea el contrato y el orden del enum no importe.
4. Terminal.Gui y todo el stack de UI: un servicio root acoplado a la app.
</details>

### En código

1. Escribe el test RED de `Evaluate` con una ventana que cruza medianoche.
2. Predice el orden de señales si el proceso ignora `SIGTERM`.
3. Envía un `force_stop` sin `app_name`: ¿qué responde el handler?

### Antes de cerrar

- [ ] Puedo explicar el recorrido completo, desde el tick hasta el cooldown.
- [ ] Sé por qué la decisión vive en el daemon y no en la TUI.
- [ ] Puedo explicar los cuatro bordes de una regla de horario.

## Trampas reales de la fase

- **`DateTimeOffset.TimeOfDay` es `TimeSpan`**: se resuelve con `TimeOnly.FromDateTime(...)`.
- **`SocketException` no deriva de `IOException`**: el `catch` original no cubría el daemon caído.
- **`Dialog.Result` es el índice del botón**: OK = 0, Escape = `null`.
- **Registros DI faltantes**: sin `ProcessMonitor` ni `BlockEnforcer` registrados, el host no
  podía resolver `BlockCoordinator`.
- **`comm` truncado a 15 caracteres**: un nombre más largo nunca coincide.
- **Race de reuso de PID**: el PID puede apuntar a otro proceso entre señales.

## Nota honesta

El plan declaró `Channel<T>` como concepto de la fase. No aplicó: el bucle es secuencial, sin
productor ni consumidor. Se descartó en vez de agregar complejidad sin problema real. El
candidato natural es la fase 6, cuando el tick produzca eventos y otro los persista.

## Para profundizar

- [`ADR-009`](../adr/ADR-009-testing-seams.md) — seams y dobles.
- [`ADR-012`](../adr/ADR-012-blocking-strategies.md) — estrategias de bloqueo.
- Código: `src/FocusBlock.Daemon/Services/` y `src/FocusBlock.Core/`.
