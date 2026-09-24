# Diagramas — Fase 4: Núcleo del Bloqueador

Los dos flujos que sostienen la fase: una pasada de bloqueo completa y el early stop cruzando la
frontera TUI ↔ daemon.

## 1. Una pasada de bloqueo (tick del Worker)

Qué pasa entre que el `Worker` despierta y el kernel recibe la señal.

```mermaid
sequenceDiagram
    participant W as Worker · tick 5 s
    participant BC as BlockCoordinator
    participant BE as BlockEngine
    participant PM as ProcessMonitor
    participant CD as CooldownManager
    participant EN as BlockEnforcer
    participant OS as Kernel

    W->>BC: RunOnceAsync(ct)
    BC->>BC: lee el reloj una vez (nowUtc + nowLocal)
    BC->>BE: GetAppsToBlock(config, nowLocal)
    BE-->>BC: ["firefox"]

    alt Nada para bloquear
        BC-->>W: early return · no toca /proc
    else Hay apps activas
        BC->>PM: GetProcesses().ToList()
        PM-->>BC: [(101, "firefox"), (202, "bash")]
        loop Por cada app activa
            BC->>CD: IsOnCooldown(app, nowUtc)
            CD-->>BC: false (true → skip)
            loop Por cada proceso que matchea
                BC->>EN: KillProcessAsync(pid, ct)
                EN->>OS: SIGTERM
                Note over EN: espera el período de gracia (2 s)
                EN->>OS: SIGKILL si sigue vivo
            end
        end
    end
```

**Clave:** el reloj se lee **una sola vez** por pasada: reglas y cooldowns se evalúan contra el mismo
instante. `/proc` también se lee una sola vez y se materializa con `.ToList()` (el escaneo es lazy).
El `HashSet` de PIDs matados evita señales duplicadas cuando dos reglas activas apuntan a la misma
app. Si no hay apps activas, el early return evita tocar `/proc` por completo.

**Patrones marcados:** orquestador delgado (`BlockCoordinator` sin política propia) · función pura
(`BlockEngine.Evaluate`) · colección concurrente (`CooldownManager`) · dedup de PIDs · escalación de
señales (`BlockEnforcer`).

## 2. Early stop: TUI ↔ daemon

El camino completo de la válvula de escape, desde el menú hasta el cooldown.

```mermaid
sequenceDiagram
    participant U as Usuario
    participant APP as FocusBlockApp
    participant ES as EarlyStopService
    participant CH as ChallengeSystem
    participant PR as TerminalEarlyStopPrompt
    participant IC as IpcClient
    participant IS as IpcServer
    participant DH as DaemonRequestHandler
    participant BE as BlockEngine
    participant AV as AuthService
    participant CD as CooldownManager

    U->>APP: menú _Early Stop
    APP->>U: AppNameDialog (modal)
    U-->>APP: "firefox"
    APP->>ES: RequestEarlyStopAsync("firefox")
    ES->>CH: GenerateChallenge()
    CH-->>ES: "cobalt-otter-lantern-drift"
    ES->>PR: AskForPassword(challenge)
    PR->>U: ChallengeDialog · tipear las 4 palabras
    U-->>PR: texto + OK
    PR->>U: PasswordDialog · campo enmascarado
    U-->>PR: contraseña + OK
    PR-->>ES: password

    ES->>IC: SendAsync(force_stop, app, password)
    IC->>IS: {"type":"force_stop","app_name":"firefox","password":"..."} + \n
    IS->>DH: HandleAsync(request)
    DH->>BE: TryEarlyStop(password, security)
    BE->>AV: VerifyPassword(password, hash, salt)
    AV-->>BE: true (Argon2id + FixedTimeEquals)
    BE-->>DH: true
    DH->>CD: StartCooldown("firefox", nowUtc, 10 min)
    DH-->>IS: IpcMessage(Ok)
    IS-->>IC: {"type":"ok"}
    IC-->>ES: Ok
    ES-->>APP: true
    APP->>U: MessageBox "Early stop granted for firefox"
```

**Clave:** la decisión vive en el daemon (root); la TUI solo aporta fricción y transporte. La
contraseña viaja por el socket pero **nunca se hace eco** en la respuesta. Tres caminos de falla que
no aparecen en el diagrama: (1) cancelar cualquier diálogo → `false` sin enviar IPC; (2) contraseña
incorrecta → el daemon responde `error` y **no** arranca cooldown; (3) daemon caído → `SocketException`
capturada en `EarlyStopService` y `false`. El resultado se muestra marshalado al hilo de UI con
`_app.Invoke(...)`.

**Patrones marcados:** fricción + seguridad en dos pasos (challenge → password) · contrato IPC
compartido (`force_stop`) · validación del lado confiable · degradación elegante (daemon ausente) ·
marshal de vuelta al hilo de UI.
