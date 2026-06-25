using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace AutoResult.Tests;

public class ResultGeneratorTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────
    private static Dictionary<string, string> RunGenerator(string userSource)
    {
        var refs = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Threading.Tasks").Location),
        };

        // Load Task<T> assembly
        try { refs.Add(MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location)); }
        catch { /* best-effort */ }

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { CSharpSyntaxTree.ParseText(userSource) },
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new AutoResultGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        var result = driver.GetRunResult();
        var output = new Dictionary<string, string>();
        foreach (var tree in result.GeneratedTrees)
            output[System.IO.Path.GetFileName(tree.FilePath)] = tree.GetText().ToString();
        return output;
    }

    private static IReadOnlyList<Diagnostic> GetDiagnostics(string userSource)
    {
        var refs = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { CSharpSyntaxTree.ParseText(userSource) },
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new AutoResultGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diags);
        return diags;
    }

    // ── Core types always emitted ─────────────────────────────────────────────
    [Fact]
    public void CoreTypes_AlwaysEmitted()
    {
        var sources = RunGenerator("");
        Assert.True(sources.ContainsKey("AutoResult.Core.g.cs"), "Core types file should always be emitted");
    }

    [Fact]
    public void CoreTypes_ContainsResultT()
    {
        var src = RunGenerator("")["AutoResult.Core.g.cs"];
        Assert.Contains("struct Result<T>", src);
        Assert.Contains("IsSuccess", src);
        Assert.Contains("static Result<T> Ok(T value)", src);
        Assert.Contains("static Result<T> Fail(Exception error)", src);
    }

    [Fact]
    public void CoreTypes_ContainsResultTTError()
    {
        var src = RunGenerator("")["AutoResult.Core.g.cs"];
        Assert.Contains("struct Result<T, TError>", src);
        Assert.Contains("static Result<T, TError> Ok(T value)", src);
        Assert.Contains("static Result<T, TError> Fail(TError error)", src);
    }

    [Fact]
    public void CoreTypes_ContainsUnit()
    {
        var src = RunGenerator("")["AutoResult.Core.g.cs"];
        Assert.Contains("struct Unit", src);
        Assert.Contains("static readonly Unit Value", src);
    }

    [Fact]
    public void CoreTypes_ContainsResultExtensions()
    {
        var src = RunGenerator("")["AutoResult.Core.g.cs"];
        Assert.Contains("static class ResultExtensions", src);
        Assert.Contains("Map<T, TOut>", src);
        Assert.Contains("Bind<T, TOut>", src);
        Assert.Contains("Match<T, TOut>", src);
        Assert.Contains("OnSuccess", src);
        Assert.Contains("OnFailure", src);
    }

    [Fact]
    public void CoreTypes_ContainsTryWrapAttribute()
    {
        var src = RunGenerator("")["AutoResult.Core.g.cs"];
        Assert.Contains("TryWrapAttribute", src);
    }

    // ── [TryWrap] — sync method ───────────────────────────────────────────────
    [Fact]
    public void TryWrap_SyncMethod_GeneratesTryWrapper()
    {
        var src = RunGenerator(@"
using AutoResult;
namespace MyApp
{
    [TryWrap]
    public partial class OrderService
    {
        public int GetOrderId(string name) => 42;
    }
}");
        Assert.True(src.ContainsKey("AutoResult.OrderService.g.cs"));
        var code = src["AutoResult.OrderService.g.cs"];
        Assert.Contains("TryGetOrderId(", code);
        Assert.Contains("Result<", code);
        Assert.Contains(".Ok(GetOrderId(name))", code);
    }

    [Fact]
    public void TryWrap_VoidMethod_GeneratesUnitResult()
    {
        var src = RunGenerator(@"
using AutoResult;
namespace MyApp
{
    [TryWrap]
    public partial class OrderService
    {
        public void Save(int id) { }
    }
}");
        var code = src["AutoResult.OrderService.g.cs"];
        Assert.Contains("TrySave(", code);
        Assert.Contains("Result<Unit>", code);
        Assert.Contains("Result<Unit>.Ok(Unit.Value)", code);
    }

    [Fact]
    public void TryWrap_AsyncMethod_GeneratesAsyncTryWrapper()
    {
        var src = RunGenerator(@"
using AutoResult;
using System.Threading.Tasks;
namespace MyApp
{
    [TryWrap]
    public partial class OrderService
    {
        public async Task<int> GetAsync(int id) => await Task.FromResult(id);
    }
}");
        var code = src["AutoResult.OrderService.g.cs"];
        Assert.Contains("TryGetAsync(", code);
        Assert.Contains("async Task<Result<", code);
        Assert.Contains(".ConfigureAwait(false)", code);
    }

    [Fact]
    public void TryWrap_AsyncVoidMethod_GeneratesUnitTask()
    {
        var src = RunGenerator(@"
using AutoResult;
using System.Threading.Tasks;
namespace MyApp
{
    [TryWrap]
    public partial class OrderService
    {
        public async Task SaveAsync(int id) { await Task.CompletedTask; }
    }
}");
        var code = src["AutoResult.OrderService.g.cs"];
        Assert.Contains("TrySaveAsync(", code);
        Assert.Contains("Task<Result<Unit>>", code);
    }

    [Fact]
    public void TryWrap_MultipleParams_PassedThrough()
    {
        var src = RunGenerator(@"
using AutoResult;
namespace MyApp
{
    [TryWrap]
    public partial class Svc
    {
        public string Create(int id, string name) => name;
    }
}");
        var code = src["AutoResult.Svc.g.cs"];
        Assert.Contains("id, string name", code);
        Assert.Contains("Create(id, name)", code);
    }

    [Fact]
    public void TryWrap_ExistingTryMethods_NotDoubleWrapped()
    {
        var src = RunGenerator(@"
using AutoResult;
namespace MyApp
{
    [TryWrap]
    public partial class Svc
    {
        public int GetId() => 1;
        public int TryAlreadyWrapped() => 1; // should be skipped
    }
}");
        var code = src["AutoResult.Svc.g.cs"];
        Assert.DoesNotContain("TryTryAlreadyWrapped", code);
        Assert.Contains("TryGetId", code);
    }

    [Fact]
    public void TryWrap_StaticMethods_NotWrapped()
    {
        var src = RunGenerator(@"
using AutoResult;
namespace MyApp
{
    [TryWrap]
    public partial class Svc
    {
        public int InstanceMethod() => 1;
        public static int StaticMethod() => 2; // should be skipped
    }
}");
        var code = src["AutoResult.Svc.g.cs"];
        Assert.DoesNotContain("TryStaticMethod", code);
        Assert.Contains("TryInstanceMethod", code);
    }

    // ── Diagnostics ───────────────────────────────────────────────────────────
    [Fact]
    public void TryWrap_NonPartialClass_EmitsAR001()
    {
        var diags = GetDiagnostics(@"
using AutoResult;
namespace MyApp
{
    [TryWrap]
    public class OrderService // NOT partial
    {
        public int Get() => 1;
    }
}");
        Assert.Contains(diags, d => d.Id == "AR001");
    }

    [Fact]
    public void TryWrap_NoMethods_EmitsAR002()
    {
        var diags = GetDiagnostics(@"
using AutoResult;
namespace MyApp
{
    [TryWrap]
    public partial class EmptyService { }
}");
        Assert.Contains(diags, d => d.Id == "AR002");
    }

    // ── Namespace handling ────────────────────────────────────────────────────
    [Fact]
    public void TryWrap_NamespacedClass_CorrectNamespace()
    {
        var src = RunGenerator(@"
using AutoResult;
namespace My.Deep.Namespace
{
    [TryWrap]
    public partial class Svc
    {
        public int Get() => 1;
    }
}");
        var code = src["AutoResult.Svc.g.cs"];
        Assert.Contains("namespace My.Deep.Namespace", code);
        Assert.Contains("public partial class Svc", code);
    }

    [Fact]
    public void TryWrap_CatchWrapsException()
    {
        var src = RunGenerator(@"
using AutoResult;
namespace MyApp
{
    [TryWrap]
    public partial class Svc
    {
        public int Get() => 1;
    }
}");
        var code = src["AutoResult.Svc.g.cs"];
        Assert.Contains("catch (Exception ex)", code);
        Assert.Contains(".Fail(ex)", code);
    }
}
