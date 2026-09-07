# Fase 02 — Configuración: Modelos, JSON, I/O y Seguridad

> Conceptos de la fase en la que la app aprendió a definir, guardar, cargar y proteger su configuración.
> Log de la fase (tareas, decisiones, problemas) → `docs/progress-log/phase-02-config.md`

---

## Modelos de configuración y valores por defecto

### Qué es

Clases de datos (`AppConfig`, `BlockRuleConfig`, `SecurityConfig`) en el proyecto `FocusBlock.Contracts`, con **valores por defecto** asignados en los inicializadores de propiedades.

### Qué problema resuelve

Una configuración sin valores por defecto sería "vacía e inválida". Con defaults, un `new AppConfig()` ya es una config **sensible** (horario 09:00-17:00, cooldown 10 min, regla activa).

### Para qué sirve en este proyecto

`ConfigService` devuelve `new AppConfig()` cuando no hay archivo → la app arranca bien la primera vez, heredando los defaults del modelo.

### Cómo se usa

```cs
public class BlockRuleConfig
{
    public string AppName { get; set; } = "";
    public TimeOnly StartTime { get; set; } = new(9, 0);
    public bool Enabled { get; set; } = true;
}
```

### Error común

Asumir que una config nueva es "vacía". Con defaults, es *válida por construcción*.

### Referencias

- `src/FocusBlock.Contracts/AppConfig.cs` · `BlockRuleConfig.cs` · `SecurityConfig.cs`

---

## Serialización JSON (round-trip)

### Qué es

`ConfigSerializer` convierte objetos ↔ texto JSON con `System.Text.Json` (`Serialize` / `Deserialize`). *Round-trip* = guardar, leer y obtener el mismo objeto.

### Qué problema resuelve

Persistir la configuración en un archivo legible y versionable, sin escribir serialización a mano.

### Para qué sirve en este proyecto

La config viaja entre memoria y disco como JSON; `WriteIndented = true` la deja legible para humanos.

### Cómo se usa

```cs
string json = ConfigSerializer.Serialize(config);
AppConfig config = ConfigSerializer.Deserialize(json);
```

### Error común

Creer que `TimeOnly` necesita converter. En .NET 10, `System.Text.Json` lo serializa **nativo** (desde .NET 7) — no hizo falta.

### Referencias

- `src/FocusBlock.Contracts/ConfigSerializer.cs`

---

## Async I/O (async/await)

### Qué es

`async`/`await` permite esperar operaciones lentas (leer/escribir disco) **sin bloquear el hilo**. El método devuelve un `Task` y se reanuda cuando la operación termina.

### Qué problema resuelve

Sin async, cada lectura de disco congelaría la TUI. Con async, la interfaz sigue respondiendo.

### Para qué sirve en este proyecto

`ConfigService.LoadAsync` / `SaveAsync` leen y escriben el archivo de config de forma no bloqueante, con `CancellationToken` para abortar limpiamente.

### Cómo se usa

```cs
string json = await File.ReadAllTextAsync(_path, ct);
await File.WriteAllTextAsync(_path, json, ct);
```

### Error común

Olvidar que *async va hacia arriba*: un método async debe ser `await`-eado por su llamador.

### Referencias

- `src/FocusBlock.Tui/Services/ConfigService.cs`

---

## Hashing de contraseñas (Argon2id)

### Qué es

`AuthService` guarda la contraseña como una **huella** (hash) + **sal** (bytes aleatorios), calculada con Argon2id — un hash *memory-hard* deliberadamente caro.

### Qué problema resuelve

Nunca guardar la contraseña en texto plano: si roban el archivo de config, no pueden recuperarla.

### Para qué sirve en este proyecto

Es el motor del **anti-bypass** (Fases 4/5): `HashPassword` al configurar, `VerifyPassword` al desbloquear temprano.

### Cómo se usa

```cs
var (hash, salt) = auth.HashPassword("secreto");       // guardar en SecurityConfig
bool ok = auth.VerifyPassword("secreto", hash, salt);  // recalcula con la misma sal
```

### Error común

Comparar con `==` (vulnerable a timing attacks). Usar `CryptographicOperations.FixedTimeEquals` (tiempo constante).

### Referencias

- `src/FocusBlock.Tui/Services/AuthService.cs` · `docs/adr/ADR-004-argon2id.md`

---

## Seguridad criptográfica: memoria ajustable y tiempo constante

### Qué es

Los parámetros de Argon2id (`MemorySize`, `Iterations`, `DegreeOfParallelism`) definen el **costo** del hash; la comparación usa **tiempo constante**.

### Qué problema resuelve

- Costo alto (16 MB, 3 iteraciones) → fuerza bruta inviable (GPU limitada por memoria/ancho de banda).
- Tiempo constante → el atacante no deduce cuántos bytes acertó midiendo la duración.

### Para qué sirve en este proyecto

El costo es **ajustable**: si el hardware futuro acelera, se suben los parámetros.

### Cómo se usa

```cs
MemorySize = 16 * 1024,   // 16 MB
Iterations = 3,
DegreeOfParallelism = 4,
```

### Error común

Usar `Random` en vez de `RandomNumberGenerator` para la sal — el primero es predecible, no criptográfico.

### Referencias

- `src/FocusBlock.Tui/Services/AuthService.cs`

---

## Relación entre estos conceptos

Los modelos definen la FORMA de la config (con defaults). `ConfigSerializer` traduce objeto ↔ JSON. `ConfigService` hace el I/O async (y decide defaults si no hay archivo). `AuthService` protege la contraseña con Argon2id + sal, listo para el anti-bypass de las Fases 4/5. Cada capa tiene un solo trabajo — Single Responsibility aplicada a la configuración.