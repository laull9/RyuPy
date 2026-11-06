// 六个 optparse 异常共享冷绑定；不改变其它内建异常和通用调用 ABI。
using System;
using System.Collections.Generic;
namespace RyuPy.Runtime;

/// <summary>固定optparse异常域的真实构造或类型入口。</summary>
public static class PyOptParseExceptions
{
    private static readonly PySignature Message = Signature("OptParseError", "msg");
    private static readonly PySignature Option = Signature("OptionError", "msg", "option");
    private static readonly PySignature Bad = Signature("BadOptionError", "opt_str");
    private static readonly PySignature Ambiguous = Signature("AmbiguousOptionError", "opt_str", "possibilities");
    private static PySignature Signature(string name, params string[] parameters)
    {
        var names = new string[parameters.Length + 1]; names[0] = "self";
        Array.Copy(parameters, 0, names, 1, parameters.Length);
        var kinds = new string[names.Length]; Array.Fill(kinds, "pos");
        var defaults = new string[names.Length]; Array.Fill(defaults, "0");
        return PySignature.Create(name + ".__init__", names, kinds, defaults);
    }
    /// <summary>固定optparse异常域的真实构造或类型入口。</summary>
    public static bool IsType(string name) => name is "OptParseError" or "OptionError" or "OptionConflictError"
        or "OptionValueError" or "BadOptionError" or "AmbiguousOptionError";
    /// <summary>绑定参数与原始位置 args 分离；参数检查先于用户 option.__str__。</summary>
    public static PyBaseException Construct(string name, object?[] args, string[] names, object?[] values)
    {
        PySignature signature = name switch
        {
            "OptParseError" or "OptionValueError" => Message,
            "OptionError" or "OptionConflictError" => Option,
            "BadOptionError" => Bad,
            "AmbiguousOptionError" => Ambiguous,
            _ => throw new PyNotImplementedError("unknown optparse exception type"),
        };
        object?[] supplied = new object?[args.Length + 1];
        supplied[0] = PyNone.Instance; Array.Copy(args, 0, supplied, 1, args.Length);
        object?[] bound;
        try { bound = PyBinder.Bind(signature, supplied, names, values, new object?[signature.Names.Length]); }
        catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
        object?[] original = (object?[])args.Clone();
        return name switch
        {
            "OptParseError" => new PyOptParseError(name, original, bound[1]),
            "OptionValueError" => new PyOptionValueError(original, bound[1]),
            "OptionError" => new PyOptionError(name, original, bound[1], bound[2]),
            "OptionConflictError" => new PyOptionConflictError(original, bound[1], bound[2]),
            "BadOptionError" => new PyBadOptionError(name, original, bound[1]),
            "AmbiguousOptionError" => new PyAmbiguousOptionError(original, bound[1], bound[2]),
            _ => throw new PyNotImplementedError("unknown optparse exception type"),
        };
    }
    internal static string RequireMessage(object? message) => message as string
        ?? throw (PyBaseException)PyErrors.PrepareRaise(new PyTypeError($"__str__ returned non-string (type {PyRepr.TypeName(message)})"));
    // 兼容消息工厂也必须服从 required 参数；双参数类型不能伪造 option。
    internal static PyBaseException FromMessage(string name, string message) => Construct(name, [message], [], []);
    internal static string DisplayName(PyBaseException error) => error is PyOptParseError
        ? "optparse." + error.PyTypeName : error.PyTypeName;
}
