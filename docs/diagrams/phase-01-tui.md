# Diagramas — Fase 1: Esqueleto TUI

Diagramas de componentes y navegación de la Fase 1. Renderizan en GitHub (Mermaid).

## 1. Estructura y conexiones de archivos

Quién depende de quién, y dónde vive cada patrón. Los tests (abajo) apuntan a las mismas clases que usa la producción — esa es la base de la testabilidad.

```mermaid
graph TD
    subgraph Producción
        PC["Program.cs · Composition Root"] -->|"new FocusBlockApp"| FA["FocusBlockApp · Container + DI"]
        FA -->|"new MainWindow"| MW["MainWindow : Window"]
        MW --> MB["MenuBar"]
        MW --> SB["StatusBar"]
        MW --> C["Content : View"]
        C --> SV["StatusView"]
        C --> BLV["BlockListView"]
        C --> ABV["AddBlockView"]
    end
    subgraph Tests
        T1["FocusBlockAppTests"] -->|"Moq · IApplication fake"| FA
        T2["MainWindowTests"] --> MW
        T3["StatusViewTests"] --> SV
        T4["BlockListViewTests"] --> BLV
        T5["AddBlockViewTests"] --> ABV
    end
```

**Patrones marcados:** Composition Root (`Program.cs`) · DI (`FocusBlockApp(IApplication)`) · Container/Presentational (`FocusBlockApp` orquesta, las vistas presentan) · Test Doubles (Moq en los tests).

## 2. Flujo de navegación

Qué pasa cuando el usuario elige un ítem del menú: el `Action` (delegado) dispara `ShowView`, que intercambia el `Content`.

```mermaid
sequenceDiagram
    participant U as Usuario
    participant M as MenuItem
    participant W as MainWindow
    U->>M: elige "Block → List"
    M->>W: invoca Action = () => ShowView(BlockListView)
    W->>W: Remove(Content) → Content = view → Add(view)
    W-->>U: muestra BlockListView
```

**Clave:** el `Action` es el *disparador* (qué pasa al hacer clic); `ShowView` es la *operación* (cómo se hace el cambio).