# Fase 00 — Setup: solución, TUI y driver

> Qué construye: la solución .NET, el primer Hello World en terminal y la infraestructura
> de tests.
> Después: [Fase 01](phase-01-tui.md) · [Glosario](glossary.md)

## Dónde estamos

No existe código. El objetivo es una ventana TUI que se dibuje y un `dotnet test` que
corra, con la estructura mínima para empezar.

```text
Terminal (grilla de celdas)
   ▲  secuencias de escape
Driver DOTNET
   ▲
Terminal.Gui (IApplication)
   ▲
FocusBlockApp ── MainWindow ── Label
   ▲
dotnet run ── FocusBlock.slnx ── 4 proyectos
```

## Qué construye esta fase

Una solución `.slnx`, un proyecto TUI con Terminal.Gui v2, un proyecto de tests con
xUnit + Moq + FluentAssertions y un Hello World verificado en Linux. Al cerrar,
`dotnet build` y `dotnet test` funcionan desde la raíz.

## El recorrido de una acción

1. Ejecutas `dotnet run --project src/FocusBlock.Tui`.
2. El SDK lee `FocusBlock.slnx` y compila los proyectos en orden de dependencias.
3. `Program.cs` crea una aplicación con `Application.Create()`.
4. `FocusBlockApp` recibe esa aplicación y arma `MainWindow`.
5. `Run()` llama a `Init(DOTNET)` y entra en el bucle de eventos.
6. El driver traduce la ventana a secuencias de escape y la terminal la dibuja.

## Los conceptos

| # | Concepto | Idea en una línea | Código |
|---|---|---|---|
| 1 | API por instancia | La app es un objeto que se inyecta | `App.cs` |
| 2 | Solución `.slnx` | Un archivo lista los proyectos | `FocusBlock.slnx` |
| 3 | Driver DOTNET | El driver ANSI de Linux falla | `App.cs` |
| 4 | Case-sensitivity | `Y` y `y` son identificadores distintos | `StatusView.cs` |

---

## 1. API por instancia

**Idea.** En Terminal.Gui v2 la aplicación es un objeto (`IApplication`) que creas e
inyectas. El estado vive en ese objeto, no en la clase. Sin esto, el test abriría una
terminal real.

**Analogía.** La API estática es el altavoz del edificio: todos hablan por el mismo canal.
La API por instancia es tu propia radio: cada uno tiene la suya y la apaga cuando quiere.

**Cómo funciona.** 1) `Application.Create()` construye el objeto con su estado. 2)
`Init(driver)` prepara la terminal y la pantalla alterna. 3) `Run(window)` ejecuta el bucle
de eventos. 4) `RequestStop()` o `Esc` lo corta y revierte `Init`.

```cs
// src/FocusBlock.Tui/Program.cs
FocusBlockApp app = new(Application.Create());
app.Run();

// src/FocusBlock.Tui/App.cs
public FocusBlockApp(IApplication app)
{
    _app = app;
    MainWindow = new MainWindow(OnEarlyStopAsync);   // callback de la fase 04
}

public void Run()
{
    _app.Init(DriverRegistry.Names.DOTNET);
    _app.Run(MainWindow);
}
```

**Trampa.** Copiar código de v1: `Application.Init()` estático o `Label("texto")` produce
`CS0246` o `CS0117`. Verifica el API contra la versión instalada.

<details><summary>Verifica</summary>

1. ¿Dónde vive el estado de la UI en v2?
2. ¿Por qué el test puede pasar `Mock.Of<IApplication>()` sin abrir terminal?
3. ¿Qué hace `Run()` y qué lo detiene?

</details>

---

## 2. Solución `.slnx`

**Idea.** Un archivo XML que lista los proyectos del repositorio para compilarlos y
testearlos con un solo comando.

**Analogía.** El proyecto es el ladrillo; la solución es la lista de materiales. Puedes
fabricar ladrillos sueltos, pero la lista te deja construir la casa de una vez.

**Cómo funciona.** 1) `dotnet new sln -n FocusBlock` crea el archivo. 2) `dotnet sln add`
agrega cada proyecto. 3) `dotnet build` compila todo lo listado. 4) Las carpetas `<Folder>`
son agrupación visual.

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/FocusBlock.Contracts/FocusBlock.Contracts.csproj" />
    <Project Path="src/FocusBlock.Core/FocusBlock.Core.csproj" />  <!-- fase 04 -->
    <Project Path="src/FocusBlock.Daemon/FocusBlock.Daemon.csproj" />
    <Project Path="src/FocusBlock.Tui/FocusBlock.Tui.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/FocusBlock.Tests.Unit/FocusBlock.Tests.Unit.csproj" />
  </Folder>
</Solution>
```

**Trampa.** Buscar un `FocusBlock.sln` clásico: en .NET 10 el SDK genera `.slnx` por
defecto. Si una herramienta no lo reconoce, pásale los `.csproj`.

<details><summary>Verifica</summary>

1. ¿Qué diferencia hay entre proyecto y solución?
2. ¿Qué pasa si agregas un proyecto y olvidas incluirlo en el `.slnx`?
3. ¿Para qué sirven las carpetas `<Folder>` si no afectan el build?

</details>

---

## 3. Driver DOTNET

**Idea.** Terminal.Gui dibuja a través de un driver. En Linux, el driver ANSI de la versión
2.4.17 tiene un bug y el proyecto fuerza el driver `DOTNET`.

**Analogía.** Es el intérprete entre el director y los músicos. Si un intérprete se
equivoca, cambias de intérprete, no la partitura: tu UI puede estar bien.

**Cómo funciona.** 1) `Init(...DOTNET)` resuelve la fábrica de ese driver. 2) El driver
emite las secuencias de escape al dibujar. 3) El resto del código no cambia. 4) Es un
workaround: se revisa cuando el bug se corrija upstream.

```cs
// src/FocusBlock.Tui/App.cs
public void Run()
{
    _app.Init(DriverRegistry.Names.DOTNET);   // ANSI roto en Linux
    _app.Run(MainWindow);
}
```

**Trampa.** Culpar al layout cuando la ventana sale vacía. Si compila y no se ven las
subviews, sospecha del driver. Los tests no lo detectan porque no abren terminal.

<details><summary>Verifica</summary>

1. ¿Qué capa convierte la UI en bytes?
2. ¿Por qué el bug no lo detectan los tests unitarios?
3. ¿Qué se pierde al usar el driver DOTNET y por qué se acepta?

</details>

---

## 4. Case-sensitivity

**Idea.** C# compara identificadores carácter por carácter. `Y` y `y` son nombres
distintos, aunque se parezcan. Aplica a toda la API de la librería.

**Analogía.** Como los nombres de archivo en Linux: `Foto.jpg` y `foto.jpg` pueden convivir.
En Windows, en cambio, el sistema los trata como el mismo archivo.

**Cómo funciona.** 1) Escribes `label.y`. 2) El compilador busca exactamente `y`. 3) No
existe (el miembro es `Y`) y emite `CS0103`. 4) Corriges la grafía y compila.

```cs
// src/FocusBlock.Tui/Views/StatusView.cs
_statusLabel = new Label
{
    Text = "Daemon: unknown",
    X = 0,
    Y = 0,          // 'Y' mayúscula; 'y' daría CS0103
};
Add(_statusLabel);
```

**Trampa.** Tipear de memoria y confiar en el autocompletado. `CS0103` en un nombre que
"parece existir" casi siempre es un problema de mayúsculas.

<details><summary>Verifica</summary>

1. ¿Qué significa que C# sea case-sensitive?
2. ¿Qué error produce `label.y` y por qué?
3. ¿Por qué la convención .NET usa PascalCase en miembros públicos?

</details>

---

## Práctica

### Mezcla

1. Si eliminas `Init()` y llamas `Run()`, ¿qué falla y en qué capa?
2. ¿Por qué el test de la app no necesita una terminal real?
3. ¿Qué ventaja tiene `.slnx` sobre enumerar `.csproj` en cada comando?
4. ¿Qué capa dibuja un `Label` que agregas a `MainWindow`?

<details><summary>Respuestas</summary>
1. La terminal no queda preparada: falla la infraestructura.
2. Depende de `IApplication`, una interfaz; el test inyecta un doble.
3. Un único punto de entrada, legible y versionable.
4. El driver de rendering, no el `Label`.
</details>

### En código

1. Cambia `Init` al driver ANSI y observa la ventana. Explica por qué los tests siguen
   verdes.
2. Escribe el test RED de la fase siguiente: `FocusBlockApp_CreatesMainWindow` usando
   `Mock.Of<IApplication>()`.

### Antes de cerrar

- [ ] Puedo explicar qué es `IApplication` y por qué se inyecta.
- [ ] Sé qué hace cada línea de `Program.cs` + `App.Run()`.
- [ ] Puedo reproducir la ventana vacía y explicar su causa.

## Trampas reales de la fase

- **API v1 contra v2**: `CS0246` y `CS0117` por namespaces movidos y `Label` sin texto en
  el constructor.
- **Ventana vacía**: el driver ANSI no dibujaba las subviews; se forzó `DOTNET`.
- **`y` minúscula**: la propiedad es `Y`; el compilador no adivina.

## Para profundizar

- [`ADR-001`](../adr/ADR-001-terminal-gui.md) — por qué Terminal.Gui v2.
- [`ADR-011`](../adr/ADR-011-dotnet-driver.md) — el workaround del driver.
- [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui) — repositorio oficial.
