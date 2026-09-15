# Fase 02 — Configuración: Modelos, JSON, I/O y Seguridad

> Qué se aprende: cómo se define una configuración con valores por defecto, cómo se
> persiste como JSON, cómo se lee/escribe sin bloquear y cómo se protege una contraseña.
> Log de la fase (tareas, decisiones, problemas) → `docs/progress-log/phase-02-config.md`
> Fase anterior → `docs/learning/phase-01-tui.md`
> ADRs relacionados → `docs/adr/ADR-004-argon2id.md`, `docs/adr/ADR-005-config-json.md`

## Glosario de la fase

| Término | Qué significa (en una línea) |
|---|---|
| Modelo / DTO / POCO | Clase que solo lleva datos, sin lógica de negocio. |
| Propiedad auto-implementada | Propiedad `{ get; set; }` con campo de respaldo generado. |
| Inicializador de propiedad | Valor asignado en la declaración; se ejecuta en cada `new`. |
| `null` | Referencia que no apunta a ningún objeto. |
| `NullReferenceException` | Error al usar un miembro sobre una referencia `null`. |
| `TimeOnly` | Tipo que representa una hora del día, sin fecha. |
| Target-typed new | `new(9, 0)` infiere el tipo desde el contexto. |
| Collection expression | `[]` que construye una colección; se adapta al tipo destino. |
| Contrato | Acuerdo de datos entre dos procesos independientes (TUI usuario ↔ daemon root). |
| Serializar | Convertir un objeto en texto/bytes. |
| Deserializar | Reconstruir un objeto desde texto/bytes. |
| JSON | Formato de texto con pares clave-valor anidados. |
| Round-trip | Guardar y volver a leer obteniendo el mismo objeto. |
| `JsonSerializerOptions` | Configuración del serializador (formato, indentado, etc.). |
| `WriteIndented` | Opción que produce JSON con saltos de línea y sangría (legible). |
| Operador `??` | Devuelve el lado derecho si el izquierdo es `null` (null-coalescing). |
| Hilo (thread) | Unidad de ejecución que el sistema operativo planifica en un núcleo. |
| Bloquear un hilo | Dejar un hilo esperando sin poder hacer otro trabajo. |
| `Task<T>` | Objeto que representa una operación en curso y promete un valor de tipo `T`. |
| `await` | Suspende el método hasta que la operación termine, sin bloquear el hilo. |
| `CancellationToken` | Señal cooperativa para pedir que una operación se cancele. |
| Función de hash | Transformación de un solo sentido: entra un dato, sale una huella fija. |
| Fuerza bruta | Probar todas las combinaciones posibles de contraseña. |
| Rainbow table | Tabla precalculada de hashes comunes para revertirlos al instante. |
| Sal (salt) | Bytes aleatorios únicos que se mezclan con la contraseña antes de hashear. |
| Argon2id | Hashing de contraseñas memory-hard (ganador de la Password Hashing Competition). |
| Memory-hard | Que cada intento requiera mucha memoria RAM, encareciendo el paralelismo masivo. |
| CSPRNG | Generador de números aleatorios criptográficamente seguro. |
| Base64 | Codificación de bytes en caracteres imprimibles para guardarlos como texto. |
| Timing attack | Deducir información midiendo cuánto tarda una operación. |
| Tiempo constante | Comparar sin que la duración revele cuántos bytes coinciden. |

## Mapa de conceptos

```text
Modelo (AppConfig) ──┐
                     ├──▶ ConfigSerializer ──▶ JSON (round-trip) ──▶ archivo
BlockRuleConfig ─────┘                                             │
                                                                   ▼
                                                         ConfigService (async I/O)
                                                                   │
SecurityConfig ◀── AuthService (Argon2id + sal + tiempo constante)
```

El orden importa: primero la FORMA (modelo), después la TRADUCCIÓN (serializer), luego el
TRANSPORTE (I/O async) y al final la PROTECCIÓN (auth). Cada capa tiene un solo trabajo.

---

## Modelos de configuración y valores por defecto

### En una frase

Un modelo de configuración es una clase que solo describe *qué campos* tiene la config; los
valores por defecto garantizan que un objeto recién creado sea usable.

### Fundamentos previos

**¿Qué es un modelo / DTO / POCO?**
Un POCO (Plain Old CLR Object) es una clase sin dependencias ni lógica: solo propiedades.
Un DTO (Data Transfer Object) es un objeto cuyo único trabajo es transportar datos. Un
"modelo" de configuración es lo mismo: describe la forma de los datos. ¿Por qué una clase
*sin* comportamiento? Porque su responsabilidad es ser el *contrato* de datos; si le
metés lógica, deja de ser un contrato estable y se acopla a quien la use. En FocusBlock,
`AppConfig`, `BlockRuleConfig` y `SecurityConfig` son POCOs.

**¿Qué es una propiedad auto-implementada?**
Es una propiedad como `public string AppName { get; set; }`. El compilador genera por
detrás un campo privado (el "campo de respaldo") y los métodos `get`/`set`. No escribís el
campo ni la lógica de acceso; solo declarás que la propiedad se lee y se escribe.

**¿Qué es un inicializador de propiedad?**
Es el `= valor` en la declaración: `public TimeOnly StartTime { get; set; } = new(9, 0);`.
Ese valor se asigna **en cada `new`**, como parte del constructor. No es un valor
compartido: cada instancia arranca con su propio default. Sin inicializador, el campo
queda en su valor por defecto del tipo (`null` para referencias, `0` para números).

**¿Qué es `null` y qué es `NullReferenceException`?**
`null` es una referencia que no apunta a ningún objeto. Si intentás usar un miembro sobre
`null`, el runtime lanza `NullReferenceException` (NRE). Es el error más común en C#.
Ejemplo: si `AppConfig.Security` no se inicializara y alguien hiciera
`config.Security.CooldownMinutes`, explotaría con NRE. Por eso `AppConfig` inicializa
`Security = new()`: garantiza que el objeto anidado siempre exista.

**¿Qué es `TimeOnly` y por qué NO usar `DateTime` ni `TimeSpan`?**
- `DateTime` representa un *instante*: fecha + hora. Un horario de bloqueo ("09:00") no
  tiene fecha; usar `DateTime` obligaría a inventar una fecha ficticia (¿hoy? ¿1970?) y
  traería zona horaria y día de la semana que no aplican.
- `TimeSpan` representa una *duración* ("2 horas"). Un horario no es una duración:
  "09:00" no significa "9 horas desde algún origen". Podría codificarse, pero es un abuso
  semántico y permite valores absurdos como 30 horas.
- `TimeOnly` (introducido en .NET 6) representa exactamente una hora del día, con rango
  00:00:00–23:59:59.9999999. Es el tipo correcto para "a las 9:00".

**¿Qué es un target-typed new?**
Es escribir `new(9, 0)` sin repetir el tipo. El compilador lo infiere del contexto: como
`StartTime` es `TimeOnly`, `new(9, 0)` construye un `TimeOnly(9, 0)`. Evita repetir y
alinea la legibilidad.

**¿Qué es una collection expression?**
Es la sintaxis `[]` para construir una colección. El compilador la adapta al tipo destino:
como `BlockRules` es `List<BlockRuleConfig>`, `= []` crea una lista vacía. Es la forma
moderna de escribir `new List<BlockRuleConfig>()`.

**¿Qué es un contrato entre dos procesos?**
FocusBlock tiene dos procesos: la TUI corre como usuario regular y el daemon como root. Para
que se entiendan necesitan un *contrato*: un acuerdo sobre qué datos intercambian y con qué
forma. Los modelos viven en `FocusBlock.Contracts`, un proyecto compartido, para que ambos
lados usen exactamente el mismo tipo. Sin contrato, cada lado inventaría su formato y se
rompería el IPC.

### Qué es

Tres clases de datos en `FocusBlock.Contracts`:
- `AppConfig`: la raíz. Contiene `BlockRules` (lista de reglas) y `Security`.
- `BlockRuleConfig`: una regla de bloqueo (app, horario, activa).
- `SecurityConfig`: contraseña (hash + sal) y cooldown.

Cada una asigna defaults en los inicializadores de propiedades.

### Qué problema resuelve

Una config "vacía" obligaría a validar y llenar cada campo antes de usar la app, y a
manejar `null` en cada acceso. Con defaults, un objeto recién creado es válido por
construcción: la app arranca la primera vez sin archivo y hereda los defaults del modelo.

### Cómo funciona paso a paso

1. `ConfigService.LoadAsync` no encuentra el archivo y devuelve `new AppConfig()`.
2. Al ejecutarse el constructor, C# aplica los inicializadores:
   - `BlockRules = []` → **lista vacía** (no hay ninguna regla todavía).
   - `Security = new()` → un `SecurityConfig` con `CooldownMinutes = 10`.
3. Ese objeto es válido: tiene lista de reglas vacía y seguridad configurada.
4. Cuando el usuario agregue una regla, recién ahí se instancia un `BlockRuleConfig`, que
   aporta sus propios defaults (09:00, 17:00, `Enabled = true`, `AppName = ""`).

**Punto clave (corrección de un error previo):** un `new AppConfig()` NO tiene una regla
activa. Tiene `BlockRules` **vacía**. El horario 09:00–17:00 y `Enabled = true` son
defaults de `BlockRuleConfig`, que solo existen cuando hay al menos una regla en la lista.
La app NO arranca bloqueando nada.

### Qué se rompería sin esto en FocusBlock

`ConfigServiceTests.ConfigService_LoadAsync_ReturnsDefaults_WhenFileMissing` verifica
`config.BlockRules.Should().BeEmpty()` y `config.Security.CooldownMinutes.Should().Be(10)`.
Sin defaults, la app fallaría al primer arranque (archivo inexistente) o tendría que
tener lógica de "si está vacío, completar acá". Y sin inicializar `Security`, cualquier
acceso a `config.Security.CooldownMinutes` sería un NRE.

### Para qué sirve en este proyecto

`ConfigService` devuelve `new AppConfig()` cuando no hay archivo: la app arranca bien la
primera vez. `AppConfig` es además el tipo que ambas capas (TUI y daemon) compartirán como
contrato.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Contracts/AppConfig.cs (archivo completo)
public class AppConfig
{
    public List<BlockRuleConfig> BlockRules { get; set; } = [];   // arranca VACÍA
    public SecurityConfig Security { get; set; } = new();         // nunca null
}

// src/FocusBlock.Contracts/BlockRuleConfig.cs (archivo completo)
public class BlockRuleConfig
{
    public string AppName { get; set; } = "";
    public TimeOnly StartTime { get; set; } = new(9, 0);   // target-typed new
    public TimeOnly EndTime { get; set; } = new(17, 0);    // target-typed new
    public bool Enabled { get; set; } = true;
}

// src/FocusBlock.Contracts/SecurityConfig.cs (archivo completo)
public class SecurityConfig
{
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
    public int CooldownMinutes { get; set; } = 10;
}
```

### Error común

Asumir que "config nueva" trae una regla activa. Falso: `AppConfig.BlockRules` es una
lista vacía; los defaults de horario viven en `BlockRuleConfig`, que todavía no existe.
Detección directa:
`new AppConfig().BlockRules.Count == 0` (verificable en `AppConfigTests`). Otra trampa:
no inicializar un objeto anidado y acceder a sus miembros → `NullReferenceException`.

### Para profundizar

- [`TimeOnly` (Microsoft Learn)](https://learn.microsoft.com/dotnet/api/system.timeonly)
- [Colecciones y estructuras de datos](https://learn.microsoft.com/dotnet/standard/collections/)
- Código: `src/FocusBlock.Contracts/AppConfig.cs`, `BlockRuleConfig.cs`, `SecurityConfig.cs`.
- Tests: `tests/FocusBlock.Tests.Unit/AppConfigTests.cs`.

---

## Serialización JSON (round-trip)

### En una frase

Serializar es convertir el objeto de config en texto JSON; deserializar es reconstruirlo.
Si el ida y vuelta devuelve los mismos datos, el round-trip es correcto.

### Fundamentos previos

**¿Qué es serializar y deserializar?**
Un objeto vive en memoria como estructura binaria (campos, punteros). Un archivo guarda
bytes en disco. Serializar es *traducir* el objeto a un formato guardable (texto JSON);
deserializar es *reconstruir* el objeto desde ese texto. La palabra viene de "serie":
poner los datos en secuencia.

**¿Qué es JSON?**
JavaScript Object Notation: un formato de texto con objetos (`{ "clave": valor }`),
listas (`[ ... ]`), strings, números, booleanos y `null`. Es independiente del lenguaje y
legible por humanos. Es el formato que fija `ADR-005`.

**¿Por qué un archivo de config debe ser legible?**
Porque el usuario (o un admin) puede querer editarlo a mano, revisarlo en un diff de git o
diagnosticar un problema. Un formato binario cumpliría la función de persistir, pero
ocultaría la información. JSON es texto plano: visible, editable y diffeable.

**¿Qué significa "round-trip"?**
Es el viaje completo: objeto → texto → objeto. Si el resultado tiene los mismos valores
que el original, el round-trip es fiel. Es la prueba más importante de un serializador:
detecta campos que se pierden, tipos que no se traducen bien o defaults que no se
respetan.

**¿Qué es un `JsonSerializerOptions`?**
Es el objeto que configura cómo serializa `System.Text.Json`. Por defecto produce JSON
compacto (todo en una línea). `WriteIndented = true` le pide saltos de línea y sangría:
JSON legible para humanos. Es una opción, no un flag global.

**¿Qué es el operador `??` (null-coalescing)?**
`a ?? b` devuelve `a` si `a` no es `null`; si es `null`, devuelve `b`. Se usa para dar un
valor de respaldo. En `Deserialize`, si el JSON es literalmente `"null"`,
`JsonSerializer.Deserialize` devuelve `null`; el `?? throw new JsonException(...)` convierte
ese caso silencioso en un error explícito. **Fallar temprano** es mejor que devolver un
objeto nulo que explotará más tarde y lejos, donde el diagnóstico es difícil.

**Contexto histórico de `TimeOnly`:** en .NET 6, `System.Text.Json` no sabía serializar
`TimeOnly`/`DateOnly`; había que escribir un converter a mano. Desde .NET 7 el soporte es
nativo. FocusBlock usa .NET 10, así que no hizo falta converter — eso verificó el ADR-005
con el round-trip test.

### Qué es

`ConfigSerializer` es una clase estática con dos métodos: `Serialize(AppConfig) → string` y
`Deserialize(string) → AppConfig`. Por dentro usa `System.Text.Json` con `WriteIndented`.

### Qué problema resuelve

Persistir la configuración sin escribir el parseo a mano (y sin inventar bugs de escape de
comillas, anidado o tipos). El serializer se encarga de traducir el grafo de objetos a
texto y de vuelta.

### Cómo funciona paso a paso

1. `Serialize` recibe un `AppConfig`, lo recorre con reflexión y emite el JSON. Con
   `WriteIndented = true`, agrega saltos e indentación.
2. `List<BlockRuleConfig>` se convierte en un array JSON; cada regla, en un objeto.
3. `TimeOnly` se escribe como string ISO-8601 (por ejemplo, `"09:30:00"`).
4. `Deserialize` lee el texto, crea un `AppConfig`, y va llenando propiedades según las
   claves del JSON.
5. Si el resultado es `null`, `??` lanza `JsonException`: no se propaga un `null`.

### Qué se rompería sin esto en FocusBlock

`AppConfigTests.AppConfig_SerializeDeserialize_RoundTrips` guarda una regla `firefox` con
`StartTime = 09:30` y `CooldownMinutes = 25`, y verifica que al deserializar se obtienen
los mismos valores. Sin serialización correcta, la config no sobreviviría entre
ejecuciones: cada arranque volvería a defaults y el usuario perdería sus reglas.

### Para qué sirve en este proyecto

La config viaja entre memoria y disco como JSON. `ConfigSerializer` vive en
`FocusBlock.Contracts` (decisión registrada en `progress-log`): la traducción del contrato
pertenece al contrato, y TUI y daemon la reutilizarán.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Contracts/ConfigSerializer.cs (archivo completo)
public static class ConfigSerializer
{
    public static string Serialize(AppConfig config) =>
        JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

    public static AppConfig Deserialize(string json) =>
        JsonSerializer.Deserialize<AppConfig>(json)
        ?? throw new JsonException("Config JSON is empty or invalid.");
}

// tests/FocusBlock.Tests.Unit/AppConfigTests.cs:31-37 (round-trip)
string json = ConfigSerializer.Serialize(original);
AppConfig result = ConfigSerializer.Deserialize(json);
result.BlockRules[0].StartTime.Should().Be(new TimeOnly(9, 30));
```

### Error común

Creer que `TimeOnly` necesita un converter propio. En .NET 10 es nativo (desde .NET 7) y no
hace falta. Detección: el round-trip test lo demuestra. Otra trampa: serializar con
`WriteIndented = false` (default) y luego pretender leer el archivo cómodamente; sale todo
en una línea. Y otra: ignorar el caso `"null"` en `Deserialize`, que devolvería un objeto
nulo camuflado de éxito.

### Para profundizar

- [`docs/adr/ADR-005-config-json.md`](../adr/ADR-005-config-json.md)
- [System.Text.Json — docs](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/)
- Código: `src/FocusBlock.Contracts/ConfigSerializer.cs`.

---

## Async I/O (async/await)

### En una frase

`async`/`await` permite esperar al disco sin dejar al hilo bloqueado esperando: el hilo se
libera para hacer otra cosa y el método se reanuda cuando el dato está listo.

### Fundamentos previos

**¿Qué es un hilo (thread)?**
Un hilo es la unidad de ejecución que el sistema operativo planifica en un núcleo de CPU.
Un programa .NET arranca con un hilo principal (el que corre `Main`). Si ese hilo se queda
esperando, no puede atender nada más: la ventana se congela.

**¿Qué significa "bloquear un hilo"?**
Es dejarlo detenido sin hacer trabajo. Ejemplo: `File.ReadAllText(path)` (versión síncrona)
bloquea al hilo hasta que el disco responda. Si eso pasa en el hilo de la UI, la TUI no
responde a las teclas: se siente "colgada". El bloqueo no es un error de lógica; es una
consecuencia de pedir el dato de forma síncrona.

**¿Por qué el disco es lento relativo a la CPU?**
Los accesos a disco se miden en milisegundos o más; los ciclos de CPU, en nanosegundos.
Un núcleo moderno podría ejecutar millones de instrucciones en el tiempo que tarda un
lector de disco. Por eso dejar un hilo girando en espera (o peor, bloqueado) es
desperdicio: hay trabajo disponible mientras el disco trabaja.

**¿Qué es un `Task<T>`?**
Es un objeto que representa una operación *en curso* y que promete un valor de tipo `T`
cuando termine. No es el valor: es el *recibo* de una operación futura. `Task<AppConfig>`
es la promesa de un `AppConfig`; `Task` (sin `<T>`) es una promesa sin valor de retorno.

**¿Qué hace `await` exactamente?**
`await tarea` hace dos cosas: (1) si la tarea ya terminó, extrae el valor y sigue; (2) si
no terminó, *suspende* el método actual, devuelve el control a quien lo llamó, y registra
una continuación para retomar cuando la tarea complete. El método no se bloquea: se
"pausa" y se reanuda.

**El misconception central: `async` NO crea un hilo.**
Para I/O (leer archivo, red, socket), no hay hilo esperando. El sistema operativo maneja la
operación en segundo plano y notifica al runtime cuando termina. `async` no lanza un
`Thread`; usa el mecanismo de *I/O completion* del SO. Crear un hilo por cada lectura sería
carísimo. (En cambio, `Task.Run` sí usa un hilo del pool para trabajo intensivo de CPU,
que es otro caso.)

**¿Por qué `async` es "contagioso hacia arriba"?**
Si un método es `async`, devuelve un `Task`. Un llamador que quiera el valor debe hacer
`await`, y para poder hacer `await` debe ser `async` él también. El "virus" sube por la
cadena de llamadas hasta el evento/bucle que puede permitirse no esperar. Por eso
`ConfigService.LoadAsync` es async, y quien lo llame tendrá que serlo también.

**¿Qué es un `CancellationToken`?**
Es una señal cooperativa de cancelación. No aborta el hilo por la fuerza (eso sería
peligroso); el código debe *observarla* y decidir cuándo parar. Quien cancela llama
`Cancel()` en un `CancellationTokenSource`; quien recibe el token chequea si se pidió
cancelación. "Cooperativa" significa que la operación debe colaborar.

**¿Por qué `.Result` o `.Wait()` puede deadlockear?**
Bloquean el hilo actual esperando la tarea. Si la continuación de esa tarea necesita el
mismo hilo (por ejemplo, el hilo de UI), quedan los dos esperándose mutuamente: deadlock.
La regla es no bloquear sobre async: usar `await`.

**`async void` vs `async Task`:** `async Task` permite al llamador esperar y capturar
excepciones. `async void` no se puede esperar y sus excepciones escapan sin control. Solo
se usa en handlers de eventos (donde la firma lo exige). En servicios, siempre `async Task`.

### Qué es

`ConfigService` expone `LoadAsync` y `SaveAsync`, que devuelven `Task<AppConfig>` y `Task`
respectivamente, y aceptan un `CancellationToken` opcional.

### Qué problema resuelve

Sin async, cada lectura/escritura de config congelaría la TUI. Con async, la interfaz sigue
respondiendo mientras el disco trabaja, y la operación se puede cancelar limpiamente.

### Cómo funciona paso a paso

1. `LoadAsync` verifica si el archivo existe. Si no, corta temprano devolviendo
   `new AppConfig()` (sin tocar disco).
2. Si existe, llama `await File.ReadAllTextAsync(_path, ct)`.
3. Si el archivo no está leído aún, el método se suspende y devuelve el control.
4. El SO lee el archivo; cuando termina, el runtime retoma `LoadAsync` en el punto del
   `await`.
5. `ReadAllTextAsync` devuelve el `string`; se deserializa y se retorna el `AppConfig`.
6. `SaveAsync` hace el camino inverso: serializa y `await File.WriteAllTextAsync(...)`.

### Qué se rompería sin esto en FocusBlock

`ConfigServiceTests` usa `await service.LoadAsync()` y `await service.SaveAsync(config)`. Si
los métodos fueran síncronos, el test seguiría andando, pero la TUI real se congelaría en
cada guardado. Y el `CancellationToken` no tendría dónde aplicarse: no se podría cancelar
un guardado lanzado al cerrar la app, por ejemplo.

### Para qué sirve en este proyecto

`ConfigService.LoadAsync`/`SaveAsync` son la única capa de I/O de la fase. Al usar
`File.*Async`, el hilo de la UI queda libre. `ct = default` permite llamarlos sin token
para el caso simple, y pasarlo cuando exista una operación cancelable.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Tui/Services/ConfigService.cs:14-29
public async Task<AppConfig> LoadAsync(CancellationToken ct = default)
{
    if (!File.Exists(_path))
    {
        return new AppConfig();                 // corta temprano, sin I/O
    }

    string json = await File.ReadAllTextAsync(_path, ct);
    return ConfigSerializer.Deserialize(json);
}

public async Task SaveAsync(AppConfig config, CancellationToken ct = default)
{
    string json = ConfigSerializer.Serialize(config);
    await File.WriteAllTextAsync(_path, json, ct);
}
```

### Error común

Olvidar que *async va hacia arriba*: un método async debe ser `await`-eado por su llamador,
y ese llamador debe ser async. Detección: el compilador avisa si un `Task` queda sin
`await` (warning `CS4014`). Otra trampa grave: usar `.Result` o `.Wait()` para "no hacer
async a mi llamador"; en UI eso puede deadlockear. Y confundir `async` con "crea un hilo":
no lo hace para I/O.

### Para profundizar

- [Patrones de programación asíncrona](https://learn.microsoft.com/dotnet/standard/asynchronous-programming-patterns/)
- [Task-based asynchronous pattern](https://learn.microsoft.com/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap)
- Código: `src/FocusBlock.Tui/Services/ConfigService.cs`.

---

## Hashing de contraseñas (Argon2id)

### En una frase

No se guarda la contraseña: se guarda una *huella* (hash) que no se puede revertir, y para
verificar se recalcula la huella y se compara.

### Fundamentos previos

**¿Qué es una función de hash?**
Es una función que toma un dato de cualquier tamaño y devuelve una huella de tamaño fijo
(por ejemplo, 32 bytes). Propiedades clave:
- *Determinista*: el mismo dato produce siempre la misma huella.
- *Un solo sentido*: desde la huella no se puede recuperar el dato original.
- *Efecto avalancha*: un cambio mínimo en la entrada cambia la huella por completo.

**¿Por qué NO es cifrado? (analogía huella vs caja fuerte)**
El cifrado es una caja fuerte: se mete el dato y se recupera con la llave. El hash es una
huella digital: identifica sin permitir reconstruir a la persona. Con la huella verificás
"¿es esta la misma persona?" sin poder "reconstruirla". Por eso no hay llave ni descifrado:
no existe forma de volver atrás.

**¿Por qué no guardar texto plano?**
Porque si roban el archivo (backup, git, acceso al disco), el atacante tiene la contraseña
directamente, y además suele reutilizarse en otros servicios. Con hash, robar el archivo no
entrega la contraseña.

**¿Por qué SHA-256/MD5 no alcanzan?**
Porque están diseñados para ser *rápidos*. Un atacante con hardware moderno puede calcular
miles de millones de hashes por segundo, así que probar contraseñas débiles es barato.
Además son de propósito general (integridad), no de protección de contraseñas.

**¿Qué es la fuerza bruta?**
Probar sistemáticamente todas las contraseñas posibles (o las más probables) hasta acertar.
Su costo depende de cuántos intentos por segundo permita el esquema.

**¿Qué es una rainbow table?**
Una tabla precalculada gigante que mapea hash → contraseña para millones de valores
comunes. Con ella, romper un hash es buscar en la tabla, inmediato. Es el equivalente a
tener un diccionario de huellas ya resueltas.

**¿Qué es una sal (salt) y por qué no es secreta?**
La sal son bytes aleatorios únicos por contraseña que se mezclan con ella antes de hashear.
Rompe las rainbow tables (el hash ya no depende solo de la contraseña) y hace que dos
usuarios con la misma contraseña tengan huellas distintas. No es secreta: se guarda junto
al hash. Su seguridad no depende de ocultarla, sino de su unicidad y aleatoriedad.

**¿Qué es Argon2id y qué significa memory-hard?**
Argon2id es un algoritmo de hashing de contraseñas (ganador de la Password Hashing
Competition, 2015). *Memory-hard* significa que cada intento debe reservar y recorrer una
cantidad grande de memoria RAM. Los atacantes suelen acelerar con GPUs: tienen muchos
núcleos pero poca memoria por núcleo. Si cada hash exige 16 MB, la GPU se queda sin memoria
y no puede paralelizar tanto. Esa es la defensa.

**¿Qué es un CSPRNG?**
Un generador de números aleatorios criptográficamente seguro. Cumple que su salida es
impredecible incluso conociendo parte de ella. `RandomNumberGenerator` (del SO) es un
CSPRNG. `System.Random` NO lo es: es un generador determinista; con la semilla correcta
(que suele ser el reloj) se pueden predecir los "aleatorios". Usarlo para una sal es
inaceptable.

**¿Qué es Base64 y por qué se usa para guardar bytes en JSON?**
JSON no puede contener bytes binarios arbitrarios (habría bytes de control, comillas, etc.).
Base64 codifica esos bytes usando solo 64 caracteres imprimibles (`A-Z`, `a-z`, `0-9`, `+`,
`/`) más `=` de relleno. Ocupa ~33% más, pero se guarda como texto seguro. Por eso `hash` y
`salt` se guardan como strings Base64.

**¿Qué es un timing attack y por qué `FixedTimeEquals`?**
Comparar con `==` recorre los bytes y **corta al primer byte distinto**. Eso hace que la
operación tarde más o menos según cuántos bytes coincidan al principio. Un atacante puede
medir el tiempo de miles de intentos y deducir la huella byte a byte. Es un *timing attack*.
`CryptographicOperations.FixedTimeEquals` recorre la misma cantidad de trabajo sin importar
dónde difieran, así el tiempo no filtra información.

**Recalcular vs descifrar:** para verificar una contraseña NO se descifra el hash (no se
puede). Se recalcula el hash usando la contraseña ingresada y la *misma sal guardada*, y se
compara. Si coinciden, la contraseña es correcta. Por eso la sal se persiste: sin ella no
se puede repetir el cálculo.

### Qué es

`AuthService` tiene `HashPassword` (genera hash + sal) y `VerifyPassword` (recalcula y
compara). Usa `Konscious.Security.Cryptography.Argon2` con `Argon2id`.

### Qué problema resuelve

Nunca guardar la contraseña en texto plano. Aun robando el archivo de config, el atacante
obtiene una huella + sal que no puede revertir eficientemente. Es la base del anti-bypass
de las Fases 4/5.

### Cómo funciona paso a paso

1. `HashPassword(password)` genera 16 bytes de sal con `RandomNumberGenerator.GetBytes(16)`.
2. Llama `ComputeArgon2id`: crea un `Argon2id` con la contraseña en UTF-8, la sal y los
   parámetros (16 MB, 3 iteraciones, 4 grados de paralelismo).
3. `argon2.GetBytes(32)` devuelve 32 bytes de hash.
4. Convierte ambos a Base64 y devuelve la tupla `(Hash, Salt)`. El llamador los guarda en
   `SecurityConfig`.
5. `VerifyPassword(password, hash, salt)` decodifica el hash y la sal desde Base64.
6. Recalcula el hash con la misma sal y parámetros.
7. Compara con `FixedTimeEquals` (tiempo constante) y devuelve `true`/`false`.

### Qué se rompería sin esto en FocusBlock

`AuthServiceTests` verifica: (a) que el hash y la sal no estén vacíos y que el hash no sea
igual a la contraseña; (b) que la verificación correcta devuelva `true`; (c) que la
incorrecta devuelva `false`. Sin hashing, `SecurityConfig.PasswordHash` guardaría texto
plano y el anti-bypass sería trivial de eludir leyendo el archivo.

### Para qué sirve en este proyecto

Es el motor del anti-bypass: `HashPassword` al configurar la contraseña (Fase 2);
`VerifyPassword` al desbloquear temprano (Fases 4/5). La tupla `(hash, salt)` se persiste
en `SecurityConfig`.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Tui/Services/AuthService.cs (extracto)
private const int SaltSize = 16;
private const int HashSize = 32;

public (string Hash, string Salt) HashPassword(string password)
{
    byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);   // CSPRNG
    byte[] hash = ComputeArgon2id(password, salt);
    return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
}

public bool VerifyPassword(string password, string hash, string salt)
{
    byte[] expected = Convert.FromBase64String(hash);
    byte[] saltBytes = Convert.FromBase64String(salt);
    byte[] computed = ComputeArgon2id(password, saltBytes);   // recalcular, no descifrar
    return CryptographicOperations.FixedTimeEquals(computed, expected);
}

private static byte[] ComputeArgon2id(string password, byte[] salt)
{
    using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
    {
        Salt = salt,
        DegreeOfParallelism = 4,
        MemorySize = 16 * 1024,   // 16 MB
        Iterations = 3,
    };
    return argon2.GetBytes(HashSize);
}
```

### Error común

Comparar con `==` (vulnerable a timing attacks) o usar `System.Random` para la sal
(predecible, no criptográfico). Detección: buscar `==` en la verificación de hashes y
`new Random()` en cualquier cosa relacionada con seguridad. Otra trampa: tratar la sal como
secreta y no guardarla; sin ella no se puede verificar.

### Para profundizar

- [`docs/adr/ADR-004-argon2id.md`](../adr/ADR-004-argon2id.md)
- [Argon2 (RFC 9106)](https://datatracker.ietf.org/doc/html/rfc9106)
- [`RandomNumberGenerator` (Microsoft Learn)](https://learn.microsoft.com/dotnet/api/system.security.cryptography.randomnumbergenerator)
- [`CryptographicOperations.FixedTimeEquals`](https://learn.microsoft.com/dotnet/api/system.security.cryptography.cryptographicoperations.fixedtimeequals)
- Código: `src/FocusBlock.Tui/Services/AuthService.cs`.

---

## Seguridad criptográfica: memoria ajustable y tiempo constante

### En una frase

El costo del hash es una muralla que se puede ensanchar con el tiempo, y la comparación se
hace en tiempo constante para no filtrar información.

### Fundamentos previos

Este concepto profundiza dos ideas del anterior: **memory-hard** (cada intento cuesta
RAM) y **tiempo constante** (comparar sin filtrar cuántos bytes coinciden).

**Costo ajustable, en una analogía:** la muralla de un castillo se construye según las
armas de la época. Cuando aparecen escaleras más largas, se sube la muralla. En hashing,
las "armas" son el hardware del atacante: cuando las GPUs se vuelven más rápidas, se suben
`MemorySize` e `Iterations` para que atacar siga siendo caro. Los parámetros no son una
constante mágica: son una decisión de diseño revisable.

**Tiempo constante, precisión:** `FixedTimeEquals` recorre los bytes sin cortar ante la
primera diferencia. En este caso ambos arreglos miden 32 bytes, así que la duración no
depende del contenido.

**La regla de oro: no inventes criptografía propia.**
Usá primitivas y librerías auditadas (Argon2id de Konscious, `RandomNumberGenerator`,
`FixedTimeEquals`). Una implementación casera de un algoritmo de seguridad casi siempre
tiene fallas sutiles (fuga de tiempo, aleatoriedad débil, manejo de errores). En seguridad,
"parece que funciona" no es suficiente.

### Qué es

Los parámetros de Argon2id (`MemorySize`, `Iterations`, `DegreeOfParallelism`) definen el
costo; la verificación usa comparación en tiempo constante.

### Qué problema resuelve

- **Costo alto** (16 MB, 3 iteraciones): encarece la fuerza bruta; la GPU queda limitada
  por memoria y ancho de banda, no por núcleos.
- **Tiempo constante**: el atacante no deduce cuántos bytes acertó midiendo la duración.

### Cómo funciona paso a paso

1. `MemorySize = 16 * 1024` significa 16.384 KiB = 16 MB de RAM reservados y recorridos por
   cada hash (la constante está en KiB).
2. `Iterations = 3` repite el recorrido tres veces.
3. `DegreeOfParallelism = 4` indica que el algoritmo puede usar hasta 4 carriles paralelos
   (intrínseco al cálculo, no hilos que crea el modelo async).
4. Al verificar, `FixedTimeEquals` compara los 32 bytes completos.
5. Los parámetros son **constantes del código**, no se guardan junto al hash. Consecuencia
   importante: si mañana se suben (`MemorySize`, `Iterations`), las huellas viejas —calculadas
   con los parámetros viejos— **dejan de verificar**, porque `VerifyPassword` recalcula con
   los parámetros NUEVOS. Subir el costo exige re-hashear la contraseña (un cambio o reset).
6. La forma profesional de que el costo evolucione sin romper nada es **guardar los
   parámetros junto al hash** (o un formato que los incluya, como el string PHC:
   `$argon2id$v=19$m=16384,t=3,p=4$<sal>$<hash>`). Así cada huella vieja se verifica con
   SUS parámetros y se puede migrar a los nuevos en el próximo login. Hoy FocusBlock no lo
   hace: es una mejora pendiente, no un bug.

### Qué se rompería sin esto en FocusBlock

Con parámetros bajos, un atacante con GPU probaría muchísimas contraseñas por segundo. Con
`==` en vez de `FixedTimeEquals`, mediría tiempos y deduciría la huella. Ninguno de los
dos fallos rompe un test funcional — por eso importan: son fallos que el test no ve y solo
la revisión de seguridad detecta.

### Para qué sirve en este proyecto

El costo es **ajustable**: si el hardware futuro acelera, se suben los parámetros. Es una
decisión documentada en `ADR-004`, que lista alternativas (bcrypt con límites de memoria,
PBKDF2 no memory-hard) y su consecuencia principal: resistente a ataques con GPU/ASIC.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Tui/Services/AuthService.cs:30-37
using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
{
    Salt = salt,
    DegreeOfParallelism = 4,
    MemorySize = 16 * 1024,   // 16 MB: cada intento consume RAM real
    Iterations = 3,
};
return argon2.GetBytes(HashSize);

// src/FocusBlock.Tui/Services/AuthService.cs:25
return CryptographicOperations.FixedTimeEquals(computed, expected);   // tiempo constante
```

### Error común

Usar `Random` en vez de `RandomNumberGenerator` para la sal: `System.Random` es predecible
y no criptográfico. Detección: buscar `new Random(` en código de seguridad. Otra: comparar
hashes con `==`. Y la más peligrosa: implementar criptografía propia. Regla: usá primitivas
auditadas; no diseñes tu propio esquema.

### Para profundizar

- [`docs/adr/ADR-004-argon2id.md`](../adr/ADR-004-argon2id.md)
- [Password Hashing Competition](https://www.password-hashing.net/)
- [Konscious.Security.Cryptography](https://github.com/kmaragon/Konscious.Security.Cryptography)
- Código: `src/FocusBlock.Tui/Services/AuthService.cs`.

---

## Relación entre estos conceptos

Cada capa tiene un solo trabajo (SRP aplicado a la configuración):

| Capa | Clase(s) | Responsabilidad única |
|---|---|---|
| Modelo | `AppConfig`, `BlockRuleConfig`, `SecurityConfig` | Definir la forma de la config. |
| Traducción | `ConfigSerializer` | Convertir objeto ↔ JSON (round-trip). |
| Transporte | `ConfigService` | Leer/escribir el archivo sin bloquear (async I/O). |
| Protección | `AuthService` | Hashear y verificar la contraseña (Argon2id). |

El flujo es: `ConfigService` no encuentra archivo → devuelve `new AppConfig()` (modelo con
defaults) → cuando hay archivo, `ConfigSerializer` lo deserializa → `SaveAsync` vuelve a
serializar y escribe. En paralelo, `AuthService` produce la huella y la sal que
`SecurityConfig` persiste. Ninguna capa conoce los detalles internos de otra: el serializer
no sabe de disco, el service no sabe de criptografía, el modelo no sabe de JSON. Esa
separación es lo que hace cada pieza testeable por separado y lo que permite reutilizar el
contrato entre TUI y daemon.

La seguridad y el I/O async se cruzan en un punto: ambos existen para un contexto donde el
hilo de la UI no debe bloquearse y donde el archivo puede caer en manos hostiles. Un
`ConfigService` síncrono congelaría la TUI; un `AuthService` con `System.Random` o `==`
entregaría la contraseña. Son fallos que el código funcional no muestra, pero que definen
si el anti-bypass sirve o es decorativo.

---

## Convención

- **Archivo**: `docs/learning/phase-NN-name.md`, uno por fase.
- **Título**: `# Fase NN — Tema`.
- **Código**: bloques `cs`; líneas de ~100 caracteres.
- **Secciones por concepto**: ver `docs/learning/template-phase.md`.
