// 函数定义来源只随共享签名冷登记；限定名改写另存弱键表，不增加函数实例字段或调用开销。
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RyuPy.Runtime;

internal static class PyFunctionDefinition
{
    // 编译器提供真实词法名与源码首行；运行时不得用可变名字或函数序号推测来源。
    private sealed class DefinitionInfo(string? lexicalQualname, long firstLine, CompiledCodeSource? source = null)
    {
        internal readonly string? LexicalQualname = lexicalQualname;
        internal readonly long FirstLine = firstLine;
        internal readonly CompiledCodeSource? Source = source;
    }

    private sealed class QualnameOverride(string value)
    {
        internal string Value = value;
    }

    private static readonly ConditionalWeakTable<PySignature, DefinitionInfo> Definitions = new();
    // 连覆盖表本身也延迟到第一次实际赋值，只有定义登记的程序不分配它。
    private static ConditionalWeakTable<PyFunction, QualnameOverride>? Qualnames;

    internal static void Register(PySignature signature, string qualname, long firstLine)
    {
        if (qualname is null) throw new PyTypeError("function definition qualname must be a string");
        if (firstLine < 1) throw new PyValueError("function definition first line must be positive");
        try { Definitions.Add(signature, new DefinitionInfo(qualname, firstLine)); }
        catch (ArgumentException)
        {
            // 即使内容相同也拒绝第二次登记，防止后端悄悄复用错误的签名身份。
            throw new PyRuntimeError("function definition is already registered");
        }
    }

    internal static void RegisterSource(PySignature signature, string? qualname, long firstLine, object source)
    {
        var code = CompiledCodeSource.Require(source);
        // 合成signature可只有代码关联；不为它伪造词法限定名或首行。
        if (qualname is null ? firstLine != 0 : firstLine < 1)
            throw new PyValueError("invalid source definition lexical metadata");
        try { Definitions.Add(signature, new DefinitionInfo(qualname, firstLine, code)); }
        catch (ArgumentException) { throw new PyRuntimeError("function definition is already registered"); }
    }

    internal static CompiledCodeSource? Source(PySignature signature) =>
        Definitions.TryGetValue(signature, out DefinitionInfo? info) ? info.Source : null;

    internal static string GetQualname(PyFunction function)
    {
        if (Qualnames is { } overrides && overrides.TryGetValue(function, out QualnameOverride? value)) return value.Value;
        if (Definitions.TryGetValue(function.Signature, out DefinitionInfo? info)
            && info.LexicalQualname is { } name) return name;
        throw new PyNotImplementedError("function lexical qualname is not available");
    }

    internal static void SetQualname(PyFunction function, object? value)
    {
        // 真str子类需要保留原对象身份；本切不剥payload，也不假报成非字符串。
        if (value is PyStringInstance)
            throw new PyNotImplementedError("function qualname with str subclass identity is not supported");
        if (value is not string name) throw new PyTypeError("__qualname__ must be set to a string object");
        // 命中已有覆盖时直接改值，回调闭包必须隔离到miss helper，避免反复写也分配。
        var overrides = LazyInitializer.EnsureInitialized(ref Qualnames);
        if (overrides.TryGetValue(function, out QualnameOverride? existing))
        {
            existing.Value = name;
            return;
        }
        SetFirstQualname(overrides, function, name);
    }

    // 保留CWT的并发弱键语义；只有首次写入miss才创建捕获字符串的工厂回调。
    private static void SetFirstQualname(ConditionalWeakTable<PyFunction, QualnameOverride> overrides,
        PyFunction function, string name) =>
        overrides.GetValue(function, _ => new QualnameOverride(name)).Value = name;

    internal static long GetFirstLine(PySignature signature) =>
        Definitions.TryGetValue(signature, out DefinitionInfo? info) && info.FirstLine > 0 ? info.FirstLine
            : throw new PyNotImplementedError("function definition first line is not available");
}

public sealed partial class PyFunction
{
    /// <summary>内部消费者只读真实定义行号；未知来源明确失败，不公开代码对象。</summary>
    internal long DefinitionFirstLine => PyFunctionDefinition.GetFirstLine(Signature);
}

public static partial class PyOps
{
    /// <summary>新版冷登记：每签名一次关联实际代码，不修改旧函数实例/调用ABI。</summary>
    public static void FunctionDefinitionWithSource(object signature, string? lexicalQualname, long firstLine, object source)
    {
        if (signature is not PySignature target)
            throw new PyTypeError("function definition target must be a signature");
        PyFunctionDefinition.RegisterSource(target, lexicalQualname, firstLine, source);
    }

    /// <summary>每个共享签名初始化时冷登记一次不可变定义来源，不改变真实def的求值顺序。</summary>
    public static void FunctionDefinition(object signature, string qualname, long firstlineno)
    {
        if (signature is not PySignature target)
            throw new PyTypeError("function definition target must be a signature");
        PyFunctionDefinition.Register(target, qualname, firstlineno);
    }
}
