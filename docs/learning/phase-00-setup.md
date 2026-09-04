# Fase 00 — Setup: Fundamentos del Proyecto

> Conceptos de la fase de arranque: Terminal.Gui v2, .NET 10 y el entorno de pruebas.
> Log de la fase (tareas, decisiones, problemas) → `docs/progress-log/phase-00-setup.md`

---

## Terminal.Gui v2 — API por instancia

### Qué es

Terminal.Gui es un framework TUI para .NET. En v2 el modelo cambió: ya no hay objeto estático global; se crea la aplicación con `Application.Create()` que devuelve `IApplication`.

### Qué problema resuelve

La API estática legacy de v1 no compila contra v2 y no permite manejar correctamente el ciclo de vida. El modelo por instancia permite crear, correr y detener ventanas de forma controlada.

### Para qué sirve en este proyecto

FocusBlock usa `Application.Create()` → `IApplication` y `app.Run(window)` para lanzar la ventana principal. El *alternate screen buffer* toma control de la terminal mientras corre.

### Cómo se usa

```csharp
var app = Application.Create();
app.Run(window); // bloquea hasta que la ventana se cierra (Esc / RequestStop())
```

Los tipos viven en sub-namespaces: `Terminal.Gui.App`, `Terminal.Gui.Views`, `Terminal.Gui.ViewBase`. El texto de los controles se asigna por propiedad (`Text`), no por constructor.

### Error común

Escribir código de v1 (objeto estático, `Label(texto)` en constructor) → errores CS0246/CS0117 porque en v2.4.17 los tipos cambiaron de namespace y firma.

### Referencias

- `docs/progress-log/phase-00-setup.md` — log de la fase
- [Terminal.Gui docs](https://github.com/gui-cs/Terminal.Gui)

---

## Formato `.slnx` en .NET 10

### Qué es

`dotnet new sln` en .NET 10 genera una solución en formato XML `.slnx`, en lugar del `.sln` clásico de texto.

### Qué problema resuelve

Un formato de solución más simple, versionable y fácil de leer/editar.

### Para qué sirve en este proyecto

FocusBlock.slnx es la solución raíz que agrupa `src/` y `tests/`.

### Cómo se usa

```bash
dotnet new sln -n FocusBlock        # genera FocusBlock.slnx
dotnet sln add src/FocusBlock.Tui   # agrega proyectos
```

### Error común

Buscar el archivo `.sln` clásico o asumir que no existe solución. El SDK 10 usa `.slnx` por defecto.

### Referencias

- `FocusBlock.slnx` — solución del proyecto

---

## Driver DOTNET de Terminal.Gui en Linux

### Qué es

Terminal.Gui tiene drivers de rendering. El driver ANSI (por defecto en Linux) tiene un bug que deja la ventana sin subviews (issues upstream #4848 y #4374).

### Qué problema resuelve

Forzar el driver `DOTNET` con `app.Init(DriverRegistry.Names.DOTNET)` renderiza la ventana correctamente en Linux.

### Para qué sirve en este proyecto

Workaround activo: sin él, el Hello World de FocusBlock muestra una ventana vacía aunque el código sea idéntico al ejemplo oficial.

### Cómo se usa

```csharp
app.Init(DriverRegistry.Names.DOTNET);
```

### Error común

Copiar el ejemplo oficial tal cual y ver la ventana vacía; pensar que el problema es el layout cuando es el driver. Revisar cuando el driver ANSI se estabilice.

### Referencias

- `docs/progress-log/phase-00-setup.md` — decisión 2
- [Terminal.Gui issues #4848, #4374](https://github.com/gui-cs/Terminal.Gui/issues)

---

## Case-sensitivity en C# (propiedades PascalCase)

### Qué es

C# es case-sensitive: `Y` y `y` son identificadores distintos. Las propiedades de librerías siguen PascalCase.

### Qué problema resuelve

Evita errores de compilación tontos (CS0103) cuando una propiedad no existe con esa grafía.

### Para qué sirve en este proyecto

En Terminal.Gui, la propiedad de posición es `Y` (mayúscula); escribir `y` falla.

### Cómo se usa

```csharp
label.Y = 1;   // correcto
label.y = 1;   // CS0103: 'y' no existe
```

### Error común

Asumir que el autocompletado o la memoria del lenguaje corrigen la grafía; en C# el caso importa siempre, incluso en propiedades de librerías.

### Referencias

- `docs/progress-log/phase-00-setup.md` — problema 3

---

## Relación entre estos conceptos

Terminal.Gui v2 define CÓMO se construye la UI (API por instancia, sub-namespaces, driver). El driver DOTNET es el workaround que hace funcionar esa API en Linux. El `.slnx` es el contenedor del proyecto. La case-sensitivity es la regla del lenguaje que aplica a todas las librerías, incluida Terminal.Gui. Juntos explican por qué el scaffolding de FocusBlock se ve así y qué tener en cuenta al escribir TUI.