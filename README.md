# FocusBlock

> Terminal app blocker for Arch Linux — block distracting applications by schedule, enforce it with a root daemon, and track your usage.

![C#](https://img.shields.io/badge/C%23-.NET%2010-blue) ![License](https://img.shields.io/badge/license-MIT-green)

## Demo

> Screenshot pending — TUI runs with `dotnet run --project src/FocusBlock.Tui`.

## Problem it solves

Staying focused on a Linux desktop is hard when a browser or game is one click away. FocusBlock lets you define blocking rules (per app, per schedule), and a root daemon actually enforces them — killing the process, locking the config, and requiring a challenge or password to stop early. It's the app-blocking equivalent of a pomodoro timer with teeth.

## Features

- Block apps by process name with scheduled rules (time ranges per day)
- Root daemon (systemd) enforces blocks — SIGTERM → SIGKILL escalation
- Anti-bypass: password protection, `chattr +i` config lock, cooldown
- Usage metrics with ASCII charts in the terminal
- TUI ↔ daemon communication over Unix domain socket (JSON protocol)

## Stack and why

| Layer | Technology | Why |
|---|---|---|
| Language | C# 14 / .NET 10 | Modern language, strong typing, native AOT path |
| TUI | Terminal.Gui v2 | Full widget set, native .NET, cross-terminal |
| IPC | Unix domain socket | Fast, dependency-free, native Linux |
| Persistence | SQLite + Dapper | Simple, fast, no migrations ceremony |
| Hashing | Argon2id | Memory-hard, modern standard |
| Testing | xUnit + Moq + FluentAssertions | Industry standard, strong mocking |

## Architecture

```
┌─────────────────────────────────────────────┐
│  TUI (user)          Unix socket            │
│  Terminal.Gui  ◄──────────────────────►     │
├─────────────────────────────────────────────┤
│  Daemon (root)      /proc scan, kill        │
│  systemd service    SQLite metrics          │
└─────────────────────────────────────────────┘
```

TUI runs as a regular user (safe, easy to develop). Daemon runs as root via systemd (required for `/proc` and `chattr +i`). IPC keeps them decoupled.

## Setup

**Requirements:** .NET 10 SDK (Arch: `sudo pacman -S dotnet-sdk`)

```bash
git clone git@github.com:ails-w/focusblock.git
cd focusblock

dotnet build
dotnet test
```

## Usage

```bash
# Install daemon (systemd)
sudo systemctl start focusblock

# Run TUI as regular user
dotnet run --project src/FocusBlock.Tui
```

## Testing

- Unit: `dotnet test` — services, rule engine, cooldown, auth
- Integration: real SQLite, `/proc`, IPC socket, `chattr +i`
- Functional: daemon lifecycle in Docker (see `docs/development-plan.md`)

## What I Learned / Key Decisions

- **TUI ↔ daemon split**: running the TUI as a user and the enforcer as root via Unix socket taught me privilege separation without over-engineering. IPC over HTTP would have added auth surface for no gain.
- **Driver workaround**: Terminal.Gui's ANSI driver renders empty windows on Linux; forcing the DOTNET driver fixed it — a lesson in not trusting "official example == correct" on your platform.
- **Argon2id over bcrypt**: memory-hard hashing is the right default for local password protection.

## Known Limitations / What I'd Do Differently

- **Linux-only**: `/proc`, `chattr +i` and systemd are Arch-specific. Portability would require abstraction layers that don't yet pay off.
- **`chattr +i` requires root and can lock you out**: I'd add a recovery flow (e.g., systemd timer that clears the flag after a cooldown) before shipping.
- **SQLite without migrations**: fine for metrics, but I'd adopt a migration tool (FluentMigrator) if the schema grows.

## Roadmap

- [ ] Anti-bypass: challenge dialog + early-stop flow
- [ ] Usage metrics view with ASCII charts
- [ ] Docker deployment of the daemon

## License

MIT — 2026

## Contact

[tu-usuario](https://github.com/tu-usuario) — open an issue or PR for feedback.