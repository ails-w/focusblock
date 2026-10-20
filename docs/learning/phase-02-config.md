# Fase 02 — Configuración: modelos, JSON y contraseña

> Qué construye: la configuración persistente (reglas y seguridad) y el hashing de la
> contraseña que después usa el anti-bypass.
> Antes: [Fase 01](phase-01-tui.md) · Después: [Fase 03](phase-03-daemon.md) ·
> [Glosario](glossary.md)

## Dónde estamos

La TUI navega, pero no recuerda nada: no hay reglas ni contraseña. Falta definir la forma de
los datos, guardarlos en disco y proteger el secreto.

```text
AppConfig ──▶ ConfigSerializer ──▶ JSON ──▶ archivo
   │                                        │
   └── BlockRules / Security      ConfigService (async)
                                        │
                              AuthService (Argon2id + sal)
```

## Qué construye esta fase

Tres modelos compartidos (`AppConfig`, `BlockRuleConfig`, `SecurityConfig`), un serializer
JSON con round-trip verificado y un servicio de I/O asíncrono. Además, `AuthService` hashea
y verifica la contraseña. Trece tests cubren el resultado.

## El recorrido de una acción

1. La app arranca y `ConfigService.LoadAsync` no encuentra el archivo.
2. Devuelve un `AppConfig` nuevo, con defaults válidos.
3. El usuario configura una contraseña.
4. `AuthService.HashPassword` genera una sal y una huella Argon2id.
5. `SaveAsync` serializa la config y la escribe en disco.
6. Al reiniciar, `LoadAsync` lee el JSON y `VerifyPassword` compara la huella.

## Los conceptos

| # | Concepto | Idea en una línea | Código |
|---|---|---|---|
| 1 | Modelo y defaults | La config recién creada ya es válida | `AppConfig.cs` |
| 2 | JSON round-trip | Guardar y releer debe devolver lo mismo | `ConfigSerializer.cs` |
| 3 | Async I/O | Esperar al disco sin congelar la interfaz | `ConfigService.cs` |
| 4 | Hashing seguro | Se guarda una huella cara de calcular y se compara sin filtrar | `AuthService.cs` |

---

## 1. Modelo y defaults

**Idea.** Un modelo de configuración solo describe qué campos tiene. Los valores por defecto
hacen que un objeto recién creado sea usable sin completar nada. Si no hay archivo, la app
arranca igual.

**Analogía.** Es un formulario prellenado: si lo entregas sin escribir, igual es válido. No
tienes que validar cada casilla antes de empezar.

**Cómo funciona.** 1) `AppConfig` declara `BlockRules = []` y `Security = new()`. 2)
`BlockRuleConfig` aporta sus defaults: 09:00, 17:00, activa. 3) `SecurityConfig` arranca con
cooldown de 10 minutos. 4) C# aplica los inicializadores en cada `new`.

```cs
// src/FocusBlock.Contracts/AppConfig.cs
public class AppConfig
{
    public List<BlockRuleConfig> BlockRules { get; set; } = [];   // vacía
    public SecurityConfig Security { get; set; } = new();         // nunca null
}

// src/FocusBlock.Contracts/BlockRuleConfig.cs
public TimeOnly StartTime { get; set; } = new(9, 0);
public TimeOnly EndTime { get; set; } = new(17, 0);
public bool Enabled { get; set; } = true;
```

**Trampa.** Creer que una config nueva trae una regla activa. Trae la lista **vacía**: los
horarios son defaults de `BlockRuleConfig`, que todavía no existe. La app no bloquea nada.

<details><summary>Verifica</summary>
1. ¿Por qué `Security = new()` evita un `NullReferenceException`?
2. ¿Qué diferencia hay entre un default de `AppConfig` y uno de `BlockRuleConfig`?
3. ¿Qué tipos usas para una hora del día, un instante y una duración?
</details>

---

## 2. JSON round-trip

**Idea.** Serializar convierte el objeto en texto; deserializar lo reconstruye. Si el ida y
vuelta conserva los datos, el round-trip es correcto.

**Analogía.** Es empacar una mudanza y desempacar: si al llegar falta un plato o se rompió
una copa, el embalaje falló. El test de round-trip es abrir la caja y comparar.

**Cómo funciona.** 1) `Serialize` recorre el objeto y emite JSON. 2) `WriteIndented` agrega
sangría para que sea legible. 3) `TimeOnly` se escribe como hora ISO. 4) `Deserialize`
reconstruye; si el texto es `"null"`, lanza `JsonException`.

```cs
// src/FocusBlock.Contracts/ConfigSerializer.cs
private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

public static string Serialize(AppConfig config) => JsonSerializer.Serialize(config, Options);

public static AppConfig Deserialize(string json) =>
    JsonSerializer.Deserialize<AppConfig>(json)
    ?? throw new JsonException("Config JSON is empty or invalid.");
```

**Trampa.** Creer que `TimeOnly` necesita un converter propio: en .NET 10 es nativo desde
.NET 7. Otra: ignorar el caso `"null"` y devolver un objeto nulo camuflado de éxito.

<details><summary>Verifica</summary>
1. ¿Qué prueba exactamente un test de round-trip?
2. ¿Por qué `TimeOnly` no necesita un converter en .NET 10?
3. ¿Qué gana `Deserialize` al lanzar en vez de devolver `null`?
</details>

---

## 3. Async I/O

**Idea.** `async`/`await` espera al disco sin dejar el hilo bloqueado: el hilo se libera y el
método se reanuda cuando el dato está listo.

**Analogía.** Es pedir comida a domicilio y seguir trabajando mientras llega. Bloquear el
hilo es quedarte parado en la puerta hasta que el repartidor aparezca.

**Cómo funciona.** 1) `LoadAsync` comprueba si el archivo existe; si no, corta sin tocar
disco. 2) `await File.ReadAllTextAsync(...)` suspende el método. 3) El sistema operativo lee
y el runtime retoma en el `await`. 4) `SaveAsync` hace el camino inverso con un token.

```cs
// src/FocusBlock.Core/ConfigService.cs
public async Task<AppConfig> LoadAsync(CancellationToken ct = default)
{
    if (!File.Exists(_path))
    {
        return new AppConfig();                 // corta temprano
    }

    string json = await File.ReadAllTextAsync(_path, ct);
    return ConfigSerializer.Deserialize(json);
}
```

**Trampa.** Usar `.Result` o `.Wait()` para evitar hacer async al llamador: en UI puede
provocar un deadlock. Y `async` no crea un hilo para I/O.

<details><summary>Verifica</summary>
1. ¿Qué hace `await` si la tarea ya terminó y qué hace si no?
2. ¿Por qué `.Result` puede bloquear la UI?
3. ¿Qué significa que async sea "contagioso hacia arriba"?
</details>

---

## 4. Hashing seguro

**Idea.** No se guarda la contraseña: se guarda una huella de un solo sentido. Para verificar
se recalcula con la misma sal y se compara en tiempo constante. El costo se ajusta con RAM
e iteraciones.

**Analogía.** El hash es una huella digital: identifica sin reconstruir a la persona. La sal
es un sello único por contraseña. El costo es la muralla del castillo: se sube cuando las
GPUs mejoran.

**Cómo funciona.** 1) Se generan 16 bytes de sal con un CSPRNG. 2) Argon2id calcula 32 bytes
con 16 MB, 3 iteraciones y 4 carriles. 3) Hash y sal se guardan en Base64. 4) Al verificar,
se recalcula con la misma sal y se compara con `FixedTimeEquals`.

```cs
// src/FocusBlock.Core/AuthService.cs (extracto)
byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
byte[] hash = ComputeArgon2id(password, salt);
return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));

// verificación: recalcular, nunca descifrar
byte[] computed = ComputeArgon2id(password, saltBytes);
return CryptographicOperations.FixedTimeEquals(computed, expected);

// costo por intento
using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
{
    Salt = salt,
    DegreeOfParallelism = 4,
    MemorySize = 16 * 1024,   // 16 MB
    Iterations = 3,
};
```

**Trampa.** Usar `System.Random` para la sal (predecible) o comparar con `==` (filtra cuántos
bytes coinciden). Y subir `MemorySize` invalida las huellas viejas: los parámetros no se
guardan con el hash; el formato PHC está pendiente.

<details><summary>Verifica</summary>
1. ¿Por qué no se puede "descifrar" un hash?
2. ¿Para qué sirve la sal y por qué se guarda?
3. ¿Por qué memory-hard frena a una GPU y qué pasa si subes el costo?
</details>

---

## Práctica

### Mezcla

1. `new AppConfig()`: ¿cuántas reglas activas tiene y por qué?
2. Si `LoadAsync` fuera síncrono, ¿qué se congela y en qué hilo?
3. Cambias `Iterations` de 3 a 4: ¿qué pasa con la contraseña ya guardada?
4. ¿Por qué el serializer vive en `Contracts` y no en la TUI?

<details><summary>Respuestas</summary>
1. Cero: la lista arranca vacía; los horarios son defaults de cada regla.
2. El hilo de la UI durante la lectura: la TUI deja de responder.
3. La verificación recalcula con 4 y no coincide; hay que re-hashear.
4. El contrato es compartido por TUI y daemon; la traducción pertenece al contrato.
</details>

### En código

1. Serializa con `WriteIndented = false` y explica qué cambia y qué no.
2. Agrega un campo a `BlockRuleConfig` y demuestra con un test que el round-trip lo conserva.
3. Escribe el test RED de `VerifyPassword` con una contraseña incorrecta.

### Antes de cerrar

- [ ] Puedo explicar por qué la config nueva no bloquea nada.
- [ ] Sé qué hace cada capa: modelo, serializer, service, auth.
- [ ] Puedo explicar por qué se recalcula el hash en vez de descifrarlo.

## Trampas reales de la fase

- **Miedo al converter de `TimeOnly`**: serializa nativo desde .NET 7; lo probó el round-trip.
- **Objeto anidado sin inicializar**: `Security` debe nacer con `new()` para evitar NRE.
- **Parámetros de Argon2id no persistidos**: subir el costo invalida las huellas existentes.

## Para profundizar

- [`ADR-005`](../adr/ADR-005-config-json.md) — JSON para la configuración.
- [`ADR-004`](../adr/ADR-004-argon2id.md) — Argon2id y sus parámetros.
- Código: `src/FocusBlock.Contracts/` y `src/FocusBlock.Core/`.
