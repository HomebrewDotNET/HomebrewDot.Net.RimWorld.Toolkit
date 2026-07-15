---
name: csharp-testing-system
description: 'C# system testing conventions using xUnit and WebApplicationFactory with Aspire. Use when writing end-to-end system tests for APIs, UIs, services, or CLIs. Tests go in tests/System/ with .SystemTests suffix. No dependency mocking by default — test the full application as a user would.'
---

# C# System Testing

## Purpose

Defines conventions for writing system tests in C# projects. System tests focus on the entry point of the application (APIs, UI, Services, CLIs) and test the entire system end-to-end, including all layers and external dependencies. The goal is to validate overall behavior as it would be used in production.

For shared conventions (framework, naming, structure), see `csharp-testing` skill.

## When to Use

- Testing API endpoints end-to-end
- Testing UI workflows
- Testing background services
- Testing CLI tools
- Any test that validates the full application stack

## Project Structure

Source code structure:
```
src/
├── Presentation/
│   ├── Web/
│   │   ├── Api.csproj
│   │   └── Dashboard.csproj
│   ├── Services/
│   │   ├── BackgroundService.csproj
│   │   └── NotificationService.csproj
│   └── Tools/
│       └── CLI.csproj
```

Test structure:
```
tests/
├── System/
│   ├── Web.SystemTests.csproj
│   ├── Services.SystemTests.csproj
│   └── Tools.SystemTests.csproj
```

**Rules:**
- Test projects go in `tests/System/` at the root level
- Test project names: `<original-module-name>.SystemTests`
- Example: `src/Services/Services.csproj` → `tests/System/Services/Services.SystemTests.csproj`

### Test File Structure

- Test folders mirror the project being tested
- Test files named according to the component being tested, followed by `SystemTests.cs`
- Example: `Web/Api/Controllers/UserController.cs` → `Web/Api/UserSystemTests.cs`

## Test Method Naming

```
<OriginalProjectName>_<WhenCondition>_<ExpectedResult>
```

Examples:
- `Api_WhenUserCreated_UserCanBeRetrieved`
- `Dashboard_WhenDataUpdated_DashboardReflectsChanges`
- `BackgroundService_WhenTriggered_TaskIsExecuted`
- `CLI_WhenCommandExecuted_CorrectOutputIsDisplayed`

## Test Host

- Use `WebApplicationFactory` with Aspire as the test host
- Apps should be started and interacted with as a user would
- No classes are called directly

## Mocking Strategy

- **No dependency mocking by default.** Mocking is only allowed for non-critical dependencies that are difficult to set up in test environments (e.g., third-party APIs).

## Scope

- Tests always target an application with all its real dependencies.

## Data Creation

- When a test relies on specific data or state, create that data through the same application layer as in production.
- For API tests: create required data through API calls rather than directly manipulating the database.
- Fallback: calling services directly is allowed if there are no exposed APIs to create the required state, but avoid when possible.

## State Management

- Each test that relies on specific data or state must create that state within the test method itself.
- Avoid relying on shared state or pre-existing data.

## Test Data Variety

Use a mix of valid, invalid, edge case, and typical data to ensure comprehensive coverage of real-world scenarios.

## Trait Attributes

- `[Trait("Category", "System")]` on every test class
- `[Trait("AppType", "Api")]` to allow filtering by app type
- `[Trait("AppName", "<AppNameHere>")]` to allow filtering by app name
- `[Trait("Dependency", "<DependencyNameHere>")]` if the test relies on an externally mocked service