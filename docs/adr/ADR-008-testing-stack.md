# ADR-008: Stack de testing xUnit + Moq + FluentAssertions

- **Estado**: Aceptado
- **Fecha**: 2026-09-02

## Contexto

El proyecto exige TDD estricto y una pirámide de testing (unit → integración → funcional). Las dependencias del SO se testean por **puntos de inyección (seams)** y los tests de integración corren **en el host**, no en contenedores (ver `ADR-009`).

## Decisión

**xUnit** + **Moq** + **FluentAssertions** (proyecto `tests/FocusBlock.Tests.Unit/`).

## Alternativas consideradas

- **NUnit**: válido, menos estándar en proyectos modernos .NET.
- **MSTest**: integrado con VS, menos flexible para mocks.

## Consecuencias

- Estándar de industria, mejor mocking con Moq, assertions legibles con FluentAssertions.
- Compatible con el naming `Method_Condition_ExpectedResult`.
- Los unitarios usan fakes vía seams; los de integración corren en el host sin contenedores (ver `ADR-009`).

## Referencias

- `docs/development-plan.md` (Pirámide y ciclo TDD) · `AGENTS.md` (TDD estricto) · `docs/adr/ADR-009-testing-seams.md`