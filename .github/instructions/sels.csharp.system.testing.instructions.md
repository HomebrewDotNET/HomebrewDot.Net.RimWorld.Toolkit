---
description: This file describes the C# system testing conventions and best practices for the project.
applyTo: **/tests/System/*.Tests*/*.cs
---

# C# System Testing Instructions

## Overview

This instructions file defines the standard practices and conventions for writing system tests in C# projects. All tests must follow xUnit framework with Moq for dependency mocking only when needed. System tests focus on the entry point of the application (API's, UI, Services, CLI's, etc.) and test the entire system end-to-end, including all layers and external dependencies. Mocking is only allowed for non-critical dependencies that are difficult to set up in test environments (e.g., third-party APIs). The goal of system testing is to validate the overall behavior and functionality of the application as it would be used in production.

## Project Structure

### Test Project Organization

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
- Test projects are created in the `tests/System/` folder at the root level
- Test project names follow the pattern: `<original-module-name>.SystemTests`
- Example: If source project is `src/Services/Services.csproj`, test project is `tests/System/Services/Services.SystemTests.csproj`

### Test File Structure
**Folder Structure:**
- Test folders should mirror the project being tests. For example, if testing a Web project with API and Dashboard, test files should be organized in `Web/` folder within the SystemTests project.
- Example: If source file is `Web/Api/Controllers/UserController.cs`, test file should be `Web/Api/UserSystemTests.cs` within the csproject files

**File Naming:**
- Test files must be named according to the component being tested, followed by `SystemTests.cs`
- Example: `UserController.cs` → `UserSystemTests.cs`


## Test Method Naming Convention

Test method names must follow this pattern:

```
<OriginalProjectName>_<WhenCondition>_<ExpectedResult>
```

**Examples:**
- `Api_WhenUserCreated_UserCanBeRetrieved`
- `Dashboard_WhenDataUpdated_DashboardReflectsChanges`
- `BackgroundService_WhenTriggered_TaskIsExecuted`
- `NotificationService_WhenEmailSent_EmailIsReceived`
- `CLI_WhenCommandExecuted_CorrectOutputIsDisplayed`

## Testing Framework & Dependencies

### Framework
- **Test Framework:** xUnit
- **Mocking Framework:** Moq
- **Test host** WebApplicationFactory with Aspire
- **NuGet Packages Required:**
  - `xunit` (latest stable)
  - `xunit.runner.visualstudio` (latest stable)
  - `Moq` (latest stable)
  - `Microsoft.NET.Test.Sdk` (latest stable)
- **Test SDK Version:** Match the .NET version of the source project
- **Test Runner:** Use `dotnet test` CLI command on the sln file to run all tests
- **AAA Pattern:** Follow Arrange-Act-Assert pattern for test method structure
- **Mark test category** Add `[Trait("Category", "System")]` attribute to class to allow filtering by test category
- **Mark app type** Add `[Trait("AppType", "Api")]` attribute to class to allow filtering by app type
- **Mark app name** Add `[Trait("AppName", "<AppNameHere>")]` attribute to class to allow filtering by app name
- **Mark external tests with external dependencies** Add `[Trait("Dependency", "<DependencyNameHere>")]` if the test relies on an externally mocked service
- **System Testing** No dependency mocking by default. No classes are called directory. Apps should be started and interacted with as a user would. Mocking is only allowed for non-critical dependencies that are difficult to set up in test environments (e.g., third-party APIs). The goal of system testing is to validate the overall behavior and functionality of the application as it would be used in production.
- **Scope** Tests always target an application with all its real dependencies.
- **Use same layer for data** When a test relies on specific data or state, that data should be created through the same layer of the application as it would be in production. For example, if testing an API, any required data should be created through API calls rather than directly manipulating the database or using test fixtures. This ensures that tests validate the full behavior of the system and catch issues that may arise from different layers of the application. Fallback is allowed by calling services directly if there are no exposed API's to create the required state, but this should be avoided when possible to ensure true end-to-end testing.
- **Each test that tests state should create it** Each test that relies on specific data or state should create that state within the test method itself. Avoid relying on shared state or pre-existing data to ensure tests are independent and repeatable.
- **Use a variety of test data in system tests** Use a mix of valid, invalid, edge case, and typical data in system tests to ensure comprehensive coverage of real-world scenarios. This helps identify issues that may arise from different types of input and interactions between components.