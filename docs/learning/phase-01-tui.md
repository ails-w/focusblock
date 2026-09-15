# Fase 01 — Esqueleto TUI: Composición, Layout y Navegación

> Qué se aprende: cómo se compone un árbol de vistas, cómo se declara su layout, cómo se
> navega entre pantallas y cómo todo eso se vuelve testeable con DI y tests unitarios.
> Log de la fase (tareas, decisiones, problemas) → `docs/progress-log/phase-01-tui.md`
> Fase anterior → `docs/learning/phase-00-setup.md`
> Diagramas de la fase → `docs/diagrams/phase-01-tui.md`

## Glosario de la fase

| Término | Qué significa (en una línea) |
|---|---|
| Composición de UI | Armar una pantalla agregando vistas dentro de vistas, formando un árbol. |
| Árbol de vistas | Jerarquía padre-hijo que determina qué se dibuja y en qué orden. |
| `View` | Elemento visual base de Terminal.Gui; puede contener otros `View`. |
| `Window` | Vista contenedora con borde y título; deriva de `Runnable`, que deriva de `View`. |
| Layout absoluto | Posición/tamaño fijos (ej: `X = 3`, `Width = 20`). |
| Layout relativo | Posición/tamaño según el contenedor u otra vista (`Pos.Right`, `Dim.Fill`). |
| z-order | Orden de apilado: quién se dibuja encima de quién. |
| Grafo de dependencias | Mapa de "quién necesita a quién" entre objetos. |
| Composition Root | Único punto del programa donde se construye el grafo de dependencias. |
| DI (Dependency Injection) | Recibir las dependencias por constructor en vez de crearlas adentro. |
| Container / Presentational | Separar componentes que orquestan de componentes que solo muestran. |
| DTO | Objeto que solo transporta datos, sin comportamiento de negocio. |
| `record` | Tipo de C# con igualdad por valor y propiedades inmutables por defecto. |
| Inmutabilidad | Que un objeto no cambie después de crearse. |
| Igualdad por valor | Iguales si sus datos coinciden, aunque sean objetos distintos. |
| Observer | Patrón donde un sujeto notifica a sus suscriptores cuando cambia. |
| Test double | Objeto falso que reemplaza a una dependencia real en un test. |
| Mock | Test double con expectativas/verificación programadas. |
| AAA | Arrange–Act–Assert: las tres fases de un test. |

## Mapa de conceptos

```text
Terminal.Gui v2 (API por instancia)
  ├── Composición de UI ──▶ árbol de vistas (View / Window)
  ├── Layout declarativo (Pos / Dim) ──▶ absoluto vs relativo
  └── Navegación (ShowView) ──▶ z-order + idempotencia

Testabilidad
  ├── DI + Composition Root ──▶ resuelve el grafo de dependencias
  ├── Container / Presentational ──▶ separa orquestar de mostrar
  └── xUnit + Moq + FluentAssertions ──▶ verifica (AAA, aislado)

Datos
  ├── record / DTO ──▶ inmutabilidad + igualdad por valor
  └── ObservableCollection ──▶ Observer: datos vivos que empujan cambios a la UI
```

---

## Terminal.Gui v2 — API por instancia y composición

### En una frase

Una pantalla TUI no es una lista plana de controles: es un **árbol** que se arma
agregando vistas dentro de vistas.

### Fundamentos previos

**¿Qué es composición de UI?**
Componer es construir algo complejo a partir de piezas más simples. En UI, componés
cuando metés un `Label` dentro de un `View`, y ese `View` dentro de una `Window`. Cada
pieza tiene una responsabilidad chica y el conjunto forma la pantalla. La alternativa es
una clase gigante que dibuja todo a mano: imposible de reutilizar y de testear.

**¿Qué es un árbol de vistas?**
Es la jerarquía resultante de esa composición. La `Window` es la raíz; sus hijos directos
son `MenuBar`, `Content` y `StatusBar`; y `Content` a su vez contiene la vista activa.
El motor de rendering recorre el árbol para decidir qué dibujar. Cuando una vista cambia,
solo se recalcula su rama.

**¿Qué diferencia hay entre `View` y `Window`?**
`View` (namespace `Terminal.Gui.ViewBase`) es el elemento visual base: tiene posición,
tamaño, color, puede dibujar y puede contener otros `View`. `Window`
(`Terminal.Gui.Views`) es un contenedor con borde y título opcional. La jerarquía real,
verificada contra la DLL 2.4.17, es:

```text
Terminal.Gui.Views.Window
  └── Terminal.Gui.Views.Runnable   ← agrega el comportamiento de "vista ejecutable"
        └── Terminal.Gui.ViewBase.View   ← elemento visual base (Add/Remove viven acá)
              └── System.Object
```

Es decir: `Window` es una `View` especializada que, además, puede ser "corrida" por la
aplicación. Por eso `MainWindow : Window` y `StatusView : View`: la ventana es la raíz
ejecutable; las vistas de contenido son piezas que se montan dentro.

**¿Por qué importa que `Add`/`Remove` estén en `View`?**
Porque significa que *cualquier* vista puede contener a otra. La composición es
recursiva: no hay un tipo especial de "contenedor"; el árbol se forma con la misma
operación a cualquier nivel.

### Qué es

Terminal.Gui v2 expone un modelo por instancia (`IApplication`) y una API de composición
donde agregás vistas hijas con `Add(...)`. La ventana principal de FocusBlock hereda de
`Window` y arma su árbol en el constructor.

### Qué problema resuelve

Sin composición, cada vista tendría que conocer y dibujar sus vecinas: acoplamiento
total. Con composición, cada vista declara qué contiene y el framework se encarga del
resto. Y sin API por instancia, la ventana no sería inyectable ni testeable.

### Cómo funciona paso a paso

1. `MainWindow` crea sus vistas hijas: `StatusView`, `BlockListView`, `AddBlockView`.
2. Construye `MenuBar` y `StatusBar` con sus ítems.
3. Asigna `Content = StatusView` y declara su layout (`X`, `Y`, `Width`, `Height`).
4. Llama `Add(MenuBar, Content, StatusBar)`: eso inserta los tres como hijos de la
   ventana, en ese orden.
5. `FocusBlockApp.Run()` pasa la ventana a `app.Run(window)`, que recorre el árbol para
   dibujar.

### Qué se rompería sin esto en FocusBlock

`MainWindowTests.MainWindow_HasMenuBarAndStatusBar` verifica que `MenuBar` y `StatusBar`
existan como propiedades. Sin composición, esos controles serían variables locales
inaccesibles y el test no podría inspeccionarlos. Sin API por instancia, el test no
podría instanciar `MainWindow` sin una terminal real.

### Para qué sirve en este proyecto

`MainWindow` es el hub: compone el árbol y expone propiedades públicas (`MenuBar`,
`StatusBar`, `Content`, las vistas) para que los tests verifiquen la estructura. Las
vistas hijas a su vez componen: `StatusView` agrega un `Label`, `BlockListView` agrega un
`ListView`, `AddBlockView` agrega labels, fields y un botón.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Tui/Views/MainWindow.cs:44-50
Content = StatusView;
Content.X = 0;
Content.Y = 1;
Content.Width = Dim.Fill();
Content.Height = Dim.Fill(1);

Add(MenuBar, Content, StatusBar);   // orden = z-order (el último se dibuja encima)
```

### Error común

Creer que `Add` es solo una lista y no un árbol. Si agregás la misma vista a dos padres,
o si esperás que un `Label` se vea sin haberlo agregado a ningún contenedor, el rendering
no aparece. Detección: si una vista no se dibuja, revisá que esté en el árbol (agregada
con `Add`) y que su padre esté a su vez en el árbol hasta la raíz.

### Para profundizar

- `docs/learning/phase-00-setup.md` — API por instancia.
- `docs/diagrams/phase-01-tui.md` — diagrama de archivos y dependencias.
- Código: `src/FocusBlock.Tui/App.cs`, `Views/MainWindow.cs`, `Views/*.cs`.

---

## Layout declarativo: `Pos` y `Dim`

### En una frase

En vez de calcular coordenadas a mano, declarás *relaciones*: "a la derecha de este
label", "llená el contenedor menos una fila".

### Fundamentos previos

**¿Qué es layout absoluto?**
Posicionar con números fijos: `X = 0`, `Y = 1`, `Width = 20`. Es simple pero frágil: si
cambia el tamaño de la terminal o el tamaño de un vecino, los números quedan mal. El
layout absoluto asume que nada se mueve.

**¿Qué es layout relativo?**
Definir posición/tamaño en función de otra cosa: del contenedor (`Dim.Fill()`) o de otra
vista (`Pos.Right(label) + 1`). El framework recalcula cuando algo cambia. Es más
robusto a redimensionamientos.

**¿Por qué importa la diferencia?**
Porque una TUI vive en una terminal que el usuario puede redimensionar en cualquier
momento. Si todo fuera absoluto, al agrandar la ventana quedarían huecos negros y
controles desalineados. El layout relativo expresa la *intención* ("pegado a la derecha")
y deja que el motor la resuelva.

**La grilla de una terminal:** el origen `(0,0)` es la esquina superior izquierda. `X`
crece hacia la derecha, `Y` hacia abajo. Una celda = un carácter.

### Qué es

`Pos` calcula coordenadas X/Y; `Dim` calcula ancho/alto. Ambos viven en
`Terminal.Gui.ViewBase`. Se combinan con operadores: `Pos.Right(vista) + 1`,
`Dim.Fill(1)`.

### Qué problema resuelve

El layout calculado a mano depende del orden en que se midan las cosas y del tamaño
actual. El layout declarativo delega ese cálculo al framework, que lo rehace cada vez que
hace falta (por ejemplo, tras un resize).

### Cómo funciona paso a paso

1. Asignás `view.X = Pos.Right(otraVista) + 1`: guardás una *expresión*, no un número.
2. En cada ciclo de layout, el framework evalúa la expresión contra las dimensiones
   actuales.
3. `Dim.Fill()` resuelve a "todo el espacio del contenedor"; `Dim.Fill(1)` a "todo menos
   1 fila" (o columna, según el eje).
4. Asignar un `int` a `Width`/`Height` se convierte implícitamente a `Dim` absoluto.
5. El resultado se aplica y se dibuja.

### Qué se rompería sin esto en FocusBlock

El `Content` se dibuja debajo del menú (`Y = 1`) y deja la última fila para la
`StatusBar` (`Height = Dim.Fill(1)`). Sin layout relativo, cada vista tendría que calcular
su alto restando el menú y la barra: y si mañana cambia el alto del menú, habría que
editar todas las vistas. En `AddBlockView`, `X = Pos.Right(AppNameLabel) + 1` pone el
field justo a la derecha de su etiqueta sin "adivinar" el ancho del texto.

### Para qué sirve en este proyecto

- `MainWindow` usa `Dim.Fill()` / `Dim.Fill(1)` para repartir la ventana entre menú,
  contenido y status bar.
- `AddBlockView` usa `Pos.Right(...) + 1` para alinear cada field con su label.
- `BlockListView` usa `Dim.Fill()` para que el `ListView` ocupe todo su contenedor.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Tui/Views/AddBlockView.cs:16-20
AppNameLabel = new Label { Text = "App name:", X = 0, Y = 0 };
AppNameField = new TextField { X = Pos.Right(AppNameLabel) + 1, Y = 0, Width = 20 };
ScheduleLabel = new Label { Text = "Schedule:", X = 0, Y = 2 };
ScheduleField = new TextField { X = Pos.Right(ScheduleLabel) + 1, Y = 2, Width = 20 };
AddButton = new Button { Text = "Add Block", X = 0, Y = 4 };
```

### Error común

Asumir que un `View` se ve aunque no le declares `Width`/`Height`. En v2 el layout se
calcula a partir de esas propiedades; si quedan en cero o sin definir, la vista no ocupa
espacio. Otra trampa: mezclar `Pos.Right(label)` con un `X` absoluto y esperar que ambos
convivan (el último asignado gana). Detección: si algo no aparece o aparece encimado,
revisá `X`, `Y`, `Width`, `Height` de esa vista y de su contenedor.

### Para profundizar

- Código: `Views/MainWindow.cs`, `Views/AddBlockView.cs`, `Views/BlockListView.cs`.
- `docs/learning/phase-01-tui.md` — este mismo archivo, sección de z-order.

---

## Navegación entre vistas

### En una frase

`ShowView` cambia qué vista ocupa el área de contenido: saca la actual y monta la nueva,
sin recrear la ventana.

### Fundamentos previos

**¿Qué es z-order?**
Es el orden de apilado en el eje Z (profundidad). En una pantalla plana, "encima" y
"debajo" son metáforas: una vista dibujada después tapa a la anterior. En `Add`, el
último agregado se dibuja encima. Importa porque define qué se ve cuando dos vistas
ocupan el mismo espacio.

**¿Por qué intercambiar `Content` en vez de crear y destruir ventanas?**
Porque recrear la ventana perdería estado (posición, tamaño, menú) y dispararía el ciclo
de vida completo. Intercambiar la vista de contenido es barato y mantiene el marco
(menú + status bar) estable, como cambiar el canal de una TV.

**¿Qué es idempotencia?**
Una operación es idempotente si ejecutarla dos veces produce el mismo resultado que
ejecutarla una vez. Acá: llamar `ShowView(vista)` cuando esa vista ya es el `Content` no
debe hacer nada.

### Qué es

Un método de `MainWindow` que hace `Remove(Content)`, asigna `Content = view` y llama
`Add(view)`. Antes verifica `if (view == Content) return;` para evitar trabajo innecesario.

### Qué problema resuelve

Da navegación con estado mínimo: una sola área de contenido que se intercambia. El menú
dispara la operación y el usuario ve otra pantalla.

### Cómo funciona paso a paso

1. El usuario elige un `MenuItem`; su `Action` (un delegado) llama `ShowView(vista)`.
2. Si `vista` ya es `Content`, el guard retorna: nada que hacer.
3. Si no, `Remove(Content)` desmonta la vista actual del árbol.
4. `Content = vista` actualiza el puntero al contenido.
5. `Add(vista)` monta la nueva en el árbol; en el próximo ciclo de layout se dibuja.

### Qué se rompería sin esto en FocusBlock

Sin el guard, si el usuario elige la misma opción dos veces, se haría `Remove` + `Add`
del mismo objeto. Eso puede causar parpadeo o reordenar el z-order de forma innecesaria.
Sin `ShowView`, cada `Action` tendría que manipular el árbol a mano, duplicando la lógica
en cada ítem del menú.

### Para qué sirve en este proyecto

Los ítems del menú `Block → List`, `Block → New Block` y `View → Status` llaman a
`ShowView` con la vista destino (`MainWindow.cs:25-29`). El `Content` inicial es
`StatusView` (`MainWindow.cs:44`).

### Cómo se usa (código real)

```cs
// src/FocusBlock.Tui/Views/MainWindow.cs:23-29 (wiring del menú)
MenuBar = new MenuBar([
    new MenuBarItem("_Block", [
        new MenuItem { Title = "_New Block", Action = () => ShowView(AddBlockView) },
        new MenuItem { Title = "_List", Action = () => ShowView(BlockListView) },
    ]),
    // ...
]);

// src/FocusBlock.Tui/Views/MainWindow.cs:53-63 (la operación)
public void ShowView(View view)
{
    if (view == Content)
    {
        return;                     // idempotencia: ya está mostrada
    }

    Remove(Content);                // desmonta la anterior
    Content = view;                 // actualiza el puntero
    Add(view);                      // monta la nueva
}
```

### Error común

No proteger la idempotencia: `Remove`+`Add` del mismo objeto en cada clic. Otra: creer
que `ShowView` "destruye" la vista anterior. No la destruye: la desmonta del árbol; el
objeto sigue vivo y se puede volver a montar. Detección: parpadeo o saltos de foco al
elegir repetidamente la misma opción.

### Para profundizar

- `docs/diagrams/phase-01-tui.md` — secuencia de navegación.
- `tests/FocusBlock.Tests.Unit/MainWindowTests.cs` — `MainWindow_MenuNavigatesToViews`.

---

## Dependency Injection + Composition Root

### En una frase

En vez de que cada clase cree lo que necesita, alguien se lo entrega ya construido, y ese
"alguien" es un único punto de entrada.

### Fundamentos previos

**¿Qué es un grafo de dependencias?**
Es el mapa de quién necesita a quién. `FocusBlockApp` necesita una `IApplication`;
`MainWindow` necesita (crea) sus vistas; los tests necesitan una `IApplication` falsa.
Cuando dibujás esas flechas, obtenés un grafo dirigido.

**¿Por qué el "new disperso" es un problema?**
Si cada clase hace `new` de sus dependencias, el grafo queda *codificado dentro* de las
clases. Consecuencias: (1) no podés sustituir una dependencia por un doble en un test;
(2) no podés cambiar la implementación sin editar la clase; (3) un cambio en el
constructor de la dependencia obliga a tocar todos los sitios que la crean; (4) el grafo
no se ve en un solo lugar, hay que reconstruirlo mentalmente.

**¿Qué es Inversion of Control (IoC)?**
Es el principio general: la clase no controla la creación de sus dependencias, se las
inyectan. DI (Dependency Injection) es una forma concreta de IoC: recibir por
constructor. El *Composition Root* es el lugar único donde se decide qué implementación
concreta va para cada interfaz.

**Una analogía:** el `new` disperso es como si cada empleado fabricara sus propias
herramientas. La DI es un pañol central que se las entrega ya armadas; el Composition
Root es el pañol.

### Qué es

DI: una clase declara sus dependencias como parámetros de constructor y las guarda. Las
dependencias suelen ser interfaces. Composition Root: el único lugar (idealmente `Main` /
`Program.cs`) donde se instancian las implementaciones reales y se arma el grafo.

### Qué problema resuelve

El acoplamiento y la no-testabilidad. Si `FocusBlockApp` construyera su propia
`IApplication`, no habría forma de testearlo sin abrir una terminal real.

### Cómo funciona paso a paso

1. La clase declara lo que necesita como interfaz: `FocusBlockApp(IApplication app)`.
2. En producción, `Program.cs` crea la implementación real (`Application.Create()`) y la
   pasa.
3. En un test, se crea un doble (`Mock.Of<IApplication>()`) y se pasa.
4. La clase usa la interfaz sin saber cuál implementación recibió.
5. Si mañana cambia el entorno, se cambia solo el Composition Root.

### Qué se rompería sin esto en FocusBlock

`FocusBlockAppTests.FocusBlockApp_CreatesMainWindow` hace
`new FocusBlockApp(Mock.Of<IApplication>())`. Si `FocusBlockApp` hiciera
`Application.Create()` adentro, ese test no podría ejecutarse sin terminal y la suite
dejaría de ser unitaria.

### Para qué sirve en este proyecto

`Program.cs` es el Composition Root (6 líneas): crea `Application.Create()` y lo pasa al
constructor. `App.cs` guarda `_app` y lo usa en `Run()`. Es la única dependencia inyectada
hoy; las vistas todavía se crean dentro de `MainWindow`, que es un punto de mejora
natural cuando aparezcan servicios reales (config, IPC).

### Cómo se usa (código real)

```cs
// src/FocusBlock.Tui/Program.cs — Composition Root
FocusBlockApp app = new(Application.Create());

// src/FocusBlock.Tui/App.cs:13-17 — inyección por constructor
public FocusBlockApp(IApplication app)
{
    _app = app;
    MainWindow = new MainWindow();
}

// tests/FocusBlock.Tests.Unit/FocusBlockAppTests.cs:17 — doble en tests
var app = new FocusBlockApp(Mock.Of<IApplication>());
```

### Error común

Dispersar `new` por la lógica de negocio: por ejemplo, que `StatusView` cree su propio
servicio de daemon. Detección: si un test de una clase obliga a levantar infraestructura
real (red, disco, terminal), esa clase está creando sus dependencias en vez de recibirlas.
Señal de alarma en código: `new` dentro de métodos de lógica (no en constructores de
objetos de datos ni en el Composition Root).

### Para profundizar

- `docs/diagrams/phase-01-tui.md` — grafo de dependencias.
- Código: `src/FocusBlock.Tui/Program.cs`, `App.cs`.
- Concepto siguiente: **Container/Presentational**, que divide roles dentro de ese grafo.

---

## Container/Presentational

### En una frase

Unos componentes deciden y orquestan; otros solo muestran. No mezcles los dos roles en la
misma clase.

### Fundamentos previos

Este concepto es una aplicación del **SRP (Single Responsibility Principle)**: una clase
debería tener una sola razón para cambiar. Acá la "razón" es el rol: el container cambia
si cambia el ciclo de vida o el ruteo; la vista cambia si cambia cómo se ve una pantalla.
Son razones independientes, entonces son clases separadas.

**¿Qué es un componente presentacional?**
Recibe datos ya resueltos y los dibuja. No sabe de dónde vienen, no decide navegación, no
toca servicios. Eso lo hace trivialmente testeable: le pasás un dato y verificás el
resultado.

**¿Qué es un componente container?**
Sabe cómo se conecta todo: crea, compone, dispara acciones. Idealmente no dibuja detalles.

### Qué es

Un patrón de separación de responsabilidades en UI. El container es el "director de
orquesta"; las vistas presentacionales son los músicos que ejecutan su parte.

### Qué problema resuelve

Si una vista tiene lógica de negocio, deja de ser testeable (necesita esa lógica) y crece
sin control. Si el orquestador dibuja detalles, no se puede reutilizar. Separar permite
testear cada lado por separado.

### Cómo funciona paso a paso

1. `FocusBlockApp` actúa de container: recibe `IApplication`, crea `MainWindow`, corre la
   app.
2. `MainWindow` compone el marco y enruta (`ShowView`).
3. Las vistas (`StatusView`, `BlockListView`, `AddBlockView`) reciben datos y los
   muestran.
4. `RefreshStatus(DaemonStatus)` es el ejemplo puro: entra un dato, se actualiza un
   `Label`, no hay lógica de negocio.

### Qué se rompería sin esto en FocusBlock

`StatusView` no tiene lógica: solo formatea y muestra. Eso es exactamente lo que
`StatusViewTests` verifica, sin mockear nada. Si `StatusView` consultara el daemon,
tendría una dependencia real y el test dejaría de ser unitario.

### Para qué sirve en este proyecto

- Container: `FocusBlockApp` (ciclo de vida) y `MainWindow` (composición y ruteo).
- Presentational: `StatusView`, `BlockListView`, `AddBlockView`.
- Matiz honesto del proyecto: `MainWindow` hace ruteo y además compone, así que no es
  100% presentacional. Es un tradeoff aceptado en
  `docs/progress-log/phase-01-tui.md` (decisión 2) para mantener `FocusBlockApp` liviano.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Tui/Views/StatusView.cs:25-31 — presentacional puro
public void RefreshStatus(DaemonStatus status)
{
    _statusLabel.Text =
        $"Daemon: {(status.IsRunning ? "running" : "stopped")} · " +
        $"Uptime: {status.Uptime} · " +
        $"Active blocks: {status.ActiveBlocks}";
}
```

### Error común

Poner lógica de negocio en las vistas: consultar el daemon, leer config, formatear reglas.
Detección: si para testear una vista necesitás mockear un servicio, la vista está haciendo
trabajo que no le corresponde. Arreglo: mover esa lógica al container o a un servicio y
pasarle el resultado a la vista.

### Para profundizar

- Código: `src/FocusBlock.Tui/App.cs`, `Views/*.cs`.
- `docs/progress-log/phase-01-tui.md` — decisión 2 (tradeoff de `MainWindow`).

---

## Records como DTO

### En una frase

Un `record` con parámetros posicionales es un objeto inmutable que se compara por su
contenido, ideal para transportar datos.

### Fundamentos previos

**¿Qué es un DTO (Data Transfer Object)?**
Un objeto cuyo único trabajo es llevar datos de un lugar a otro. No tiene lógica de
negocio, no valida, no decide. Es un sobre con datos, no un empleado que los procesa.

**¿Qué es inmutabilidad?**
Un objeto inmutable no cambia después de crearse. Para "cambiarlo" se crea uno nuevo con
los valores deseados. Beneficios: es seguro compartirlo entre hilos, no hay sorpresas por
mutaciones inesperadas, y razonar sobre el código es más fácil porque el valor no cambia
bajo tus pies.

**¿Qué es igualdad por valor?**
Dos objetos son *iguales por valor* si todos sus datos son iguales, aunque sean objetos
distintos en memoria. Lo opuesto es *igualdad por referencia*: solo son iguales si son el
mismo objeto. Por defecto, las clases de C# usan igualdad por referencia; los `record`
usan igualdad por valor.

**¿Qué genera el compilador por vos?**
Cuando declarás `record DaemonStatus(bool IsRunning, TimeSpan Uptime, int ActiveBlocks)`,
el compilador genera automáticamente: un constructor, propiedades `init`-only, y
`Equals`/`GetHashCode` comparando *todos* los campos. Sin `record` tendrías que escribir
esos métodos a mano, en cada clase, y mantenerlos sincronizados.

**¿Qué significa `init`-only?**
La propiedad se puede asignar al construir el objeto y nunca más. Es la forma de tener
"setters de una sola vez".

### Qué es

`record` es un tipo de C# pensado para datos. La forma posicional declara, en una línea,
los campos que lo componen. `DaemonStatus` es un DTO con tres campos.

### Qué problema resuelve

Transportar datos sin ceremonia: no hay setters, no hay `Equals` manual, no hay riesgo de
mutación. Y permite usar `with` para crear una copia con un campo cambiado.

### Cómo funciona paso a paso

1. Declarás `record DaemonStatus(bool IsRunning, TimeSpan Uptime, int ActiveBlocks)`.
2. El compilador genera un constructor con esos tres parámetros.
3. Genera propiedades con `get` e `init` para cada uno.
4. Genera `Equals` y `GetHashCode` comparando los tres valores.
5. Podés construir con argumentos posicionales o nombrados.

### Qué se rompería sin esto en FocusBlock

`StatusViewTests` construye `new DaemonStatus(IsRunning: true, Uptime: ..., ActiveBlocks: 2)`.
Si `DaemonStatus` fuera una clase mutable con igualdad por referencia, cualquier
comparación de estados en tests o en lógica futura compararía *identidad* y no
*contenido*, dando falsos negativos. El uso de argumentos nombrados evita además el error
de pasar los valores en orden equivocado.

### Para qué sirve en este proyecto

`DaemonStatus` lleva el estado del daemon desde (en el futuro) la capa IPC hacia
`StatusView`, que solo lo muestra. Es el vehículo de datos entre la orquestación y la
presentación.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Tui/Models/DaemonStatus.cs (archivo completo)
public record DaemonStatus(bool IsRunning, TimeSpan Uptime, int ActiveBlocks);

// tests/FocusBlock.Tests.Unit/StatusViewTests.cs:15
view.RefreshStatus(
    new DaemonStatus(IsRunning: true, Uptime: TimeSpan.FromMinutes(5), ActiveBlocks: 2));
```

### Error común

Usar una `class` mutable para datos que solo se leen. Detección: si nadie debería cambiar
los campos después de construir el objeto, pero el tipo tiene `{ get; set; }`, es
candidato a `record` o a `init`. Otra trampa: asumir que `record` hace inmutables los
objetos *anidados*; si un `record` contiene una `List<T>`, la lista sigue siendo mutable.

### Para profundizar

- [Igualdad por valor y `System.ValueType`](https://learn.microsoft.com/dotnet/api/system.valuetype)
- Código: `src/FocusBlock.Tui/Models/DaemonStatus.cs`, `Views/StatusView.cs`.

---

## ObservableCollection + ListView (datos vivos)

### En una frase

`ObservableCollection` avisa cuando cambia; el `ListView` escucha y se redibuja solo.

### Fundamentos previos

**¿Qué es el patrón Observer?**
Un patrón donde un objeto "sujeto" mantiene una lista de "observadores" y los notifica
cuando cambia. Los observadores se suscriben a un evento y reaccionan. En .NET, los
eventos (`event`) son la implementación idiomática del patrón. La ventaja: el sujeto no
necesita conocer a sus observadores; solo publica "cambié".

**¿Push vs pull?**
En *pull*, la UI pregunta cada tanto "¿cambió algo?" (polling): desperdicia ciclos y
tiene latencia. En *push*, el dato notifica a la UI en el momento del cambio: la UI no
pregunta, reacciona. `ObservableCollection` es push.

**¿Qué es una colección observable?**
Una `List<T>` normal no notifica nada: si le agregás un elemento, nadie se entera. Una
`ObservableCollection<T>` implementa `INotifyCollectionChanged` y dispara un evento
`CollectionChanged` en cada `Add`, `Remove` y `Clear`. Terminal.Gui tiene
`ListView.SetSource<T>(ObservableCollection<T>)`, que se suscribe a ese evento y actualiza
el widget (verificado contra la DLL 2.4.17).

**¿Lista vs vista?**
La colección es el *modelo* (los datos). El `ListView` es la *vista* (cómo se muestran).
El binding las conecta: cambios en el modelo se reflejan en la vista.

### Qué es

`ObservableCollection<string>` guarda los nombres de apps; `ListView` se conecta con
`SetSource`. La colección expone los datos vivos; el widget los muestra.

### Qué problema resuelve

Sin notificación, cada cambio exigiría redibujar el widget a mano. Con Observer, se
modifica la colección y la vista se entera sola. Menos acoplamiento y menos código de
actualización.

### Cómo funciona paso a paso

1. `BlockListView` crea `_apps = new ObservableCollection<string>()`.
2. En el constructor, `_listView.SetSource(_apps)` conecta la colección al widget: el
   widget se suscribe al evento `CollectionChanged`.
3. `ShowApps(apps)` hace `_apps.Clear()` y `_apps.Add(...)` por cada app.
4. Cada operación dispara `CollectionChanged`.
5. El `ListView` reacciona y se redibuja en el próximo ciclo.

### Qué se rompería sin esto en FocusBlock

Si `BlockListView` usara `List<string>`, `SetSource` no tendría la sobrecarga observable y
el widget no se enteraría de los cambios: la lista quedaría desactualizada hasta un
redibujado manual. Además, `BlockListViewTests` verifica `view.Apps` (la colección
expuesta como `IReadOnlyList<string>`): la mirilla permite el test sin acoplarlo al
widget.

### Para qué sirve en este proyecto

Es el mecanismo de "datos vivos" para la lista de bloqueos: cuando la TUI lea las apps
bloqueadas del daemon, va a hacer `ShowApps(...)` y la lista se actualizará sola.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Tui/Views/BlockListView.cs:11-35
private readonly ListView _listView;
private readonly ObservableCollection<string> _apps = new();

public IReadOnlyList<string> Apps => _apps;

// ...
_listView.SetSource(_apps);   // conecta la fuente observable al widget

public void ShowApps(IEnumerable<string> apps)
{
    _apps.Clear();            // dispara CollectionChanged
    foreach (string app in apps)
    {
        _apps.Add(app);       // dispara CollectionChanged por cada alta
    }
}
```

### Error común

Usar `List<T>` plano y esperar que el widget se entere. Detección: modificás la colección
y la pantalla no cambia. Arreglo: `ObservableCollection<T>` (o notificar a mano). Otra
trampa: mutar los *elementos* de la colección (si fueran objetos mutables) no dispara
eventos; solo lo hacen las operaciones sobre la colección (`Add`, `Remove`, `Clear`).

### Para profundizar

- [Observer pattern (refactoring.guru)](https://refactoring.guru/design-patterns/observer)
- [`ObservableCollection<T>` — docs](https://learn.microsoft.com/dotnet/api/system.collections.objectmodel.observablecollection-1)
- Código: `src/FocusBlock.Tui/Views/BlockListView.cs`.

---

## xUnit + Moq + FluentAssertions (tests aislados)

### En una frase

Un test unitario aísla una pieza y verifica su comportamiento; Moq reemplaza lo externo y
FluentAssertions hace legible el resultado.

### Fundamentos previos

**¿Qué es un test unitario aislado?**
Prueba una unidad (una clase o un método) sin depender de infraestructura real: sin
terminal, sin red, sin disco. "Aislado" significa que si el test falla, sabés que el
problema está en esa unidad; y que debe dar el mismo resultado siempre (determinista).
Depender de una terminal real rompería ambas cosas.

**¿Qué es un test double y qué es un mock?**
Un *test double* es un objeto falso que reemplaza a una dependencia real en un test (igual
que un doble de riesgo reemplaza al actor en las escenas peligrosas). "Mock" es un tipo de
doble: uno con expectativas programadas, que además podés *verificar* (¿se llamó? ¿con qué
argumentos?). Moq crea estos dobles: `Mock.Of<IApplication>()` devuelve una implementación
falsa de la interfaz, sin terminal.

**¿Por qué mockear es la última opción?**
Porque cada mock es acoplamiento al *cómo* interactúa la unidad, no solo al *qué*
devuelve. Los mocks atan el test a la implementación; si cambiás cómo colaboran dos
objetos, los tests se rompen aunque el comportamiento externo sea correcto. Preferencia:
(1) usar valores reales simples (un `string`, un `int`); (2) usar una implementación real
liviana (un fake escrito a mano); (3) mockear solo para aislar algo lento/externo/no
determinista (terminal, red, reloj). En `FocusBlockAppTests` se mockea `IApplication`
porque una terminal real no es viable en un unit test.

**¿Qué es el ciclo AAA?**
Cada test tiene tres fases:
- **Arrange**: preparar el escenario (crear objetos, configurar dobles).
- **Act**: ejecutar la acción bajo prueba (una sola llamada).
- **Assert**: verificar el resultado esperado.

Separar las fases hace que un test se lea como una historia.

### Qué es

- **xUnit**: framework que descubre los tests (métodos con `[Fact]`) y los corre.
- **Moq**: crea dobles de dependencias.
- **FluentAssertions**: aserciones encadenadas y legibles: `valor.Should().Be(10)`.

### Qué problema resuelve

Correr verificaciones rápidas y repetibles sobre el código, con errores claros, sin
levantar la app completa. Es la red de seguridad que permite refactorizar.

### Cómo funciona paso a paso

1. xUnit descubre cada método marcado `[Fact]`.
2. Crea una instancia nueva de la clase de test por método (aislamiento entre tests).
3. Ejecuta el cuerpo: Arrange → Act → Assert.
4. Si una aserción de FluentAssertions falla, lanza una excepción con el detalle
   (esperado vs obtenido) y xUnit reporta el test como fallido.
5. El test runner devuelve el resumen (pasan/fallan).

### Qué se rompería sin esto en FocusBlock

Los 13 tests del repo (6 de Fase 1 + 7 de Fase 2) son la definición de "terminado". Sin
tests no habría forma de verificar el layout declarado sin abrir una terminal, ni el
round-trip de config sin un archivo real. La fase de TDD (RED → GREEN → REFACTOR) no
existiría.

### Para qué sirve en este proyecto

Los tests de Fase 1 verifican: orquestación (`FocusBlockApp`), estructura de `MainWindow`,
navegación (`ShowView`), `StatusView`, `BlockListView` y `AddBlockView`. Cada uno sigue
AAA. El patrón de "mirillas" (`StatusText`, `Apps`, `AppNameField`) permite verificar sin
romper encapsulación.

### Cómo se usa (código real)

```cs
// tests/FocusBlock.Tests.Unit/FocusBlockAppTests.cs:14-21
[Fact]
public void FocusBlockApp_CreatesMainWindow()
{
    // Arrange
    var app = new FocusBlockApp(Mock.Of<IApplication>());

    // Act + Assert
    app.MainWindow.Should().NotBeNull();
    app.MainWindow.Should().BeOfType<MainWindow>();
}

// tests/FocusBlock.Tests.Unit/StatusViewTests.cs:10-19 — otra verificación
[Fact]
public void StatusView_DisplaysDaemonStatus()
{
    var view = new StatusView();
    view.RefreshStatus(new DaemonStatus(true, TimeSpan.FromMinutes(5), 2));
    view.StatusText.Should().Contain("running");
    view.StatusText.Should().Contain("Active blocks: 2");
}
```

### Error común

Mockear de más: si `StatusViewTests` mockeara algo, dejaría de ser un test puro de
presentación. Detección: un test que configura varios mocks suele estar probando la
*colaboración* en vez de la *unidad*. Otra trampa: no saber **qué no prueba** el test.
`MainWindow_MenuNavigatesToViews` llama `ShowView` directamente; verifica el mecanismo de
navegación, **no** el wiring de `MenuItem.Action → ShowView`. Ese cableado solo se
verificaría con un test de integración o de UI.

### Para profundizar

- [`docs/adr/ADR-008-testing-stack.md`](../adr/ADR-008-testing-stack.md)
- [xUnit](https://xunit.net/) · [Moq](https://github.com/devlooped/moq) ·
  [FluentAssertions](https://fluentassertions.com/)
- Tests: `tests/FocusBlock.Tests.Unit/*.cs`.

---

## Relación entre estos conceptos

Terminal.Gui v2 define CÓMO se compone la UI (árbol de vistas con `View`/`Window`), el
layout (`Pos`/`Dim`) y la navegación (`ShowView`, con z-order e idempotencia). La
testabilidad es otro eje: DI + Composition Root resuelve el grafo de dependencias sin
`new` disperso; Container/Presentational separa orquestar de mostrar; xUnit + Moq +
FluentAssertions lo verifica con tests aislados (AAA). Los `record` y
`ObservableCollection` son los vehículos de datos entre la orquestación y las vistas: el
primero transporta valores inmutables comparables por contenido; la segunda notifica
cambios (Observer) para que la UI viva. El orden importa: sin API por instancia no hay
inyección; sin inyección no hay tests aislados; sin tests no hay confianza para refactorizar.

---

## Convención

- **Archivo**: `docs/learning/phase-NN-name.md`, uno por fase.
- **Título**: `# Fase NN — Tema`.
- **Código**: bloques `cs`; líneas de ~100 caracteres.
- **Secciones por concepto**: ver `docs/learning/template-phase.md`.
