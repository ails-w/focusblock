# Session Bootstrap

## Iniciar Sesión

1. **Memoria:** `mem_context(project: "focusblock")`
2. **Leer fase actual:** `progreso/FASE-{N}-{Name}.md`
3. **Leer este archivo** para contexto rápido
4. **Leer phases.md:** `docs/referencia/phases.md` — ver qué falta
5. **Reportar al usuario:** estado actual, qué sigue
6. **Preguntar:** "¿Qué hacemos hoy?"

## Reanudar Trabajo

- Verificar `progreso/` para la última tarea completada
- Verificar tests: `dotnet test`
- Continuar desde donde se quedó

## Empezar Fase Nueva

1. Leer la sección de la fase en `docs/referencia/phases.md`
2. Crear/actualizar `progreso/FASE-{N}.md`
3. Iniciar ciclo TDD: RED → GREEN → REFACTOR

## Reglas Importantes

- **Código:** English | **Docs:** Spanish
- **Tests ANTES de implementar** (TDD)
- **Guardar decisiones:** `mem_save` después de cada decisión
- **Cerrar sesión:** `mem_session_summary` (OBLIGATORIO)
