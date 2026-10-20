# Fase 01 — TUI: composición, layout y navegación

> Qué construye: una ventana con menú, tres vistas y navegación entre ellas, todo testeable
> sin abrir una terminal.
> Antes: [Fase 00](phase-00-setup.md) · Después: [Fase 02](phase-02-config.md) ·
> [Glosario](glossary.md)

## Dónde estamos

Existe una ventana con un label. Falta el marco fijo (menú y barra de estado), varias
pantallas y la forma de cambiar entre ellas sin recrear la ventana.

```text
IApplication (inyectada)
   └── MainWindow : Window
        ├── MenuBar     ── acciones → ShowView
        ├── Content     ── StatusView | BlockListView | AddBlockView
        └── StatusBar
```

## Qué construye esta fase

`FocusBlockApp` orquesta el ciclo de vida; `MainWindow` compone el marco y enruta; las tres
vistas muestran datos o formularios. Seis tests verifican estructura y navegación. La UI aún
no tiene datos reales.

## El recorrido de una acción

1. El usuario abre el menú `Block`.
2. Elige `New Block`.
3. El `Action` del `MenuItem` llama `ShowView(AddBlockView)`.
4. `ShowView` desmonta el contenido actual y monta el nuevo.
5. El layout recalcula posiciones y tamaños.
6. El driver dibuja el formulario en el área de contenido.

## Los conceptos

| # | Concepto | Idea en una línea | Código |
|---|---|---|---|
| 1 | Composición y layout | La pantalla es un árbol con relaciones de tamaño | `MainWindow.cs` |
| 2 | Navegación | Cambiar el contenido sin recrear la ventana | `MainWindow.cs` |
| 3 | DI y roles | Construir afuera, recibir dentro; orquestar no es dibujar | `App.cs` |
| 4 | Datos vivos | `record` transporta; `ObservableCollection` notifica | `BlockListView.cs` |
| 5 | Tests aislados | Dobles en vez de infraestructura real | `*Tests.cs` |

---

## 1. Composición y layout

**Idea.** Una pantalla se arma agregando vistas dentro de vistas, y cada una declara su
tamaño como relación con el contenedor o con otra vista.

**Analogía.** Es una casa de muñecas: cada habitación contiene muebles y cada mueble se ubica
respecto de la pared, no con coordenadas absolutas. Si cambia la casa, el mueble sigue
pegado a la pared.

**Cómo funciona.** 1) `MainWindow` crea sus vistas hijas. 2) Asigna `Content` y declara
`Dim.Fill()` para ocupar el espacio. 3) `Add(MenuBar, Content, StatusBar)` monta el árbol;
el último se dibuja encima (z-order). 4) `Pos.Right(label) + 1` alinea un campo con su
etiqueta sin adivinar el ancho del texto.

```cs
// src/FocusBlock.Tui/Views/MainWindow.cs
Content = StatusView;
Content.Y = 1;
Content.Width = Dim.Fill();
Content.Height = Dim.Fill(1);
Add(MenuBar, Content, StatusBar);   // orden = z-order

// src/FocusBlock.Tui/Views/AddBlockView.cs
AppNameField = new TextField { X = Pos.Right(AppNameLabel) + 1, Y = 0, Width = 20 };
```

**Trampa.** Creer que `Add` es una lista: si una vista no está en el árbol hasta la raíz, no
se dibuja. Y sin `Width`/`Height` declarados, ocupa cero espacio.

<details><summary>Verifica</summary>
1. ¿Por qué cualquier vista puede contener otra?
2. ¿Qué diferencia hay entre `X = 5` y `Pos.Right(label) + 1`?
3. ¿Qué pasa si redimensionas la terminal con `Dim.Fill()`?
</details>

---

## 2. Navegación

**Idea.** `ShowView` intercambia la vista de contenido: desmonta la actual, monta la nueva y
conserva el marco de la ventana.

**Analogía.** Es cambiar el canal de la televisión: el televisor (ventana, menú, barra) no se
apaga ni se reconstruye; solo cambia lo que se ve.

**Cómo funciona.** 1) El menú dispara una acción con la vista destino. 2) Si ya es el
contenido actual, retorna: la operación es idempotente. 3) `Remove(Content)` desmonta la
anterior. 4) `Content = view` actualiza el puntero y `Add(view)` la monta.

```cs
// src/FocusBlock.Tui/Views/MainWindow.cs
public void ShowView(View view)
{
    if (view == Content) return;   // idempotencia

    Remove(Content);               // desmonta la anterior
    Content = view;                // actualiza el puntero
    Add(view);                     // monta la nueva
}
```

**Trampa.** Creer que `ShowView` destruye la vista anterior. Solo la desmonta: el objeto
sigue vivo y se puede volver a montar.

<details><summary>Verifica</summary>
1. ¿Qué pasa si llamas `ShowView` con la vista que ya está visible?
2. ¿Por qué no se recrea `MainWindow` en cada navegación?
3. ¿Qué hace `Remove` y qué no hace?
</details>

---

## 3. DI y roles

**Idea.** Una clase declara lo que necesita y lo recibe por constructor. Un único punto
decide la implementación, y quien orquesta no debería dibujar.

**Analogía.** Es un pañol central: cada empleado pide su herramienta armada en vez de
fabricarla. Y el director no toca los instrumentos: cada rol tiene su trabajo.

**Cómo funciona.** 1) La clase pide una interfaz: `FocusBlockApp(IApplication app)`. 2)
`Program.cs` crea la implementación real (Composition Root). 3) El test pasa un doble. 4)
`FocusBlockApp` orquesta, `MainWindow` enruta y las vistas solo muestran datos.

```cs
// src/FocusBlock.Tui/Program.cs
FocusBlockApp app = new(Application.Create());
app.Run();

// src/FocusBlock.Tui/App.cs
public FocusBlockApp(IApplication app)
{
    _app = app;
    MainWindow = new MainWindow(OnEarlyStopAsync);
}

// src/FocusBlock.Tui/Views/StatusView.cs — presentacional: solo formatea
public void RefreshStatus(DaemonStatus status)
{
    _statusLabel.Text =
        $"Daemon: {(status.IsRunning ? "running" : "stopped")} · " +
        $"Uptime: {status.Uptime} · " +
        $"Active blocks: {status.ActiveBlocks}";
}
```

**Trampa.** Dispersar `new` en la lógica: señal de que un test obliga a levantar terminal,
red o disco. Y poner lógica de negocio en una vista: deja de ser testeable sin mocks.

<details><summary>Verifica</summary>
1. ¿Qué decide el Composition Root y qué no?
2. ¿Por qué `StatusView` no necesita mocks?
3. ¿Dónde moverías una consulta al daemon hecha desde una vista?
</details>

---

## 4. Datos vivos

**Idea.** Un `record` transporta datos inmutables y se compara por contenido. Una
`ObservableCollection` notifica cambios y la vista se redibuja sola.

**Analogía.** El `record` es un sobre cerrado: viaja con datos y nadie lo modifica. La
colección observable es un altavoz: cuando cambia la lista, avisa a quien esté suscrito.

**Cómo funciona.** 1) El `record` genera constructor, propiedades `init` y `Equals` por
valor. 2) `ObservableCollection` dispara `CollectionChanged` en `Add`, `Remove` y `Clear`.
3) `ListView.SetSource(_apps)` se suscribe. 4) `ShowApps` limpia y agrega: el widget se
actualiza en el próximo ciclo.

```cs
// src/FocusBlock.Tui/Models/DaemonStatus.cs
public record DaemonStatus(bool IsRunning, TimeSpan Uptime, int ActiveBlocks);

// src/FocusBlock.Tui/Views/BlockListView.cs
private readonly ObservableCollection<string> _apps = new();
public IReadOnlyList<string> Apps => _apps;

_listView.SetSource(_apps);   // el widget escucha CollectionChanged

public void ShowApps(IEnumerable<string> apps)
{
    _apps.Clear();
    foreach (string app in apps) _apps.Add(app);
}
```

**Trampa.** Usar `List<T>` y esperar que la vista se entere. Solo las operaciones sobre la
colección notifican; mutar un elemento no dispara nada.

<details><summary>Verifica</summary>
1. ¿Qué genera el compilador con un `record` posicional?
2. ¿Por qué `ObservableCollection` y no `List<T>`?
3. ¿Qué operaciones disparan `CollectionChanged` y cuáles no?
</details>

---

## 5. Tests aislados

**Idea.** Un test unitario prueba una pieza sin infraestructura real. Moq reemplaza lo
externo y FluentAssertions hace legible el resultado.

**Analogía.** Es un simulador de vuelo: practicas la maniobra sin el avión real. Si algo
falla, sabes que es tu maniobra, no el clima.

**Cómo funciona.** 1) xUnit descubre los `[Fact]` y crea una instancia nueva por test. 2)
**Arrange**: preparas objetos y dobles. 3) **Act**: ejecutas una acción. 4) **Assert**:
verificas con `Should()`.

```cs
// tests/FocusBlock.Tests.Unit/FocusBlockAppTests.cs
[Fact]
public void FocusBlockApp_CreatesMainWindow()
{
    var app = new FocusBlockApp(Mock.Of<IApplication>());

    app.MainWindow.Should().NotBeNull();
    app.MainWindow.Should().BeOfType<MainWindow>();
}
```

**Trampa.** Mockear de más: un test con varios mocks suele probar la colaboración y no la
unidad. Y `MainWindow_MenuNavigatesToViews` prueba el mecanismo, no el cableado del menú.

<details><summary>Verifica</summary>
1. ¿Por qué xUnit crea una instancia nueva por test?
2. ¿Qué diferencia hay entre un test double y un mock?
3. ¿Qué NO prueba `MainWindow_MenuNavigatesToViews`?
</details>

---

## Práctica

### Mezcla

1. Cambias `Dim.Fill(1)` por `Dim.Fill()`: ¿qué tapa la barra de estado?
2. Si `StatusView` consultara al daemon, ¿qué test deja de ser unitario?
3. ¿Por qué `ShowView` no destruye la vista anterior?
4. ¿Qué rol cumple `Program.cs` y qué rol no cumple?

<details><summary>Respuestas</summary>
1. El contenido ocupa toda la altura y se dibuja encima de la barra; decide el z-order.
2. `StatusViewTests`: necesitaría un daemon o un doble.
3. Solo la desmonta del árbol; el objeto sigue vivo.
4. Es el Composition Root: construye el grafo, no orquesta la UI.
</details>

### En código

1. Predice qué pasa si llamas `ShowView(StatusView)` dos veces con y sin el guard.
2. Escribe el test RED de `MainWindow_MenuNavigatesToViews`.

### Antes de cerrar

- [ ] Puedo dibujar el árbol de `MainWindow` de memoria.
- [ ] Sé por qué `Content` usa `Dim.Fill()` y `Dim.Fill(1)`.
- [ ] Puedo explicar qué test se rompe si una vista consulta al daemon.

## Trampas reales de la fase

- **`MenuItem(string, Action)` no existe en 2.4.17**: se resolvió con object-initializer.
- **`StatusItem` no existe**: `StatusBar` usa `Shortcut`.
- **`y` vs `Y`**: el case-sensitive reapareció al componer vistas.

## Para profundizar

- [`ADR-008`](../adr/ADR-008-testing-stack.md) — stack de testing.
- [`ADR-001`](../adr/ADR-001-terminal-gui.md) — Terminal.Gui v2.
- Código: `src/FocusBlock.Tui/Views/` y `tests/FocusBlock.Tests.Unit/`.
