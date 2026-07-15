---
name: csharp-testing
description: 'Shared C# testing conventions covering the differences between Unit, Integration, and System tests. Use when determining which test type to write, understanding test project structure, or referencing general C# testing patterns. All C# test types use xUnit + Moq with AAA pattern.'
---

# C# Testing Conventions (Shared)

## Purpose

Defines the shared conventions that apply across all C# test types. For type-specific rules, see the dedicated skills: `csharp-testing-unit`, `csharp-testing-integration`, `csharp-testing-system`.

## Test Type Differences

| Aspect | Unit | Integration | System |
|--------|------|-------------|--------|
| **Applied to** | Business logic, algorithms, utilities | Database access, external APIs, I/O operations | End-to-end workflows, full application scenarios |
| **Test path** | `tests/Unit/` | `tests/Integration/` | `tests/System/` |
| **Project suffix** | `.Tests` | `.IntegrationTests` | `.SystemTests` |
| **File suffix** | `Tests.cs` | `IntegrationTests.cs` | `SystemTests.cs` |
| **Mocking** | All dependencies mocked | No mocking by default; only for non-critical external deps | No mocking by default; only for non-critical external deps |
| **Scope** | Single class/method | Class with all real dependencies | Full application entry point |
| **Test host** | N/A | N/A | `WebApplicationFactory` with Aspire |

## Shared Framework & Dependencies

- **Test Framework:** xUnit
- **Mocking Framework:** Moq
- **NuGet Packages Required:**
  - `xunit` (latest stable)
  - `xunit.runner.visualstudio` (latest stable)
  - `Moq` (latest stable)
  - `Microsoft.NET.Test.Sdk` (latest stable)
- **Test SDK Version:** Match the .NET version of the source project
- **Test Runner:** `dotnet test` CLI on the solution file
- **AAA Pattern:** Arrange-Act-Assert for all test methods

## Test Method Naming Convention

```
<MethodOrComponentName>_<WhenCondition>_<ExpectedResult>
```

Examples:
- `GetUser_WithValidId_ReturnsUser`
- `GetUser_WithInvalidId_ThrowsArgumentException`
- `CalculateTotal_WithEmptyList_ReturnsZero`

## Project Structure Rules

- Test projects mirror source project paths under the appropriate `tests/<Type>/` folder
- Test file names follow `<original-file-name><TypeSuffix>.cs`
- Test folders within the test project mirror the relative path of source code files

## Trait Attributes

All test classes must include a category trait for filtering:

- Unit: `[Trait("Category", "Unit")]`
- Integration: `[Trait("Category", "Integration")]`
- System: `[Trait("Category", "System")]`

Tests with external dependencies must also include:
- `[Trait("Dependency", "<DependencyNameHere>")]`