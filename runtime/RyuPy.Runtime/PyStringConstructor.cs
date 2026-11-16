// str的完整调用形态绑定与有限非解码转换；用户槽正文不置于绑定catch中。
using System;

namespace RyuPy.Runtime;

internal static class PyStringConstructor
{
    internal static object Invoke(object?[] args, string[] names, object?[] values) =>
        PyRepr.StrObject(Bind(args, 0, names, values));

    internal static object? Bind(object?[] args, int offset, string[] names, object?[] values)
    {
        int count = args.Length - offset;
        if ((long)count + names.Length > 3)
            throw Own(new PyTypeError($"str() takes at most 3 arguments ({(long)count + names.Length} given)"));
        object? input = count > 0 ? args[offset] : "";
        object? encoding = count > 1 ? args[offset + 1] : null;
        object? errors = count > 2 ? args[offset + 2] : null;
        bool hasObject = count > 0, hasEncoding = count > 1, hasErrors = count > 2;
        for (int index = 0; index < names.Length; index++)
        {
            string name = names[index]; object? value = values[index];
            switch (name)
            {
                case "object":
                    if (hasObject) throw Duplicate(name, 1);
                    hasObject = true; input = value; break;
                case "encoding":
                    if (hasEncoding) throw Duplicate(name, 2);
                    hasEncoding = true; encoding = value; break;
                case "errors":
                    if (hasErrors) throw Duplicate(name, 3);
                    hasErrors = true; errors = value; break;
                default: throw Own(new PyTypeError($"'{name}' is an invalid keyword argument for str()"));
            }
        }
        if (hasEncoding && !PyStringLayout.TryGet(encoding, out _))
            throw Own(new PyTypeError($"str() argument 'encoding' must be str, not {PyRepr.TypeName(encoding)}"));
        if (hasErrors && !PyStringLayout.TryGet(errors, out _))
            throw Own(new PyTypeError($"str() argument 'errors' must be str, not {PyRepr.TypeName(errors)}"));
        if (hasEncoding || hasErrors)
        {
            if (hasObject && PyStringLayout.TryGet(input, out _))
                throw Own(new PyTypeError("decoding str is not supported"));
            if (hasObject && input is not (PyBytes or PyByteArray or PyMemoryView))
                throw Own(new PyTypeError($"decoding to str: need a bytes-like object, {PyRepr.TypeName(input)} found"));
            throw Own(new PyNotImplementedError("str decoding with encoding or errors is not supported"));
        }
        return input;
    }

    private static PyTypeError Duplicate(string name, int position) => Own(new PyTypeError(
        $"argument for str() given by name ('{name}') and position ({position})"));
    private static T Own<T>(T error) where T : PyBaseException => PyStringLayout.Own(error);
}
