# Referencia Técnica

Documentación de referencia rápida para el desarrollo.

## Archivos

### `arquitectura.md`
- Diagrama ASCII del sistema (TUI ↔ Daemon)
- Por qué esta arquitectura (separación user/root)
- Componentes y sus responsabilidades
- Protocolo IPC (Unix socket, JSON messages)
- Esquema SQLite
- Servicio systemd
- Decisiones de arquitectura

### `plan-desarrollo.md`
- Estrategia de testing (pirámide + ciclo TDD)
- Convención de nombres de tests
- Integración Docker (Dockerfiles + compose)
- Cuándo usar cada tipo de test
- Checklist de deployment

### `phases.md`
- Fase 0-7 con checkboxes
- Features individuales con tests requeridos
- Orden de desarrollo feature por feature
- **Este es el archivo principal de referencia durante el desarrollo**

## Uso

Consultar estos archivos durante el desarrollo para:
- Entender cómo interactúan los componentes
- Recordar comandos de uso frecuente
- Seguir el orden de desarrollo por fases
- Verificar el protocolo IPC al implementar nueva funcionalidad
