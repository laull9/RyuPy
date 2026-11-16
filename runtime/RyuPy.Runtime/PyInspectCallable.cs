// 从真实可调用对象建立冷签名视图；每次读取当前默认值/注解，显式签名按身份返回。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>inspect可调用对象解析入口，供跨域适配器复用同一权威元数据。</summary>
public static class PyInspectCallable
{
    internal static object SignatureCall(object?[] a, string[] n, object?[] v)
    {
        var b = PyInspectModule.Arguments("signature", ["obj", "follow_wrapped", "globals", "locals", "eval_str"],
            ["pos", "kwonly", "kwonly", "kwonly", "kwonly"],
            [null, true, PyNone.Instance, PyNone.Instance, false], 1, a, n, v);
        return Signature(b[0], PyOps.Truthy(b[1]), PyOps.Truthy(b[4]));
    }
    /// <summary>建立当前签名快照；字符串求值和lazy annotations仍为显式边界。</summary>
    public static PyInspectSignature Signature(object? value, bool followWrapped = true, bool evalStr = false)
    {
        var signature = Resolve(value, followWrapped, new HashSet<object>(ReferenceEqualityComparer.Instance));
        if (evalStr)
        {
            if (signature.ReturnAnnotation is string) throw new PyNotImplementedError("inspect.signature eval_str with string annotations is not supported");
            foreach (var p in signature.Parameters)
                if (p.Annotation is string) throw new PyNotImplementedError("inspect.signature eval_str with string annotations is not supported");
        }
        return signature;
    }
    internal static object UnwrapCall(object?[] a, string[] n, object?[] v)
    {
        var b = PyInspectModule.Arguments("unwrap", ["func", "stop"], ["pos", "kwonly"], [null, PyNone.Instance], 1, a, n, v);
        return Unwrap(b[0], b[1])!;
    }
    private static object? Unwrap(object? value, object? stop)
    {
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        while (true)
        {
            if (value is not null && !seen.Add(value)) throw new PyValueError("wrapper loop when unwrapping");
            if (!PyInspectModule.IsNone(stop) && PyOps.Truthy(PyOps.CallValue(stop, [value], [], []))) return value;
            if (!PyInspectModule.OptionalAttr(value, "__wrapped__", out object? wrapped)) return value;
            value = wrapped;
        }
    }
    private static PyInspectSignature Resolve(object? value, bool follow, HashSet<object> seen)
    {
        if (!PyFunctools.IsCallable(value)) throw new PyTypeError($"{PyRepr.Repr(value)} is not a callable object");
        if (value is IPyForeignObject foreign && PyInspectForeign.Current is { } bridge) return bridge.Signature(foreign, follow);
        // 方法先解析底层显式签名，再移除接收者一次；自定义签名也遵循此规则。
        if (value is PyBoundMethod method) return DropReceiver(Resolve(method.Target, follow, seen));
        if (value is not null && !seen.Add(value)) throw new PyValueError("wrapper loop when unwrapping");
        bool explicitSignature = PyInspectModule.OptionalAttr(value, "__signature__", out object? custom);
        if (explicitSignature && !PyInspectModule.IsNone(custom))
            return custom as PyInspectSignature ?? throw new PyTypeError("unexpected object in __signature__ attribute");
        if (follow && !explicitSignature && PyInspectModule.OptionalAttr(value, "__wrapped__", out object? wrapped))
            return Resolve(wrapped, follow, seen);
        return value switch
        {
            PyFunction function => Function(function),
            PyPartial partial => Partial(partial, follow, seen),
            PyClass cls => Class(cls, follow, seen),
            PyInstance instance => Resolve(PyObject.GetAttr(instance, "__call__"), follow, seen),
            PyBuiltinFunction { Name: "len" } => new PyInspectSignature([P("obj", 0)], PyInspectModule.Empty),
            PyType { Name: "float" } => new PyInspectSignature([P("x", 0, 0.0)], PyInspectModule.Empty),
            PyType { Name: "object" } => new PyInspectSignature([], PyInspectModule.Empty),
            _ => throw new PyValueError($"no signature found for builtin {PyRepr.Repr(value)}"),
        };
    }
    // 读取默认值会建立既有冷反射state，使缺省直调守卫正确失效；不维护第二份函数元数据。
    private static PyInspectSignature Function(PyFunction function)
    {
        PySignature shape = function.Signature;
        var annotations = (PyDict)function.GetMetadata("__annotations__")!;
        var defaults = function.GetMetadata("__defaults__") as PyTuple;
        var keywords = function.GetMetadata("__kwdefaults__") as PyDict;
        int positional = 0;
        foreach (var kind in shape.Kinds) if (kind is PyParamKind.PositionalOnly or PyParamKind.PositionalOrKeyword) positional++;
        int firstDefault = positional - (defaults?.Length ?? 0); int index = 0;
        var parameters = new List<PyInspectParameter>();
        for (int i = 0; i < shape.Names.Length; i++)
        {
            string name = shape.Names[i]; int kind = (int)shape.Kinds[i]; object? defaultValue = PyInspectModule.Empty;
            if (kind is 0 or 1)
            { if (index >= firstDefault && defaults is not null) defaultValue = defaults.Items[index - firstDefault]; index++; }
            else if (kind == 3 && keywords is not null && keywords.TryGet(name, out object? kw)) defaultValue = kw;
            object? annotation = annotations.TryGet(name, out object? found) ? found : PyInspectModule.Empty;
            parameters.Add(new PyInspectParameter(name, PyInspectKind.Values[kind], defaultValue, annotation));
        }
        return new PyInspectSignature(parameters,
            annotations.TryGet("return", out object? result) ? result : PyInspectModule.Empty, false);
    }
    private static PyInspectSignature Class(PyClass cls, bool follow, HashSet<object> seen)
    {
        // 普通类沿MRO寻找实际声明的构造器，特殊原生/外部metaclass由各自域明确处理。
        if (cls.TryLookup("__new__", out _, out object? create))
        {
            if (create is PyStaticMethod staticMethod) create = staticMethod.Value;
            return DropReceiver(Resolve(create, follow, seen));
        }
        if (cls.TryLookup("__init__", out _, out object? init)) return DropReceiver(Resolve(init, follow, seen));
        return new PyInspectSignature([], PyInspectModule.Empty);
    }
    private static PyInspectSignature DropReceiver(PyInspectSignature signature)
    {
        if (signature.Parameters.Count == 0 || signature.Parameters[0].Kind.Value is 3 or 4)
            throw new PyValueError("invalid method signature");
        if (signature.Parameters[0].Kind.Value == 2) return signature;
        var result = new List<PyInspectParameter>(signature.Parameters); result.RemoveAt(0);
        return new PyInspectSignature(result, signature.ReturnAnnotation, false);
    }
    private static PyInspectSignature Partial(PyPartial partial, bool follow, HashSet<object> seen)
    {
        var signature = Resolve(partial.GetAttr("func"), follow, seen);
        var args = ((PyTuple)partial.GetAttr("args")!).Items; var keywords = (PyDict)partial.GetAttr("keywords")!;
        var names = new List<string>(); var values = new List<object?>();
        foreach (var (key, item) in keywords.Entries())
        { if (key is not string text) throw new PyValueError("partial object has incorrect arguments"); names.Add(text); values.Add(item); }
        PyInspectBoundArguments bound;
        try { bound = signature.Bind(args, names.ToArray(), values.ToArray(), true); }
        catch (PyTypeError) { throw new PyValueError("partial object has incorrect arguments"); }
        var result = new List<PyInspectParameter>(); bool keywordMode = false;
        var converted = new List<PyInspectParameter>(); var keywordOnly = new List<PyInspectParameter>(); PyInspectParameter? kwRest = null;
        foreach (var p in signature.Parameters)
        {
            bool provided = bound.Arguments.TryGet(p.Name, out object? item);
            if (p.Kind.Value == 4) { kwRest = p; continue; }
            if (provided && p.Kind.Value == 0) continue;
            if (p.Kind.Value == 1)
            {
                if (keywords.TryGet(p.Name, out object? supplied))
                { keywordMode = true; converted.Add(new PyInspectParameter(p.Name, PyInspectKind.Values[3], supplied, p.Annotation)); }
                else if (!provided)
                {
                    if (keywordMode) converted.Add(new PyInspectParameter(p.Name, PyInspectKind.Values[3], p.Default, p.Annotation));
                    else result.Add(p);
                }
            }
            else if (p.Kind.Value == 2) { if (!keywordMode) result.Add(p); }
            else if (p.Kind.Value == 3) keywordOnly.Add(provided ? new PyInspectParameter(p.Name, p.Kind, item, p.Annotation) : p);
            else result.Add(p);
        }
        result.AddRange(converted); result.AddRange(keywordOnly); if (kwRest is not null) result.Add(kwRest);
        return new PyInspectSignature(result, signature.ReturnAnnotation, false);
    }
    private static PyInspectParameter P(string name, int kind, object? value = null) =>
        new(name, PyInspectKind.Values[kind], value ?? PyInspectModule.Empty, PyInspectModule.Empty);
}

/// <summary>外部inspect适配器只在显式反射时参与，不进入普通调用热路。</summary>
public interface IPyInspectForeignBridge
{
    /// <summary>读取外部真实签名并保留其值身份。</summary>
    PyInspectSignature Signature(IPyForeignObject value, bool followWrapped);
}

/// <summary>组合根登记的inspect桥；纯原生运行没有外部依赖。</summary>
public static class PyInspectForeign
{
    /// <summary>入口安装与关闭时成对设置。</summary>
    public static IPyInspectForeignBridge? Current { get; set; }
}
