// optparse 独立类型与阶段入口；不借用 argparse 对象，不把未完成的解析/显示伪装成成功。
using System;
using System.Collections.Generic;
namespace RyuPy.Runtime;

/// <summary>有限 optparse 的真实对象工厂。</summary>
public static class PyOptparse
{
    internal static readonly string SuppressUsage = "SUPPRESSUSAGE", SuppressHelp = "SUPPRESSHELP";
    internal static readonly PyTuple NoDefault = new(["NO", "DEFAULT"]);
    internal static readonly PyNativeType OptionType = new("Option", "optparse", PyOptparseOption.Construct, x => x is PyOptparseOption);
    internal static readonly PyNativeType ValuesType = new("Values", "optparse", PyOptparseValues.Construct, x => x is PyOptparseValues);
    internal static readonly PyNativeType ParserType = new("OptionParser", "optparse", PyOptparseParser.Construct, x => x is PyOptparseParser);
    internal static readonly string[] ErrorNames = ["OptParseError", "OptionError", "OptionConflictError", "OptionValueError", "BadOptionError", "AmbiguousOptionError"];

    /// <summary>创建模块属性；六异常由同一真实异常注册表提供。</summary>
    public static Dictionary<string, object?> CreateAttributes()
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["OptionParser"] = ParserType, ["Option"] = OptionType, ["Values"] = ValuesType,
            ["NO_DEFAULT"] = NoDefault, ["SUPPRESS_HELP"] = SuppressHelp, ["SUPPRESS_USAGE"] = SuppressUsage,
        };
        foreach (string name in ErrorNames) result[name] = PyType.Get(name);
        return result;
    }
    internal static PyBaseException Error(string name, params object?[] args) =>
        (PyBaseException)PyErrors.PrepareRaise(PyOptParseExceptions.Construct(name, args, [], []));
    internal static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
    internal static PyNotImplementedError Unsupported(string feature) => Own(new PyNotImplementedError("optparse " + feature + " is not supported"));
    internal static bool None(object? value) => value is null or PyNone;
    internal static string Text(object? value) => value as string ?? throw Unsupported("non-string metadata protocol");
    internal static object? Call(object? function, object?[] args, string[] names, object?[] values) =>
        PyOps.CallValue(function, args, names, values);
    internal static string ReprIdentity(object value, string name, string display) =>
        "<" + name + " at 0x" + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value).ToString("x") + ": " + display + ">";
}
