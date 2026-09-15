# Plan de Desarrollo

Estrategia de testing, puntos de inyección y checklist de deployment.

---

## Estrategia de Testing — Pirámide

```
        ╱╲
       ╱  ╲      Funcional (host)
      ╱    ╲     — Tests de sistema completo
     ╱──────╲    — Ciclo de vida del daemon, E2E blocking
    ╱        ╲   — Solo flujos críticos
   ╱──────────╲
  ╱            ╲  Integración (host real, categorías)
 ╱              ╲ — Cliente/servidor IPC
╱────────────────╲— Operaciones SQLite
╱                  ╲
──────────────────── Unitarios (xUnit + Moq + FluentAssertions)
                    — BlockEngine, CooldownManager, AuthService
                    — Muchos, rápidos, aislados
```

---

## Ciclo TDD por Feature

Cada feature se desarrolla así:

```
┌─────────────────────────────────────────────────────────┐
│                    CICLO TDD                              │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  1. RED ──────── Escribir test que falla                 │
│       │         (define comportamiento esperado)         │
│       ▼                                                  │
│  2. GREEN ────── Escribir código mínimo para pasar       │
│       │         (implementar funcionalidad)              │
│       ▼                                                  │
│  3. REFACTOR ─── Limpiar código sin cambiar comportamiento│
│       │         (mejorar estructura)                     │
│       ▼                                                  │
│  4. INTEGRACIÓN ── Test de integración si toca límites   │
│       │           externos (SQLite, IPC, /proc)          │
│       ▼                                                  │
│  5. FUNCIONAL ── Test E2E en el host solo si crítico     │
│       │                                                  │
│       ▼                                                  │
│  6. REPETIR ──── Volver al paso 1 con siguiente feature  │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

**Reglas:**
- ANTES de escribir implementación → escribir el test
- Si el test falla → está bien, es RED
- Escribir solo el código necesario para que pase → GREEN
- Una vez que pasa → refactorizar
- NUNCA escribir implementación sin test previo

---

## Convención de Nombres de Tests

```cs
// Patrón: Method_Condition_ExpectedResult

[Fact]
public void BlockRule_WhenActive_ReturnsBlocked()

[Theory]
[InlineData("firefox", true)]
[InlineData("unknown-app", false)]
public void BlockRule_IsAppBlocked_ReturnsCorrectResult(string appName, bool expected)
```

---

## Organización de Archivos de Test

```
tests/
├── FocusBlock.Tests.Unit/           # Layout plano: un archivo por clase bajo test
│   ├── AppConfigTests.cs
│   ├── AuthServiceTests.cs
│   ├── ConfigServiceTests.cs
│   ├── ProcessMonitorTests.cs
│   ├── WorkerTests.cs
│   └── ...
└── FocusBlock.Tests.Integration/    # Futuro: tests de host real (categoría Integration)
    └── IpcIntegrationTests.cs
```

---

## Cuándo Escribir Cada Tipo de Test

| Feature | Unitario | Integración | Funcional |
|---------|----------|-------------|-----------|
| Carga/guardado config | ✅ JSON round-trip | ✅ Archivo real | ❌ |
| Reglas de bloqueo | ✅ Todas las rutas lógicas | ❌ | ❌ |
| Monitor de procesos | ✅ Parseo de `/proc` con fuente fake | ✅ Lectura /proc real | ✅ Daemon en host |
| Protocolo IPC | ✅ Serialización mensajes | ✅ Socket real | ✅ Daemon completo |
| Anti-bypass | ✅ Hashing contraseñas | ✅ chattr +i | ✅ Flujo completo |
| Métricas | ✅ Queries SQLite | ✅ DB real | ✅ Pipeline completo |

---

## Puntos de inyección (seams)

Las dependencias del sistema operativo (filesystem, `/proc`, señales, `chattr`, reloj, sockets, SQLite) **no deben filtrarse a los tests unitarios**. Se aíslan detrás de un **punto de inyección (seam)**: una interfaz o parámetro que en producción usa la implementación real y en los tests se reemplaza por un **doble** (fake). El comportamiento que toca el kernel de verdad se cubre con tests de **integración en el host**.

| Componente | Dependencia del SO | Punto de inyección (seam) | Doble para unit | Test real de integración |
|------------|--------------------|----------------------------|-----------------|--------------------------|
| `ConfigService` | Filesystem | ruta inyectada por constructor | ruta temporal | archivo real |
| `AuthService` | CSPRNG | (sin seam: el salt se inyecta/verifica; salida determinista dada la sal) | salt fijo | — |
| `ProcessMonitor` | `/proc` | `IProcessSource` (enumerar + leer) | fuente fake en memoria | `/proc` real |
| `BlockEnforcer` | syscall `kill()` | `ISignalSender` | sender fake que registra señales | proceso dummy (`sleep`) |
| `IpcServer` | Unix socket | ruta del socket inyectada | path temporal | cliente/servidor real |
| `BlockEngine` | reloj | `TimeProvider` | `FakeTimeProvider` | — |
| `CooldownManager` | reloj | `TimeProvider` | `FakeTimeProvider` | — |
| `FileProtector` | `chattr +i` (ioctl) | `IFileAttributes` | fake que registra flags | FS real con root |
| `MetricsCollector` | SQLite | connection string / path | SQLite temporal | archivo real |
| `IpcClient` | socket | ruta + socket | servidor fake | daemon real |

> Los tests de integración están **etiquetados por categoría** (`Category=Integration`) y se **saltan** cuando faltan privilegios (por ejemplo, `chattr +i` sin root).

> **Extra opcional:** Docker multi-stage vive en `docs/extras/docker-multistage.md` como material de aprendizaje. No es el runtime del daemon ni una estrategia de testing.

---

## Checklist de Deployment

```bash
# Build release
dotnet publish -c Release -r linux-x64 --self-contained

# Instalar daemon
sudo cp src/FocusBlock.Daemon/bin/Release/net10.0/linux-x64/publish/FocusBlock.Daemon /opt/focusblock/
sudo cp config/focusblock-daemon.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now focusblock

# Instalar TUI
sudo cp src/FocusBlock.Tui/bin/Release/net10.0/linux-x64/publish/FocusBlock.Tui /usr/local/bin/

# Crear directorios requeridos
sudo mkdir -p /etc/focusblock /var/lib/focusblock /run/focusblock
sudo cp config/focusblock.json /etc/focusblock/
sudo chown -R root:root /etc/focusblock /var/lib/focusblock

# Iniciar
sudo systemctl start focusblock
focusblock  # ejecutar TUI como usuario regular
```
