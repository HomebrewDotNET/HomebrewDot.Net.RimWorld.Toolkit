---
name: csharp-testing-unit
description: 'C# unit testing conventions using xUnit and Moq. Use when writing unit tests for business logic, algorithms, or utilities. Tests go in tests/Unit/ with .Tests suffix. Follow AAA pattern and mirror source paths.'
---

# C# Unit Testing

## Purpose

Defines conventions for writing unit tests in C# projects. Unit tests target business logic, algorithms, and utilities in isolation. All dependencies are mocked with Moq.

For shared conventions (framework, naming, structure), see `csharp-testing` skill.

## When to Use

- Testing business logic classes
- Testing algorithms and pure functions
- Testing utility/helper methods
- Any test where all dependencies can be mocked

## Project Structure

Source code structure:
```
src/
├── Services/
│   └── Services.csproj
├── Data/
│   └── Data.csproj
└── Models/
    └── Models.csproj
```

Test structure:
```
tests/Unit/
├── Services/
│   └── Services.Tests.csproj
├── Data/
│   └── Data.Tests.csproj
└── Models/
    └── Models.Tests.csproj
```

**Rules:**
- Test projects go in `tests/Unit/` at the root level
- Test project names: `<original-module-name>.Tests`
- Example: `src/Services/Services.csproj` → `tests/Unit/Services/Services.Tests.csproj`

### Test File Structure

- Test folders mirror the relative path of source code files in the source project
- Test files named `<original-file-name>Tests.cs`
- Example: `Services/UserService.cs` → `Services/UserServiceTests.cs`

## Test Method Naming

```
<OriginalMethodName>_<WhenCondition>_<ExpectedResult>
```

Examples:
- `GetUser_WithValidId_ReturnsUser`
- `GetUser_WithInvalidId_ThrowsArgumentException`
- `CalculateTotal_WithEmptyList_ReturnsZero`
- `ValidateEmail_WithNullInput_ThrowsArgumentNullException`

## Mocking Strategy

- Mock ALL dependencies with Moq
- Use interfaces to facilitate mocking
- Inject mocks via constructor in test setup

## Trait Attributes

- `[Trait("Category", "Unit")]` on every test class