# Agent Guidelines

## Build / Lint / Test Commands

| Command | Description |
|---------|-------------|
| `dotnet restore` | Restores NuGet packages for all projects.
| `dotnet build -c Release` | Builds the solution in Release configuration.
| `dotnet test --no-build` | Runs all tests without rebuilding. Use `--filter FullyQualifiedName~<TestClass>` to run a single test.
| `dotnet format` | Formats code according to .editorconfig rules.
| `dotnet lint` *(via dotnet-format or StyleCop)* | Lints the codebase; configure in `.editorconfig`.

### Running a Single Test
```bash
# Replace <FullyQualifiedName> with the full test name, e.g. Namespace.Class.TestMethod
# The ~ syntax selects tests that contain the string.
dotnet test --filter FullyQualifiedName~<FullyQualifiedName>
```

## Code Style Guidelines

### Documentation
- Use XML comments for public members, classes, and interfaces.
- Add comments to each step. Try to make it educational
- Use imperative tone for method descriptions.

### Import Order and Namespace Grouping
- System namespaces first, then Microsoft, third‑party, finally project namespaces.
- Avoid `using static` unless absolutely necessary.
- Place each group on its own line for readability.

### Imports & Namespaces
- Use `using` statements at the top of each file; group system namespaces first, then third‑party, then project namespaces.
- Avoid wildcard imports (`using static`). Prefer explicit references.
- Order: System, Microsoft, third‑party, local.

### Formatting
- Follow .editorconfig rules (indentation 4 spaces, no trailing whitespace). The repo contains an `.editorconfig` that enforces these settings.
- Line length ≤ 120 characters; wrap long expressions with a line break after the operator.

### Types & Naming
- Public types: PascalCase (`public class MyClass`).
- Private fields: `s_camelCase`. Prefix backing fields with `s_`.
- Methods: PascalCase. Parameters: camelCase.
- Interfaces: prefix `I` (e.g., `IDisposable`).
- Enums: PascalCase; values in PascalCase.
- Collections: Reserve mutable collections (List<T>, T[], etc.) for when mutation is required. Otherwise, prefer concrete Immutable/Frozen collections where possible.

### Error Handling
- Prefer throwing specific exceptions (`ArgumentNullException`, `InvalidOperationException`).
- Do not swallow generic `Exception`; log and rethrow if necessary.
- Use `ErrorOr<T>` patterns where appropriate (not used currently).

### Exceptions
- For cleaner Error handling, try providing bespoke Exception definitions.
- Also, try using predefined ErrorOr<T>.Error to cache known Error instances wherever possible

### Methods
- Parameters: Take care to avoid silent struct copying when passing structs to a method. Prefer using `in` parameters where possible
- Always validate passed in parameters
- Prefer `ReadOnlySpan<T> params` over `T[] params`. The runtime will resolve how best to allocate the params
- Prefer Pure methods because [Pure] methods are easier to Test repeatedly
- Compute is cheaper than storage in modern systems. So try to optimize for storage (cache hits) over raw compute, in general

### Async Patterns
- Return `Task` or `Task<T>`. Avoid `async void` except for event handlers.
- Use ValueTask where possible/appropriate
- Await on the first operation in a method; do not chain `.ContinueWith`.
- CancellationToken: Follow the cancellation pattern to check for cancellation and register for cancellation is requested.

### Logging
- Prefer LogMessageDelegates over generic logger.Log methods
- Always log what is happening: Information, Errors, etc

### Testing
- Use xUnitv3 (`[Fact]`, `[Theory]`).
- Arrange‑Act‑Assert pattern. Keep tests deterministic and isolated.
- Use NSubstitute where appropriate
- Do not use `Thread.Sleep`; prefer async delays or mocks.

## Cursor / Copilot Rules

There are no `.cursor` rules in this repository. The repo does contain a **Copilot instructions** file at `.github/copilot-instructions.md`. The contents provide guidelines for using GitHub Copilot:

```markdown
# Copilot Guidance
- Keep suggestions short and focused.
- Verify generated code against existing style rules.
- Use Copilot only as an aid; review all changes before committing.
```

## Additional Notes
- All CI runs the workflow defined in `.github/workflows/main.yml` which restores, builds, tests, and publishes NuGet packages on `main` pushes.
- The solution uses .NET 10.0 (`dotnet-version: '10.0.x'`).
- The project is a library; no executable entry points.

---

*These guidelines are intended to help agents produce consistent, high‑quality code that aligns with the existing repository conventions.*
