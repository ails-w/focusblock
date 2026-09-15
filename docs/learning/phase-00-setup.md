# Fase 00 — Setup: Fundamentos del Proyecto

> Qué se aprende: por qué una TUI .NET se organiza como una solución con proyectos y cómo
> se conecta la librería Terminal.Gui con la terminal real.
> Log de la fase (tareas, decisiones, problemas) → `docs/progress-log/phase-00-setup.md`
> Fase siguiente → `docs/learning/phase-01-tui.md`

## Glosario de la fase

| Término | Qué significa (en una línea) |
|---|---|
| TUI | Interfaz de usuario dibujada con texto dentro de una terminal. |
| GUI | Interfaz gráfica con ventanas y pixeles; necesita un servidor gráfico. |
| Terminal | Programa que mantiene una grilla de celdas y dibuja texto según los bytes que recibe. |
| Secuencia de escape | Bytes que empiezan con `ESC` y dan órdenes a la terminal. |
| Driver de rendering | Capa que traduce las vistas a secuencias de escape de una terminal. |
| Alternate screen buffer | Segunda pantalla de la terminal, para apps de pantalla completa. |
| .NET SDK | Paquete de herramientas `dotnet` para compilar, testear y ejecutar proyectos .NET. |
| Solución | Archivo que agrupa varios proyectos para operarlos juntos (`FocusBlock.slnx`). |
| Proyecto | Unidad de compilación con su `.csproj`; produce un `.dll` o `.exe`. |
| API | Superficie pública con la que tu código habla con una librería. |
| API estática | Se invoca sobre la clase (`Clase.Metodo()`); su estado es global. |
| API por instancia | Se invoca sobre un objeto (`objeto.Metodo()`); estado por instancia. |
| Namespace | Espacio de nombres que agrupa tipos y evita colisiones. |
| Case-sensitive | Distingue mayúsculas de minúsculas: `Y` y `y` son identificadores distintos. |

## Mapa de conceptos

```text
.NET SDK
  └── Solución (.slnx) ──▶ Proyectos (Tui, Contracts, Daemon, Tests)

Terminal.Gui v2 (API por instancia)
  ├── se escribe en C# (case-sensitive)
  └── dibuja a través de un Driver de rendering
        └── en Linux: driver ANSI (roto) → driver DOTNET (workaround)
```

---

## Terminal.Gui v2 — API por instancia

### En una frase

Terminal.Gui te deja construir interfaces de texto, y en v2 la aplicación es un
**objeto** que vos creás y controlás en lugar de una caja global compartida.

### Fundamentos previos

**¿Qué es una TUI y qué la diferencia de una GUI?**
Una GUI (Graphical User Interface) dibuja ventanas, botones y pixeles a través de un
servidor gráfico (X11, Wayland, Windows). Una TUI (Terminal User Interface) se dibuja
*dentro de una terminal*, usando una grilla de caracteres: por ejemplo 80 columnas por
24 filas. Cada celda tiene un carácter, un color de frente y un color de fondo. Una TUI
no necesita servidor gráfico, funciona por SSH y es rápida; a cambio, está limitada a
caracteres y pierde precisión de layout frente a los pixeles.

**¿Qué es una terminal?**
Históricamente era un dispositivo físico (un teletipo). Hoy es un *emulador*: kitty,
alacritty, GNOME Terminal. La terminal mantiene una grilla de celdas y entiende un flujo
de bytes. Los bytes imprimibles se escriben en la celda actual; otros bytes son órdenes.
Tu programa no "pinta pixeles": escribe bytes y confía en que la terminal los interprete.

**¿Qué es una secuencia de escape?**
Es un conjunto de bytes que empieza con el carácter `ESC` (código 27, escrito `\x1b` o
`\e`) y sigue con parámetros. Ejemplos reales: `\x1b[2J` limpia la pantalla, `\x1b[H`
mueve el cursor al inicio, `\x1b[31m` pone el texto en rojo. Cuando una app TUI "dibuja",
en realidad emite cientos de estas secuencias. Nunca las escribís a mano: las emite el
driver.

**¿Qué es un driver de rendering?**
Es la capa que traduce el estado deseado de la UI (qué texto va en qué celda) a las
secuencias de escape correctas para la terminal concreta. Distintas terminales soportan
distintas capacidades (true color, protocolo de teclado Kitty). Terminal.Gui trae varios
drivers: `ANSI`, `DOTNET`, `WINDOWS`.

**¿Qué es el alternate screen buffer?**
Las terminales modernas mantienen dos pantallas lógicas. La *normal* conserva tu
historial de comandos y scrollback; la *alterna* es una pantalla limpia y separada. Las
apps de pantalla completa (`vim`, `less`, una TUI) cambian a la alterna al arrancar
(secuencias `\x1b[?1049h` y `\x1b[?1049l`). Por eso, al cerrar la app, recuperás el
prompt tal como estaba. Terminal.Gui hace este cambio por vos cuando corrés una ventana.

**¿Qué es una API y qué diferencia hay entre estática y por instancia?**
Una API es la superficie pública de una librería. Hay dos estilos de diseño:

- **API estática**: se llama sobre la clase, como `Application.Init()`. Todo el estado
  vive en un único lugar global (campos estáticos). Ventaja: escribir poco. Problemas:
  dos usos en el mismo proceso se pisan, los tests comparten estado y dependen del orden,
  y el ciclo de vida queda oculto.
- **API por instancia**: se llama sobre un objeto, como `app.Init(...)`. Cada instancia
  lleva su propio estado. Ventaja: se puede crear, inyectar, reemplazar por un doble de
  test y descartar. Es la base de la testabilidad de la Fase 1.

Terminal.Gui v1 usaba API estática; v2 usa API por instancia (`IApplication`).

**¿Qué es un namespace y un sub-namespace?**
Un namespace agrupa tipos bajo un nombre con puntos. `Terminal.Gui` es el namespace raíz
de la librería; `Terminal.Gui.App`, `Terminal.Gui.Views` y `Terminal.Gui.ViewBase` son
sub-namespaces. En v2 los tipos se movieron del raíz a los sub-namespaces: por eso un
`using Terminal.Gui;` de v1 ya no encuentra `Label`. Confirmado en el repo: `App.cs:3-4`
importa `Terminal.Gui.App` y `Terminal.Gui.Drivers`; `MainWindow.cs:1-3` importa
`Terminal.Gui.Input`, `.ViewBase` y `.Views`.

### Qué es

Terminal.Gui es una librería TUI para .NET. En v2 el punto de entrada es
`Application.Create()`, que devuelve un objeto `IApplication`. Ese objeto tiene el
método `Init(driver)`, el método `Run(view)` y el método `RequestStop()`.

### Qué problema resuelve

Antes (API estática) había **un solo estado global** para toda la aplicación. No se
podían correr dos aplicaciones en el mismo proceso, ni aislar los tests, ni saber con
claridad cuándo arranca y termina el ciclo de vida. La API por instancia encapsula ese
estado en un objeto: vos decidís cuándo crearlo, cuándo iniciarlo y cuándo terminarlo.

### Cómo funciona paso a paso

1. `Application.Create()` construye un objeto `IApplication` con su propio estado.
2. `app.Init(driver)` prepara la terminal: detecta capacidades, entra al alternate
   screen buffer y deja listo el driver de rendering.
3. `app.Run(window)` entra en el bucle de eventos: lee teclado/ratón, recalcula layout,
   dibuja y repite hasta que algo pide detenerse.
4. `RequestStop()` (o una tecla configurada, como `Esc`) corta el bucle.
5. Al salir, `Init` se revierte: se restaura la pantalla normal y el cursor.

### Qué se rompería sin esto en FocusBlock

`FocusBlockApp` recibe `IApplication` por constructor (`App.cs:13`). Sin API por
instancia no habría un objeto que inyectar: el test
`FocusBlockAppTests.FocusBlockApp_CreatesMainWindow` no podría pasar
`Mock.Of<IApplication>()` y tendría que abrir una terminal real. La suite de la Fase 1
dejaría de ser unitaria y aislada.

### Para qué sirve en este proyecto

`Program.cs` crea la app real y la pasa a `FocusBlockApp`. `App.cs` la guarda en
`_app`, arma `MainWindow` y expone `Run()`. Toda la orquestación depende de la interfaz
`IApplication`, no de una implementación concreta: eso es lo que permite cambiarla por un
doble en tests.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Tui/Program.cs — Composition Root
FocusBlockApp app = new(Application.Create());
app.Run();

// src/FocusBlock.Tui/App.cs:13-22
public FocusBlockApp(IApplication app)
{
    _app = app;
    MainWindow = new MainWindow();
}

public void Run()
{
    _app.Init(DriverRegistry.Names.DOTNET);
    _app.Run(MainWindow);   // bloquea hasta que la ventana se cierra
}
```

### Error común

Escribir código de v1 contra v2: `Application.Init()` estático y `Label("texto")` por
constructor. En v2.4.17 eso produce `CS0246` (tipo no encontrado, porque cambió de
namespace) o `CS0117` (el miembro no existe con esa firma). Detección: el compilador
marca la línea; la causa no es tu lógica sino el cambio de API entre versiones mayores.
Regla práctica: antes de copiar un ejemplo, verificarlo contra la versión instalada.

### Para profundizar

- [`Terminal.Gui` en GitHub](https://github.com/gui-cs/Terminal.Gui)
- [`docs/adr/ADR-001-terminal-gui.md`](../adr/ADR-001-terminal-gui.md)
- Concepto siguiente: **Formato `.slnx`** y **Driver DOTNET** en este mismo archivo.
- `docs/progress-log/phase-00-setup.md` — decisión 3 (API por instancia).

---

## Formato `.slnx` en .NET 10

### En una frase

Una solución `.slnx` es un archivo XML legible que lista los proyectos del repo, para
que un solo comando los compile o testee a todos.

### Fundamentos previos

**¿Qué es el .NET SDK?**
El SDK (Software Development Kit) es el conjunto de herramientas que se instala con
`dotnet`: el compilador de C#, el sistema de build (MSBuild), las plantillas
(`dotnet new`) y los comandos `dotnet build`, `dotnet test`, `dotnet run`. El SDK es
*distinto* del runtime: el runtime ejecuta programas ya compilados; el SDK los compila.
En este entorno hay un único SDK: .NET 10.0.111.

**¿Qué es un proyecto?**
Un proyecto es la unidad mínima de compilación. Está descrito por un archivo `.csproj`
(XML) que declara el target framework, los paquetes NuGet y las referencias a otros
proyectos. Al compilar, produce un assembly: una librería (`.dll`) o un ejecutable
(`.exe`). FocusBlock tiene cuatro:

- `src/FocusBlock.Tui/FocusBlock.Tui.csproj` — ejecutable de la TUI.
- `src/FocusBlock.Contracts/FocusBlock.Contracts.csproj` — librería compartida.
- `src/FocusBlock.Daemon/FocusBlock.Daemon.csproj` — worker del daemon (estructura objetivo).
- `tests/FocusBlock.Tests.Unit/FocusBlock.Tests.Unit.csproj` — proyecto de tests.

**¿Qué es una solución?**
Una solución es un *agrupador lógico*: un archivo que lista proyectos para que las
herramientas los traten como un conjunto. No aporta código ni referencias; aporta
orquestación. Sin solución igual podés compilar cada `.csproj` por separado, pero perdés
el comando único y la vista de conjunto.

**Solución vs proyecto, en una analogía:** el proyecto es un ladrillo; la solución es la
lista de materiales. Podés fabricar ladrillos sueltos, pero la lista te deja construir
toda la casa de una vez.

### Qué es

`dotnet new sln` en .NET 10 genera `FocusBlock.slnx`, un archivo en formato XML. El
formato clásico `.sln` era texto con un formato propio más verboso; `.slnx` es el nuevo
formato por defecto del SDK 10.

### Qué problema resuelve

Antes había que enumerar a mano cada proyecto en cada comando, o mantener el archivo
`.sln` con sintaxis propietaria difícil de diffear. `.slnx` da un único punto de entrada
legible y versionable para compilar, testear y agregar proyectos.

### Cómo funciona paso a paso

1. `dotnet new sln -n FocusBlock` crea el archivo con la estructura `<Solution>`.
2. `dotnet sln add src/FocusBlock.Tui` agrega el `<Project Path="..."/>` dentro de la
   carpeta correspondiente.
3. Al correr `dotnet build` en la raíz, el SDK busca el `.slnx` y compila todos los
   proyectos listados en orden de dependencias.
4. Las carpetas `<Folder Name="/src/">` y `/tests/` son solo agrupación visual: no
   afectan el build.

### Qué se rompería sin esto en FocusBlock

Sin `FocusBlock.slnx` no habría un `dotnet build` ni un `dotnet test` a nivel raíz: cada
comando tendría que apuntar a un `.csproj` puntual. El proyecto de tests (que referencia
a `Tui`, `Contracts` y `Daemon`) seguiría funcionando, pero el flujo de trabajo del repo
sería manual y más propenso a olvidar un proyecto.

### Para qué sirve en este proyecto

`FocusBlock.slnx` es la raíz del monorepo: agrupa los tres proyectos de `src/` y el de
`tests/`. Es lo que hace que `dotnet build` y `dotnet test` desde la raíz cubran todo.

### Cómo se usa (código real)

```bash
dotnet new sln -n FocusBlock        # genera FocusBlock.slnx (formato XML)
dotnet sln add src/FocusBlock.Tui   # agrega un proyecto a la solución
dotnet build                        # compila todos los proyectos de la solución
dotnet test                         # corre todos los tests de la solución
```

El archivo real (`FocusBlock.slnx`) lista cuatro proyectos:

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/FocusBlock.Contracts/FocusBlock.Contracts.csproj" />
    <Project Path="src/FocusBlock.Daemon/FocusBlock.Daemon.csproj" />
    <Project Path="src/FocusBlock.Tui/FocusBlock.Tui.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/FocusBlock.Tests.Unit/FocusBlock.Tests.Unit.csproj" />
  </Folder>
</Solution>
```

### Error común

Buscar un `FocusBlock.sln` clásico y concluir que "no hay solución". En .NET 10 el SDK
genera `.slnx` por defecto. Detección: `ls *.slnx` en la raíz. Si una herramienta no
reconoce `.slnx`, hay que indicarle el formato o pasarle los `.csproj` directamente.

### Para profundizar

- [`dotnet sln` (Microsoft Learn)](https://learn.microsoft.com/dotnet/core/tools/dotnet-sln)
- [Formato `.slnx`](https://learn.microsoft.com/dotnet/core/tools/dotnet-sln#slnx)
- `docs/architecture.md` — mapa del repo.
- `docs/adr/ADR-010-dotnet-10-target.md`

---

## Driver DOTNET de Terminal.Gui en Linux

### En una frase

Terminal.Gui necesita un driver para dibujar; en Linux el driver ANSI tiene un bug y el
proyecto fuerza el driver `DOTNET` como workaround.

### Fundamentos previos

Este concepto reutiliza **terminal**, **secuencia de escape** y **driver de rendering**
del primer concepto. Repaso exprés: la terminal entiende bytes; el driver traduce la UI a
esos bytes; cada driver apunta a un conjunto de capacidades distinto.

**¿Qué significa "workaround"?**
Es una solución temporal que evita un problema ajeno a tu código. No arregla la causa (el
bug del driver), pero permite seguir trabajando. Todo workaround debe tener seguimiento:
si el bug se corrige upstream, tu código queda con una decisión innecesaria.

### Qué es

Un driver de rendering concreto para Linux/Unix. El driver ANSI emite secuencias ANSI
estándar; el driver DOTNET es otra implementación de la misma interfaz. En la versión
2.4.17, el driver ANSI por defecto en Linux tiene bugs upstream
([#4848](https://github.com/gui-cs/Terminal.Gui/issues/4848) y
[#4374](https://github.com/gui-cs/Terminal.Gui/issues/4374)): la pantalla queda garbled o
vacía, sin subviews.

### Qué problema resuelve

Evita un bug de la librería que no podemos corregir desde FocusBlock. Forzando el driver
`DOTNET` antes de `Run`, la ventana y sus vistas se renderizan correctamente en
Linux/Arch (incluido Zellij, el multiplexor que usa el usuario).

### Cómo funciona paso a paso

1. Al llamar `app.Init(DriverRegistry.Names.DOTNET)`, la app resuelve la *fábrica* del
   driver `DOTNET` en vez de la del driver por defecto.
2. Ese driver implementa la lectura de la terminal y la emisión de secuencias de escape
   sin pasar por el camino con bug del ANSI.
3. `app.Run(window)` ya opera con ese driver: al dibujar, el driver elegido produce la
   salida.
4. El resto del código no cambia: el driver es un detalle de infraestructura.

### Qué se rompería sin esto en FocusBlock

El Hello World de la Fase 0 mostraba una ventana vacía aunque el código fuera idéntico al
ejemplo oficial. Sin el workaround, ni `MainWindow` ni sus subviews (`StatusView`,
`BlockListView`, `AddBlockView`) se verían. Y como el bug es de rendering, los tests
unitarios seguirían en verde: no detectarían el problema, porque no abren una terminal.

### Para qué sirve en este proyecto

Es una línea en `FocusBlockApp.Run()` (`App.cs:21`) que condiciona todo lo visual.
Tradeoff aceptado en `ADR-011`: se pierden features avanzadas del driver ANSI
(negociación de capacidades, teclado Kitty, true color fino), que no hacen falta para
menús, listas y charts ASCII.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Tui/App.cs:19-23
public void Run()
{
    _app.Init(DriverRegistry.Names.DOTNET);   // workaround: driver ANSI roto en Linux
    _app.Run(MainWindow);
}
```

### Error común

Copiar el ejemplo oficial tal cual y culpar al layout cuando la ventana sale vacía. Otra
variante: intentar arreglarlo agregando o quitando `Add(...)` cuando el problema es el
driver, no la composición. Detección: si el código compila, el layout está declarado y la
ventana no dibuja subviews, sospechar del driver antes que de la UI. Ver
`ADR-011` para el seguimiento: revisar cuando el driver ANSI se estabilice.

### Para profundizar

- [`docs/adr/ADR-011-dotnet-driver.md`](../adr/ADR-011-dotnet-driver.md) — decisión y consecuencias.
- [Terminal.Gui issue #4848](https://github.com/gui-cs/Terminal.Gui/issues/4848)
- [Terminal.Gui issue #4374](https://github.com/gui-cs/Terminal.Gui/issues/4374)

---

## Case-sensitivity en C# (propiedades PascalCase)

### En una frase

En C#, `Y` y `y` son dos identificadores distintos; no es un capricho, es cómo el
lenguaje define lo que es un nombre.

### Fundamentos previos

**¿Qué es un identificador?**
Es el nombre con el que referenciás una variable, una clase, una propiedad o un método.
El compilador lo resuelve por *coincidencia exacta*: no adivina ni corrige.

**¿Qué significa case-sensitive?**
Significa que el lenguaje distingue mayúsculas de minúsculas al comparar nombres. En C#
`velocidad`, `Velocidad` y `VELOCIDAD` son tres símbolos distintos que podrían coexistir
en el mismo scope. Esto es distinto de un lenguaje *case-insensitive*, donde `y` e `Y`
se consideran el mismo nombre.

**¿Por qué C# es case-sensitive (y por qué no es un capricho)?**
Porque permite que un identificador local y un miembro público convivan con grafías
distintas sin colisionar. Ejemplo: dentro de un método podés tener una variable local
`y` y, al mismo tiempo, usar la propiedad `this.Y` de una vista. Si el lenguaje ignorara
el caso, ambos nombres chocarían. La contrapartida es que vos tenés que respetar la
grafía exacta que la librería definió. Por eso el ecosistema .NET usa convenciones:
`PascalCase` para miembros públicos (`Y`, `Width`), `camelCase` para locales (`width`),
`_camelCase` para campos privados (`_statusLabel`, visible en `StatusView.cs:10`).

### Qué es

Una regla léxica del lenguaje: los identificadores se comparan carácter por carácter,
incluyendo mayúsculas y minúsculas. Se combina con las convenciones de estilo de .NET
para que la *intención* (público vs local) sea legible de un vistazo.

### Qué problema resuelve

Evita colisiones de nombres entre scopes y hace explícita la distinción entre miembro
público y variable local. El costo es que hay que escribir la grafía exacta.

### Cómo funciona paso a paso

1. Escribís `label.y`.
2. El compilador busca un miembro llamado exactamente `y` en el tipo `Label`.
3. No lo encuentra (el miembro se llama `Y`) y emite `CS0103`: "el nombre 'y' no existe
   en el contexto actual".
4. Corregís a `label.Y` y compila.

### Qué se rompería sin esto en FocusBlock

El log de la Fase 0 registra el `Problema 3`: al usar Terminal.Gui, la propiedad de
posición es `Y` y escribir `y` no compilaba. Como C# es case-sensitive, ese error es
inmediato y visible en build time: no llega a producción. Además, si se hubiera
"arreglado" cambiando el nombre de un miembro propio a minúscula, habría roto la
convención PascalCase del proyecto y el tipado público.

### Para qué sirve en este proyecto

Aplica a todo el código: `Dim.Fill()` (no `dim.fill()`), `Pos.Right(...)` (no
`pos.right(...)`), `Content.Height` (no `Content.height`), `_statusLabel.Text` (no
`_statusLabel.text`). Es una regla transversal del lenguaje, no una particularidad de
Terminal.Gui.

### Cómo se usa (código real)

```cs
// src/FocusBlock.Tui/Views/StatusView.cs:14-22
_statusLabel = new Label
{
    Text = "Daemon: unknown",   // propiedad PascalCase
    X = 0,
    Y = 0,                      // 'Y' mayúscula; 'y' daría CS0103
};
Add(_statusLabel);
```

### Error común

Confiar en que el autocompletado "sabe lo que querés" y tipear de memoria. En C# el caso
importa siempre, incluso en miembros de librerías. Detección: `CS0103` en un nombre que
"parece existir" casi siempre es un problema de capitalización. Letra por letra es la
única forma de comparar: `Y` no es "lo mismo" que `y`.

### Para profundizar

- [Framework Design Guidelines — convenciones de nombres](https://learn.microsoft.com/dotnet/standard/design-guidelines/)
- `docs/progress-log/phase-00-setup.md` — Problema 3.
- `docs/learning/phase-01-tui.md` — mismos errores de API al componer vistas.

---

## Relación entre estos conceptos

El `SDK` es la herramienta que compila; la `solución (.slnx)` agrupa los proyectos que
ese SDK construye. Dentro de la TUI, `Terminal.Gui v2` define CÓMO se construye la UI
(API por instancia), y el `driver DOTNET` es el workaround que hace que esa UI se dibuje
en Linux. La `case-sensitivity` es la regla del lenguaje que aplica a todas las capas,
incluida la API de Terminal.Gui. El orden importa: sin SDK no hay build; sin solución no
hay build unificado; sin driver no hay render; sin respetar el caso no hay compilación.

---

## Convención

- **Archivo**: `docs/learning/phase-NN-name.md`, uno por fase.
- **Título**: `# Fase NN — Tema`.
- **Código**: bloques `cs`; líneas de ~100 caracteres.
- **Secciones por concepto**: ver `docs/learning/template-phase.md`.
