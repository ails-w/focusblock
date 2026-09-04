# Fase 01 — Esqueleto TUI: Composición, Layout y Navegación

> Conceptos de la fase en la que la TUI pasó de Hello World a un esqueleto navegable: orquestación, ventana principal, navegación entre vistas y testabilidad.
> Log de la fase (tareas, decisiones, problemas) → `docs/progress-log/phase-01-tui.md`

---

## Terminal.Gui v2 — API por instancia y composición

### Qué es

Terminal.Gui v2 usa un modelo por instancia: `Application.Create()` devuelve `IApplication`, y las vistas se **componen** agregándolas con `Add(...)`. `MainWindow : Window` y las vistas de contenido `: View`.

### Qué problema resuelve

La API estática de v1 no compila contra v2 y no permite manejar el ciclo de vida de forma testeable. La composición por instancia permite armar ventanas sin un estado global.

### Para qué sirve en este proyecto

`FocusBlockApp` recibe `IApplication` y construye `MainWindow`; la ventana agrega `MenuBar`, `StatusBar` y el contenido con `Add(MenuBar, Content, StatusBar)`.

### Cómo se usa

```csharp
IApplication app = Application.Create();
Window window = new() { Title = "FocusBlock" };
window.Add(menuBar, content, statusBar);   // orden importa: el último se dibuja encima
app.Run(window);
```

### Error común

Usar la API de v1 (estático, `Label("texto")` en constructor) → CS0246/CS0117. En v2 el texto va por propiedad y los tipos viven en sub-namespaces (`App`, `Views`, `ViewBase`).

### Referencias

- `docs/learning/phase-00-setup.md` · `src/FocusBlock.Tui/App.cs`

---

## Layout declarativo: `Pos` y `Dim`

### Qué es

La posición y tamaño de una vista se declaran con `Pos` (X, Y) y `Dim` (Width, Height), relativos al contenedor o a otras vistas.

### Qué problema resuelve

Evita coordenadas "a ojo": el layout se adapta cuando cambia el contenedor o los vecinos.

### Para qué sirve en este proyecto

`Content.Y = 1` (debajo del menú), `Height = Dim.Fill(1)` (llena menos 1 fila para la StatusBar), y en `AddBlockView`: `X = Pos.Right(AppNameLabel) + 1` (campo a la derecha de la etiqueta).

### Cómo se usa

```csharp
field.X = Pos.Right(label) + 1;   // relativo: 1 espacio a la derecha del label
field.Y = 1;                      // absoluto: fila 1
field.Width = 20;                 // fijo (int → Dim implícito)
content.Height = Dim.Fill(1);     // llenar menos 1 (deja fila para la StatusBar)
```

### Error común

Asumir que sin `Width`/`Height` una vista se ve. En v2 el layout se calcula desde estas propiedades; el orden de `Add` define el z-order (quién se dibuja encima).

### Referencias

- `src/FocusBlock.Tui/Views/MainWindow.cs` · `Views/AddBlockView.cs`

---

## Navegación entre vistas

### Qué es

Un mecanismo para cambiar qué vista se muestra en el área de contenido: `ShowView(View)` hace `Remove(Content)` + `Content = view` + `Add(view)`.

### Qué problema resuelve

Permite que el menú "navegue" entre pantallas sin recrear la ventana: una vista a la vez en el `Content`.

### Para qué sirve en este proyecto

Los ítems del menú (Block → List, Block → New Block, View → Status) llaman a `ShowView(...)` con la vista destino.

### Cómo se usa

```csharp
new MenuItem { Title = "_List", Action = () => ShowView(BlockListView) }
```

### Error común

No proteger la idempotencia: si ya se muestra la misma vista, `Remove` + `Add` del mismo objeto causa parpadeo. El guard `if (view == Content) return;` lo evita.

### Referencias

- `src/FocusBlock.Tui/Views/MainWindow.cs`

---

## Dependency Injection + Composition Root

### Qué es

**DI**: una clase recibe sus dependencias por constructor en lugar de crearlas. **Composition Root**: el ensamblaje del grafo ocurre en un único punto de entrada.

### Qué problema resuelve

El acoplamiento. Si `FocusBlockApp` creara su `IApplication` internamente, no se podría testear sin una terminal real.

### Para qué sirve en este proyecto

`FocusBlockApp(IApplication app)` recibe la app; `Program.cs` es el Composition Root (`new FocusBlockApp(Application.Create())`); los tests pasan un mock de Moq.

### Cómo se usa

```csharp
// Producción (Program.cs)
FocusBlockApp app = new(Application.Create());
// Test
var app = new FocusBlockApp(Mock.Of<IApplication>());
```

### Error común

Dispersar `new` en la lógica de negocio en lugar de concentrarlo en el entry point.

### Referencias

- `src/FocusBlock.Tui/Program.cs` · `App.cs` · `tests/FocusBlock.Tests.Unit/FocusBlockAppTests.cs`

---

## Container/Presentational

### Qué es

Separar los componentes que **orquestan** (ciclo de vida, composición) de los que **presentan** (solo dibujan UI).

### Qué problema resuelve

Que una clase no mezcle "cómo funciona la app" con "qué muestra la pantalla".

### Para qué sirve en este proyecto

`FocusBlockApp` = container (arma y corre la app). `MainWindow`/`StatusView`/`BlockListView`/`AddBlockView` = presentational (componen y muestran). Las vistas reciben datos (ej: `RefreshStatus(DaemonStatus)`) y los muestran.

### Cómo se usa

```csharp
// Presentational: no sabe de dónde viene el dato, solo lo muestra
public void RefreshStatus(DaemonStatus status) => _statusLabel.Text = ...;
```

### Error común

Poner lógica de negocio dentro de las vistas → dejan de ser testeables y crecen sin control.

### Referencias

- `src/FocusBlock.Tui/App.cs` · `Views/*.cs`

---

## Records como DTO

### Qué es

`record` con parámetros posicionales: tipo inmutable con propiedades `init`-only e igualdad por valor.

### Qué problema resuelve

Transportar datos sin ceremonia: no hay setters, no hay `Equals` manual.

### Para qué sirve en este proyecto

`DaemonStatus(bool IsRunning, TimeSpan Uptime, int ActiveBlocks)` lleva el estado del daemon hacia `StatusView`.

### Cómo se usa

```csharp
var status = new DaemonStatus(IsRunning: true, Uptime: TimeSpan.FromMinutes(5), ActiveBlocks: 2);
view.RefreshStatus(status);
```

### Error común

Usar `class` mutable para datos que solo se leen; con `record` se gana inmutabilidad y `with` para copiar.

### Referencias

- `src/FocusBlock.Tui/Models/DaemonStatus.cs` · `Views/StatusView.cs`

---

## ObservableCollection + ListView (datos vivos)

### Qué es

`ObservableCollection<T>` notifica cambios; `ListView` (widget de Terminal.Gui) se suscribe y redibuja solo.

### Qué problema resuelve

Que la lista se actualice automáticamente al modificar los datos, sin redibujado manual.

### Para qué sirve en este proyecto

`BlockListView` usa `SetSource(_apps)` sobre una `ObservableCollection<string>`; `ShowApps` hace `Clear` + `Add` y la pantalla se actualiza sola. Se expone `Apps` (IReadOnlyList) para el test.

### Cómo se usa

```csharp
_listView.SetSource(_apps);          // conecta la fuente al widget
_apps.Clear(); foreach (var a in apps) _apps.Add(a);  // cambios → se reflejan
```

### Error común

Usar `List<T>` plano y esperar que el widget se entere de los cambios. Necesita `ObservableCollection` (o notificación manual) para el binding reactivo.

### Referencias

- `src/FocusBlock.Tui/Views/BlockListView.cs`

---

## xUnit + Moq + FluentAssertions (tests aislados)

### Qué es

xUnit = framework que define y corre los tests (`[Fact]`). Moq = crea **dobles** de dependencias (`Mock.Of<IApplication>()`). FluentAssertions = aserciones legibles (`Should().NotBeNull()`).

### Qué problema resuelve

Testear una unidad **aislada** (sin terminal, sin red, sin disco), rápido y determinista.

### Para qué sirve en este proyecto

La suite de la fase (6 tests): estructura de `MainWindow`, navegación, `StatusView`, `BlockListView`, `AddBlockView` y orquestación. Cada test sigue AAA (Arrange → Act → Assert).

### Cómo se usa

```csharp
[Fact]
public void FocusBlockApp_CreatesMainWindow()
{
    var app = new FocusBlockApp(Mock.Of<IApplication>());   // Arrange
    app.MainWindow.Should().NotBeNull();                     // Act + Assert
}
```

### Error común

Mockear de más (solo se mockea lo externo a la unidad) o no saber **qué no prueba** el test (el alcance: mecanismo ≠ wiring del menú).

### Referencias

- `tests/FocusBlock.Tests.Unit/*.cs` · `docs/development-plan.md`

---

## Relación entre estos conceptos

Terminal.Gui v2 define CÓMO se compone la UI (API por instancia, `Pos`/`Dim`, `Add`). La navegación (`ShowView`) organiza QUÉ se muestra. DI + Composition Root + Container/Presentational hacen el sistema TESTEABLE, y xUnit + Moq + FluentAssertions lo VERIFICAN. Los `record` y `ObservableCollection` son los vehículos de datos entre el orquestador y las vistas. Juntos explican por qué el esqueleto TUI se ve así y cómo se sostiene con tests.