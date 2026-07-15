---
name: csharp-testing-integration
description: 'C# integration testing conventions using xUnit. Use when writing integration tests for database access, external APIs, or I/O operations. Tests go in tests/Integration/ with .IntegrationTests suffix. No dependency mocking by default — use real dependencies.'
---

# C# Integration Testing

## Purpose

Defines conventions for writing integration tests in C# projects. Integration tests target database access, external APIs, and I/O operations. No dependency mocking by default — use real dependencies to ensure true integration testing.

For shared conventions (framework, naming, structure), see `csharp-testing` skill.

## When to Use

- Testing database access and data layer interactions
- Testing external API integrations
- Testing file I/O and system interactions
- Any test where real dependencies should be exercised

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
tests/Integration/
├── Services/
│   └── Services.IntegrationTests.csproj
├── Data/
│   └── Data.IntegrationTests.csproj
└── Models/
    └── Models.IntegrationTests.csproj
```

**Rules:**
- Test projects go in `tests/Integration/` at the root level
- Test project names: `<original-module-name>.IntegrationTests`
- Example: `src/Services/Services.csproj` → `tests/Integration/Services/Services.IntegrationTests.csproj`

### Test File Structure

- Test folders mirror the relative path of source code files in the source project
- Test files named `<original-file-name>IntegrationTests.cs`
- Example: `Services/UserService.cs` → `Services/UserServiceIntegrationTests.cs`

## Mocking Strategy

- **No dependency mocking by default.** Use real dependencies and external resources where possible.
- Mocking is only allowed for non-critical dependencies that are difficult to set up in test environments (e.g., third-party APIs).

## Scope

- Tests always target a class with all its real dependencies.
- When testing a service that depends on a data layer, use the actual data layer implementation instead of mocking it.

## Multiple Implementations

When multiple implementations exist for a dependency:
- Create shared tests in a base class
- Inherit separate test classes for each implementation
- Result: permutation of tests across all implementations without duplication

## State Management

- Each test that relies on specific data or state must create that state within the test method itself.
- Avoid relying on shared state or pre-existing data to ensure tests are independent and repeatable.

## Test Data Variety

Use a mix of valid, invalid, edge case, and typical data to ensure comprehensive coverage of real-world scenarios.

## Trait Attributes

- `[Trait("Category", "Integration")]` on every test class
- `[Trait("Dependency", "<DependencyNameHere>")]` if the test relies on an externally mocked service