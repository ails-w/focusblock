# ADR-008: Stack de testing xUnit + Moq + FluentAssertions

- **Estado**: Aceptado
- **Fecha**: 2026-09-02

## Contexto

El proyecto exige TDD estricto y una pirámide de testing (unit → integración → funcional).

## Decisión

**xUnit** + **Moq** + **FluentAssertions** (proyecto `tests/FocusBlock.Tests.Unit/`).

## Alternativas consideradas

- **NUnit**: válido, menos estándar en proyectos modernos .NET.
- **MSTest**: integrado con VS, menos flexible para mocks.

## Consecuencias

- Estándar de industria, mejor mocking con Moq, assertions legibles con FluentAssertions.
- Compatible con el naming `Method_Condition_ExpectedResult`.

## Referencias

- `docs/development-plan.md` (Pirámide y ciclo TDD) · `AGENTS.md` (TDD estricto)