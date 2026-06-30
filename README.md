# AutoResult.Generator

[![NuGet](https://img.shields.io/nuget/v/AutoResult.Generator
[![NuGet Downloads](https://img.shields.io/nuget/dt/AutoResult.Generator.svg)](https://www.nuget.org/packages/AutoResult.Generator).svg)](https://www.nuget.org/packages/AutoResult.Generator)
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

## Also by the same author

> 🌐 Full suite overview: **[swevo.github.io](https://swevo.github.io/)**


| Package | Description |
|---|---|
| [**AutoWire**](https://github.com/Swevo/AutoWire) | Compile-time DI auto-registration — `[Scoped]`/`[Singleton]`/`[Transient]` generates `IServiceCollection` code. Zero reflection. |
| [**AutoMap.Generator**](https://github.com/Swevo/AutoMap.Generator) | Compile-time object mapping — `[Map(typeof(Dto))]` generates `ToDto()` extension methods. Zero reflection, AOT-safe. |
| [**AutoValidate.Generator**](https://github.com/Swevo/AutoValidate.Generator) | Compile-time FluentValidation wiring — discovers `AbstractValidator<T>` subclasses and generates `AddValidators()`. |
| [**AutoQuery.Generator**](https://github.com/Swevo/AutoQuery.Generator) | Compile-time LINQ query specs — `[QuerySpec(typeof(T))]` generates `Apply(IQueryable<T>)`. |
| [**AutoDispatch.Generator**](https://github.com/Swevo/AutoDispatch.Generator) | Compile-time CQRS dispatcher — `[Handler]` generates a strongly-typed `IDispatcher`. No `IRequest<T>`, no reflection. |

## License

MIT
