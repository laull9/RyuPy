// bytes/bytearray.decode统一参数绑定；严格编码限定在有限编码族，不调用字符串转换协议。
namespace RyuPy.Runtime;

internal static class PyBinaryDecode
{
    internal static object Invoke(object receiver, object?[] args, string[] names, object?[] values, int offset = 0)
    {
        try
        {
            int count = args.Length - offset;
            if (count + names.Length > 2) throw new PyTypeError($"decode() takes at most 2 arguments ({count + names.Length} given)");
            object? encoding = count > 0 ? args[offset] : "utf-8";
            object? errors = count > 1 ? args[offset + 1] : "strict";
            bool hasEncoding = count > 0, hasErrors = count > 1;
            for (int i = 0; i < names.Length; i++)
            {
                switch (names[i])
                {
                    case "encoding":
                        if (hasEncoding) throw new PyTypeError("argument for decode() given by name ('encoding') and position (1)");
                        encoding = values[i]; hasEncoding = true; break;
                    case "errors":
                        if (hasErrors) throw new PyTypeError("argument for decode() given by name ('errors') and position (2)");
                        errors = values[i]; hasErrors = true; break;
                    default: throw new PyTypeError($"'{names[i]}' is an invalid keyword argument for decode()");
                }
            }
            string codec = Text(encoding, "encoding", 1);
            string policy = Text(errors, "errors", 2);
            // 自定义处理器及非strict策略尚未实现，显式拒绝，不伪装成丢弃/替换成功。
            if (policy != "strict") throw new PyNotImplementedError("decode currently supports only errors='strict'");
            return receiver is PyBytes bytes ? PyEncoding.Decode(bytes.Bytes, codec)
                : PyEncoding.Decode([.. ((PyByteArray)receiver).Bytes], codec);
        }
        catch (PyBaseException error)
        {
            PyErrors.PrepareRaise(error);
            throw;
        }
    }

    private static string Text(object? value, string name, int position)
    {
        if (value is not string text) throw new PyTypeError($"decode() argument '{name}' must be str, not {PyRepr.TypeName(value)}");
        if (text.Contains('\0')) throw new PyValueError($"embedded null character in argument {position}");
        return text;
    }

    internal static PyBaseException Error(PyBaseException error) => (PyBaseException)PyErrors.PrepareRaise(error);
}
