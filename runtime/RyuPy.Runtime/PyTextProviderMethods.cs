// 按 API 显式借助已选 CPython provider 补齐文本方法，Unicode 规则由随包解释器执行。
namespace RyuPy.Runtime;

internal static class PyTextProviderMethods
{
    internal static object? Title(string text, object?[] args, string[] names, object?[] values)
    {
        var provider = PyProvider.Current ?? throw new PyAttributeError("'str' object has no attribute 'title'");
        object? type = PyObject.GetAttr(provider.Import("builtins"), "str");
        object? method = PyObject.GetAttr(type, "title");
        object?[] forwarded = new object?[args.Length + 1];
        forwarded[0] = text;
        System.Array.Copy(args, 0, forwarded, 1, args.Length);
        return PyOps.CallValue(method, forwarded, names, values);
    }
}
