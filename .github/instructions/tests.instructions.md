---
applyTo: "**/*Tests.fs,**/*Tests.fsx,**/tests/**/*.fs"
---

# Test File Instructions

These instructions apply to all test files.

## Testing Framework

- Use **xUnit** with `[<Fact>]` attribute for unit tests
- Use **FsCheck** with `[<Property>]` attribute for property-based tests
- Import both frameworks: `open Xunit` and `open FsCheck.Xunit`

## Test Naming

- Use descriptive test names that explain what is being tested
- Use backticks for test names with spaces: `` [<Fact>] let ``Should do something`` () = ``
- Property test names should describe the property being verified
- Follow the pattern: "Should [expected behavior] [under what conditions]"

## Test Structure

- Arrange: Set up test data and preconditions
- Act: Execute the code under test
- Assert: Verify the expected outcome
- Use clear variable names: `expected`, `actual`, `result`

## Property-Based Testing

- Use FsCheck's `NonNull<'T>` type for non-null constraints
- Use appropriate FsCheck types: `NonEmptyString`, `PositiveInt`, etc.
- Keep properties simple and focused on a single aspect
- Use `==>` for preconditions/implications when needed

## Test Data

- Use the CliWrap test dummy program from `src/CliWrap.Tests.Dummy` when available
- Reference it as `Dummy.Program.FilePath` from `CliWrap.Tests` namespace
- Create minimal test data that clearly demonstrates the test scenario
- Avoid complex test setup; prefer simple, readable tests

## Assertions

- Use xUnit assertions: `Assert.Equal`, `Assert.True`, etc.
- For property tests, return boolean expressions directly
- Compare specific properties rather than entire objects when appropriate
- Include meaningful assertion messages for complex tests

## Async Testing

- xUnit supports `async` directly; no special attributes needed
- Tests can return `Task`, `Task<'T>`, `Async<'T>`, or `unit`
- Use appropriate cancellation tokens in async tests
- Clean up resources properly with `use` bindings

## Test Organization

- Group related tests in the same file
- Use descriptive module names that reflect what's being tested
- Keep test files focused on a single component or feature
- Place test files in `src/CliWrap.FSharp.Tests/`

## Mocking and Stubs

- Prefer real implementations over mocks when practical
- Use test doubles only when necessary for isolation
- Keep test doubles simple and focused

## Code Coverage

- Aim for high code coverage but prioritize meaningful tests
- Focus on testing public APIs and edge cases
- Don't test framework code or trivial property accessors

## Performance Tests

- Mark performance-sensitive tests appropriately
- Consider using `[<Trait>]` attributes to categorize tests
- Performance tests should be deterministic and not flaky
