# Plan de Desarrollo

Estrategia de testing, Docker y checklist de deployment.

---

## Estrategia de Testing — Pirámide

```
        ╱╲
       ╱  ╲      Funcional (Docker compose)
      ╱    ╲     — Tests de sistema completo
     ╱──────╲    — Ciclo de vida del daemon, E2E blocking
    ╱        ╲   — Solo flujos críticos
   ╱──────────╲
  ╱            ╲  Integración (TestContainers)
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
│  5. FUNCIONAL ── Test E2E con Docker solo si crítico     │
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

```csharp
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
├── FocusBlock.Tests.Unit/
│   ├── Services/
│   │   ├── BlockServiceTests.cs
│   │   ├── ProcessMonitorTests.cs
│   │   └── RuleEngineTests.cs
│   └── Models/
│       └── BlockRuleTests.cs
│
└── FocusBlock.Tests.Integration/    # Agregar cuando se necesite
    ├── FocusBlockFixture.cs
    └── IpcIntegrationTests.cs
```

---

## Cuándo Escribir Cada Tipo de Test

| Feature | Unitario | Integración | Funcional |
|---------|----------|-------------|-----------|
| Carga/guardado config | ✅ JSON round-trip | ✅ Archivo real | ❌ |
| Reglas de bloqueo | ✅ Todas las rutas lógicas | ❌ | ❌ |
| Monitor de procesos | ✅ Evaluación de horario | ✅ Lectura /proc real | ✅ Daemon en Docker |
| Protocolo IPC | ✅ Serialización mensajes | ✅ Socket real | ✅ Daemon completo |
| Anti-bypass | ✅ Hashing contraseñas | ✅ chattr +i | ✅ Flujo completo |
| Métricas | ✅ Queries SQLite | ✅ DB real | ✅ Pipeline completo |

---

## Docker

### Dockerfile.daemon (Multi-stage Build)

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/FocusBlock.Daemon/FocusBlock.Daemon.csproj", "FocusBlock.Daemon/"]
COPY ["src/FocusBlock.Contracts/FocusBlock.Contracts.csproj", "FocusBlock.Contracts/"]
RUN dotnet restore "FocusBlock.Daemon/FocusBlock.Daemon.csproj"

COPY . .
WORKDIR "/src/FocusBlock.Daemon"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

RUN useradd -m focusblock
USER focusblock

ENTRYPOINT ["dotnet", "FocusBlock.Daemon.dll"]
```

### Dockerfile.dev (Hot-Reload)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src

COPY ["src/FocusBlock.Daemon/FocusBlock.Daemon.csproj", "FocusBlock.Daemon/"]
COPY ["src/FocusBlock.Contracts/FocusBlock.Contracts.csproj", "FocusBlock.Contracts/"]
RUN dotnet restore

COPY . .

ENTRYPOINT ["dotnet", "watch", "run", "--project", "FocusBlock.Daemon"]
```

### docker-compose.yml

```yaml
version: '3.8'

services:
  daemon:
    build:
      context: .
      dockerfile: config/docker/Dockerfile.daemon
    container_name: focusblock-daemon
    restart: unless-stopped
    volumes:
      - focusblock-data:/app/data
      - /proc:/host/proc:ro
    environment:
      - DOTNET_ENVIRONMENT=Production
      - FocusBlock__DataPath=/app/data
    networks:
      - focusblock-network

  daemon-dev:
    build:
      context: .
      dockerfile: config/docker/Dockerfile.dev
    container_name: focusblock-daemon-dev
    volumes:
      - ./src:/src
      - focusblock-data:/app/data
    environment:
      - DOTNET_ENVIRONMENT=Development
    networks:
      - focusblock-network

volumes:
  focusblock-data:

networks:
  focusblock-network:
    driver: bridge
```

### Uso de Docker por Fase

| Fase | Docker | Por qué |
|------|--------|---------|
| 0-2 | ❌ | Desarrollo local, aprendizaje |
| 3 | ✅ `docker-compose up daemon` | Probar daemon en entorno Linux aislado |
| 4-5 | ✅ `docker-compose up daemon-dev` | Probar bloqueo con hot-reload |
| 6 | ✅ Tests de integración | SQLite en contenedor |
| 7 | ✅ Compose completo | Testing final, preparación deployment |

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
