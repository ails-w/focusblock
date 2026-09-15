# Fase {{NN}} — {{Tema central}}

> Qué se aprende en esta fase y por qué importa.
> Cómo se conecta con las fases vecinas.
> Log de la fase → `docs/progress-log/phase-{{NN}}-{{nombre}}.md`

## Glosario de la fase

| Término | Qué significa (en una línea) |
|---|---|
| {{término}} | {{definición mínima}} |

## Mapa de conceptos

```text
{{Diagrama ASCII: qué concepto depende de cuál.}}
```

## Puntos de inyección de la fase

| Componente | Seam | Doble | Test real |
|---|---|---|---|
| {{Componente}} | {{Punto de inyección}} | {{Doble para unit}} | {{Test real de integración}} |

---

## {{Concepto}}

### En una frase
{{La idea central, sin jerga.}}

### Fundamentos previos
{{Términos e ideas que hay que entender ANTES de este concepto. Explicá cada uno acá mismo.
No asumas conocimiento previo. Si depende de un concepto de otra fase, referencialo.}}

### Qué es
{{Definición desarrollada. Explicar, no nombrar.}}

### Qué problema resuelve
{{El dolor concreto que existiría sin esto.}}

### Cómo funciona paso a paso
{{La mecánica interna, numerada. El porqué detrás de cada paso.}}

### Qué se rompería sin esto en FocusBlock
{{Contrafactual concreto y específico de este proyecto.}}

### Para qué sirve en este proyecto
{{Dónde se usa y por qué acá.}}

### Cómo se usa (código real)
```cs
{{Código real del repo. Referenciar archivo:línea cuando aporte.}}
```

### Error común
{{Qué se hace mal, por qué, y cómo detectarlo.}}

### Punto de inyección (si aplica)
{{Si el concepto depende del sistema operativo (filesystem, `/proc`, señales, `chattr`, reloj,
sockets, SQLite), nombrá acá el **seam** con el que se testea, el **doble** que se usa en unit
y el **test real de integración** en host. Si el concepto no toca el SO, omití esta sección.}}

### Para profundizar
{{Referencias oficiales + conceptos relacionados + links a otros docs.}}

---

## Relación entre estos conceptos
{{Qué depende de qué y por qué el orden importa.}}

---

## Convención

- **Archivo**: `docs/learning/phase-{{NN}}-{{nombre}}.md` (ej: `phase-03-daemon.md`)
- **Título**: `# Fase {{NN}} — {{Tema}}`
- **Un archivo por fase**, creado al comenzar la fase.
- **Secciones por concepto**: En una frase / Fundamentos previos / Qué es /
  Qué problema resuelve / Cómo funciona paso a paso / Qué se rompería sin esto /
  Para qué sirve en este proyecto / Cómo se usa / Error común / Punto de inyección (si aplica) /
  Para profundizar.
- **Glosario + mapa de conceptos**: obligatorios al inicio del archivo de cada fase.
- **Puntos de inyección**: sección por concepto (si toca el SO) + tabla de fase; el catálogo completo vive en `docs/development-plan.md`.
- **Código**: bloques `cs` (nunca el alias largo de C#) y líneas de máximo ~100 caracteres.
