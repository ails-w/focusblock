# Aprendizaje por fase

> Cómo estudiar, no qué leer. El objetivo es retener, no cubrir.

Cada fase tiene **un archivo** con 4-6 conceptos. No se lee de corrido: se estudia por
conceptos, cerrando el archivo antes de responder las preguntas.

## Sesión de 10 minutos

| Min | Qué haces |
|---|---|
| 0-2 | Lees "Dónde estamos", "Qué construye" y "El recorrido de una acción" |
| 2-4 | Lees **un** concepto |
| 4-7 | Cierras todo y respondes sus 3 preguntas en voz alta |
| 7-9 | Abres el código real y buscas el snippet |
| 9-10 | Escribes 3 líneas con tus palabras y marcas la confianza (1-5) |

Una fase completa son 4-6 sesiones cortas, no una sentada. Antes de empezar la fase
siguiente, responde 3 preguntas de una fase anterior.

## Fases

| Fase | Archivo | Tema | Estado |
|---|---|---|---|
| 0 | `phase-00-setup.md` | Solución, TUI y driver | ✅ |
| 1 | `phase-01-tui.md` | Composición, layout y navegación | ✅ |
| 2 | `phase-02-config.md` | Modelos, JSON, async y hashing | ✅ |
| 3 | `phase-03-daemon.md` | Ciclo de vida, `/proc`, señales e IPC | ✅ |
| 4 | `phase-04-blocker.md` | Reglas, cooldown y early stop | ✅ |
| 5 | `phase-05-antibypass.md` | Archivos inmutables | ⏳ |
| 6 | `phase-06-metrics.md` | SQLite y métricas | ⏳ |
| 7 | `phase-07-polish.md` | Pulido y testing | ⏳ |

## Reglas

- **Se corta sin piedad.** Lo eliminado queda en git. Cubrir no es aprender.
- **Una idea por concepto.** Si necesitas dos, son dos conceptos.
- **La jerga va después de la idea.** Primero la analogía, después el término técnico.
- **Nada se explica dos veces.** La segunda vez es un enlace al [glosario](glossary.md).
- **Cada concepto termina en `Verifica`.** Sin preguntas no hay recuperación.
- **Máximo 3 enlaces por fase:** un ADR, el código real y una referencia externa.
- **Español neutro.** "Usa", "escribe", "verifica". Sin voseo ni regionalismos.

## Repaso con IA

Pega este bloque al empezar una sesión de repaso:

```text
Actúa como tutor socrático de FocusBlock. Fuente: docs/learning/phase-NN-name.md,
concepto N.
Reglas:
- Una pregunta por turno. Nunca des la respuesta completa.
- Empieza por recuerdo; si acierto, sube a caso borde; si fallo, pide mi razonamiento.
- Si me equivoco, señala el hueco exacto con el enlace al concepto.
- Máximo 12 preguntas. Al final: 3 bullets de lo que no domino y qué repasar mañana.
- No avances si no puedo explicarlo con mis palabras.
Empieza por: <concepto>.
```

## Cómo saber que aprendiste

1. Dibujas el recorrido de la fase de memoria, sin abrir nada.
2. Explicas cada concepto en 30 segundos, con la analogía y el porqué.
3. Sabes qué archivo tocar para cambiar el comportamiento.
4. Aciertas las preguntas de mezcla dos días después, no el mismo día.
