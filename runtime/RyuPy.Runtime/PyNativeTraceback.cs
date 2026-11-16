// Python可观察的原生traceback节点与冷frame/code视图；行号来自真实传播点，身份来自真实激活。
using System;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>真实异常传播链节点；tb_next可写并拒绝循环，节点行号不随后续恢复改变。</summary>
public sealed class PyNativeTraceback : PyNativeObject
{
    internal PyNativeFrame Frame { get; }
    internal int Line { get; }
    internal PyNativeTraceback? Next { get; private set; }
    internal PyNativeTraceback(PyNativeFrame frame, int line, PyNativeTraceback? next)
    { Frame = frame; Line = line; Next = next; }
    /// <inheritdoc/>
    public override string TypeName => "traceback";
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "tb_next" => (object?)Next ?? PyNone.Instance,
        "tb_lineno" => (long)Line,
        "tb_frame" => Frame,
        "tb_lasti" => throw new PyNotImplementedError("native traceback has no CPython bytecode offset"),
        "__class__" => PyType.Get("traceback"),
        _ => throw Missing(name),
    };
    /// <inheritdoc/>
    public override void SetAttr(string name, object? value)
    {
        if (name != "tb_next") throw new PyAttributeError("readonly attribute");
        if (value is not (null or PyNone or PyNativeTraceback))
            throw new PyTypeError($"expected traceback object, got '{PyRepr.TypeName(value)}'");
        var next = value as PyNativeTraceback;
        for (var node = next; node is not null; node = node.Next)
            if (ReferenceEquals(node, this)) throw new PyValueError("traceback loop detected");
        Next = next;
    }
}

/// <summary>同一逻辑激活共享一个frame视图；普通调用仅在实际观察时分配。</summary>
internal sealed class PyNativeFrame : PyNativeObject
{
    private readonly PyNativeCode? _code;
    internal int Line;
    internal PyNativeFrame(string file, string function, int line, object? origin)
    { Line = line; _code = PyNativeCode.For(file, function, origin); }
    /// <inheritdoc/>
    public override string TypeName => "frame";
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "f_code" => _code ?? throw new PyNotImplementedError("native frame code origin is not registered"),
        "f_lineno" => (long)PyTraceback.NativeFrameLine(this),
        "__class__" => PyType.Get("frame"),
        "f_lasti" or "f_locals" or "f_globals" or "f_builtins" or "f_back" or "f_trace"
            => throw new PyNotImplementedError($"native frame field {name} is not supported"),
        _ => throw Missing(name),
    };
}

/// <summary>只暴露真实编译来源，未知字节码字段明确拒绝。</summary>
internal sealed class PyNativeCode(string file, string function) : PyNativeObject
{
    private static readonly ConditionalWeakTable<object, PyNativeCode> Codes = new();
    internal static PyNativeCode? For(string file, string function, object? origin)
    {
        // 只有受信编译登记保证signature唯一映射函数体；宿主手造共享signature不构成证明。
        object? key = origin is PyFunction callable ? callable.Signature : origin;
        CompiledCodeSource? source = key switch
        {
            PySignature signature => PyFunctionDefinition.Source(signature),
            ModuleExecutionSource module => module.Source,
            _ => null,
        };
        if (source is null) return null;
        if (!String.Equals(source.File, file, StringComparison.Ordinal))
            throw new PyRuntimeError("traceback frame and compiled origin disagree");
        return Codes.GetValue(key!, _ => new(file, function));
    }
    /// <inheritdoc/>
    public override string TypeName => "code";
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "co_name" => function,
        "co_filename" => file,
        "__class__" => PyType.Get("code"),
        _ => throw new PyNotImplementedError($"native code field {name} is not supported"),
    };
}
