# Documentación FocusBlock

Esta carpeta contiene toda la documentación del proyecto.

## Estructura

```
docs/
├── aprendizaje/       # Diario de aprendizaje por fase
├── referencia/        # Documentación técnica de referencia
└── diagramas/         # Diagramas ASCII/Mermaid del sistema
```

## Carpentas

### aprendizaje/

Diario de aprendizaje por fase de desarrollo. Cada archivo documenta:
- Objetivos de la fase
- Tareas completadas con fecha
- Decisiones tomadas y por qué
- Problemas encontrados y soluciones
- Aprendizajes clave
- Métricas (tests, cobertura)

**Plantilla:** Ver `template-fase.md` en esta carpeta.

### referencia/

Documentación técnica rápida:
- `arquitectura.md` — Diagrama del sistema, por qué esta arquitectura
- `comandos.md` — Referencia de comandos CLI, dotnet, systemd

### diagramas/

Solo si es necesario. Diagramas ASCII o Mermaid que expliquen:
- Flujo de datos entre componentes
- Diagrama de componentes
- Secuencia de operaciones

## Reglas

- Idioma: Español
- Formato: Markdown
- Mantener actualizado al final de cada fase
