---
applyTo: "**/*.fs"
---

# F# Source File Instructions

These instructions apply to all F# implementation files (`.fs`).

## Formatting

- Use **4 spaces** for indentation (never tabs)
- Do NOT add newlines before multiline computation expressions
- Maximum line length should be reasonable (aim for ~120 characters when practical)
- Use consistent spacing around operators and function parameters

## Code Organization

- Order module members logically: types first, then functions
- Group related functions together
- Place helper functions close to where they're used, or at the bottom of the module
- Use `open` statements at the top of the file, grouped logically

## Function Design

- Use XML documentation comments (`/// <summary>`) for all public functions
- Include `<param>` tags for each parameter with clear descriptions
- Include `<returns>` tag describing the return value
- Include `<remarks>` for important usage notes or warnings

## Type Annotations

- Explicit type annotations are preferred for public APIs
- Use type inference for private/internal functions when the type is obvious
- Annotate function parameters when it improves clarity

## Naming Conventions

- Use `camelCase` for function names, parameters, and local values
- Use `PascalCase` for types, modules, and discriminated union cases
- Use descriptive names that convey purpose
- Avoid abbreviations unless they're well-known (e.g., `cmd` for command is acceptable)

## Error Handling

- Prefer `Result<'T, 'Error>` or `Option<'T>` over exceptions for expected errors
- Use exceptions only for truly exceptional conditions
- Document exceptions that might be thrown using `<exception>` tags

## Async/Task Patterns

- Support both F# `Async<'T>` and .NET `Task<'T>` patterns where appropriate
- Use `async { }` computation expressions for F# async
- Use `task { }` computation expressions for Task-based async
- Provide cancellation token support for async operations

## Computation Expressions

- Keep computation expression builders focused and single-purpose
- Document custom operations clearly
- Ensure computation expressions compose well with standard F# constructs

## Interop with CliWrap

- When wrapping CliWrap APIs, maintain the fluent/builder pattern feel
- Use piping (`|>`) to chain operations naturally
- Provide overloads for common scenarios while keeping advanced options available
- Ensure proper disposal of resources (use `use` or `IDisposable` appropriately)
