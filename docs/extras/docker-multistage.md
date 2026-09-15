# Extra: Docker multi-stage

> **Extra opcional.** Este tema NO pertenece a ninguna fase y no bloquea el avance del proyecto.
> Volver al índice de extras → `docs/extras/README.md`

Docker multi-stage es una técnica valiosa de build: separar la imagen que compila (SDK) de la imagen que
ejecuta (runtime slim), para obtener artefactos pequeños y reproducibles. **Podés construir la imagen del
daemon y aprender multi-stage**, pero el daemon **NO corre en el contenedor** en FocusBlock.

## Por qué el daemon NO corre en un contenedor

El daemon de FocusBlock depende de features del **kernel del host**:

| Feature | Qué necesita | Por qué el contenedor no sirve |
|---------|--------------|-------------------------------|
| Escaneo de `/proc` | Ver los procesos del **host** | El contenedor aísla el PID namespace: solo ve sus propios procesos |
| Señales (`kill`) | Enviar SIGTERM/SIGKILL a PIDs del host | Los PIDs dentro del contenedor no mapean a los del host |
| `chattr +i` | Marcar archivos inmutables vía ioctl | Requiere privilegios reales y aplica sobre el FS del host |
| systemd | Arrancar como servicio nativo | Un contenedor no es el init del sistema |

Un contenedor comparte el kernel pero aísla el PID namespace. Para que el daemon viera los procesos del
host habría que combinarlo con `pid: host` **y** `privileged`, lo que rompe el aislamiento sin aportar nada
(el daemon necesita root igual). La decisión está en `docs/adr/ADR-009-testing-seams.md`.

**`docker-compose up daemon` NO es una forma soportada de ejecutar FocusBlock.** El daemon se despliega de
forma nativa con systemd (`docs/adr/ADR-007-systemd.md`).

## Dockerfile.daemon (Multi-stage build)

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

## Dockerfile.dev (Hot-Reload)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src

COPY ["src/FocusBlock.Daemon/FocusBlock.Daemon.csproj", "FocusBlock.Daemon/"]
COPY ["src/FocusBlock.Contracts/FocusBlock.Contracts.csproj", "FocusBlock.Contracts/"]
RUN dotnet restore

COPY . .

ENTRYPOINT ["dotnet", "watch", "run", "--project", "FocusBlock.Daemon"]
```

## docker-compose.yml

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

## Qué podés aprender acá

- **Multi-stage build**: `FROM ... AS build` + `COPY --from=build` para no arrastrar el SDK a producción.
- **Capas y caché**: copiar primero los `.csproj` y restaurar antes de copiar el código acelera rebuilds.
- **Imágenes mínimas**: `sdk` para compilar, `aspnet`/runtime para ejecutar.
- **Compose**: orquestar servicios, volúmenes y redes para desarrollo local.
- **Por qué el aislamiento falla**: entender PID namespace y `privileged` es la lección de sistemas más
  valiosa de este extra.

> Este material se conserva como aprendizaje. La decisión vigente sobre runtime y testing vive en
> `docs/adr/ADR-009-testing-seams.md`.
