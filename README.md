# AutoResult.Generator

[![NuGet](https://img.shields.io/nuget/v/AutoResult.Generator.svg)](https://www.nuget.org/packages/AutoResult.Generator)
[![CI](https://github.com/Swevo/AutoResult.Generator/actions/workflows/build.yml/badge.svg)](https://github.com/Swevo/AutoResult.Generator/actions/workflows/build.yml)

A Roslyn source generator that automatically wraps your methods in `Result<T>` — zero reflection, zero runtime overhead, compile-time safe.

## Features

- **Core Result types** always generated: `Result<T>`, `Result<T,TError>`, `Unit`, `ResultExtensions`
- **`[TryWrap]`** generates `Try*()` variants for every public method in your partial class
- Supports **sync, void, async, and async-void** methods
- Compile-time diagnostics for misuse

## Installation

```bash
dotnet add package AutoResult.Generator
```

## Usage

```csharp
using AutoResult;

[TryWrap]
public partial class OrderService
{
    public int GetOrderId(string name) => // ...

    public async Task<Order> FetchOrderAsync(int id) => // ...

    public void ProcessOrder(Order order) => // ...
}
```

The generator produces:

```csharp
public partial class OrderService
{
    public Result<int> TryGetOrderId(string name)
    {
        try { return Result<int>.Ok(GetOrderId(name)); }
        catch (Exception ex) { return Result<int>.Fail(ex); }
    }

    public async Task<Result<Order>> TryFetchOrderAsync(int id)
    {
        try { return Result<Order>.Ok(await FetchOrderAsync(id)); }
        catch (Exception ex) { return Result<Order>.Fail(ex); }
    }

    public Result<Unit> TryProcessOrder(Order order)
    {
        try { ProcessOrder(order); return Result<Unit>.Ok(Unit.Value); }
        catch (Exception ex) { return Result<Unit>.Fail(ex); }
    }
}
```

## Core Types

```csharp
// Always available — no extra imports needed
Result<T>.Ok(value)
Result<T>.Fail(exception)
Result<T>.IsSuccess / .IsFailure
Result<T>.Value / .Error
Result<T,TError>.Ok(value)
Result<T,TError>.Fail(error)
Unit.Value  // for void-returning methods
```

## Diagnostics

| Code  | Severity | Description |
|-------|----------|-------------|
| AR001 | Error    | `[TryWrap]` applied to a non-partial class |
| AR002 | Warning  | `[TryWrap]` class has no wrappable public methods |

## Why AutoResult?

- **Zero overhead** — wrapper code is generated at compile time
- **No boilerplate** — stop writing the same try/catch in every service
- **Railway-oriented** — compose results cleanly with `Map`, `Bind`, `Match`
- **Minimal API** — no third-party Result library required; types live in your project

## Related Packages

| Package | Description |
|---------|-------------|
| [AutoMap.Generator](https://www.nuget.org/packages/AutoMap.Generator) | Compile-time object mapping |
| [AutoWire.Generator](https://www.nuget.org/packages/AutoWire.Generator) | Compile-time DI registration |
| [AutoValidate.Generator](https://www.nuget.org/packages/AutoValidate.Generator) | Compile-time FluentValidation wiring |

## License

MIT
