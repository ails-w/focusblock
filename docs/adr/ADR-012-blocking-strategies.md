# ADR-012: Estrategias de bloqueo — Schedule-only por ahora

- **Estado**: Aceptado (revisar al expandir)
- **Fecha**: 2026-09-07

## Contexto

El usuario quería soportar **3 estrategias de bloqueo**: Horario (Schedule), Racha de uso (UsageWindow: máximo uso continuo antes de bloquear) y Tope diario (DailyCap: máximo uso acumulado por día). El plan original solo contemplaba **Schedule**. Las estrategias de uso requieren **telemetría** (rastrear tiempo de uso por app) que solo existirá en Fases 3 (daemon) y 6 (métricas). Decisión de producto: **mantener simple y terminar antes**, agregar las estrategias de uso más adelante.

## Decisión

Implementar **solo Schedule** por ahora. Cuando se agreguen las otras estrategias, se añade un campo `BlockStrategy Strategy { get; set; } = BlockStrategy.Schedule;` (default) → las configs ya guardadas siguen cargando (campo faltante = default) → **cero migración de datos**.

## Alternativas consideradas

- **Implementar las 3 ahora**: correcto pero expande el scope del daemon (rastreo de uso) y del motor (evaluación por estrategia) antes de tiempo.
- **Modelo polimórfico ahora** (records por estrategia + `[JsonDerivedType]`): elegante y type-safe, pero complejidad de serialización innecesaria hoy.

## Consecuencias

- Esquema simple y fase terminable ya.
- Expansión futura barata por diseño (campo con default).
- El motor (Fase 4) implementará primero la evaluación de Schedule; Racha/Tope cuando exista la telemetría.

## Referencias

- `src/FocusBlock.Contracts/BlockRuleConfig.cs` · `docs/progress-log/phase-02-config.md` (decisión 1)