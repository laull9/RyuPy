// 普通内建异常的真实类型调用：工厂保持CLR catch身份，原始位置参数延迟格式化。
namespace RyuPy.Runtime;

internal static class PyExceptionConstructors
{
    internal static PyBaseException Create(string name, object?[] args, string[] keywords)
    {
        if (name == "IncompleteRead")
        {
            if (keywords.Length != 0 || args.Length is < 1 or > 2 || args[0] is not PyBytes partial)
                throw new PyTypeError("IncompleteRead() requires bytes partial and optional expected");
            return new PyIncompleteRead(partial.Bytes, args.Length == 2 && !PyUrlParse.None(args[1]) ? PyIO.Index(args[1]) : null);
        }
        if (name == "URLError")
        {
            if (keywords.Length != 0 || args.Length != 1) throw new PyTypeError("URLError() requires one reason argument");
            return new PyURLError(args[0]);
        }
        if (name == "HTTPError")
        {
            if (keywords.Length != 0 || args.Length != 5) throw new PyTypeError("HTTPError() requires url, code, msg, hdrs, fp");
            if (!PyUrlParse.None(args[4]) && args[4] is not PyHttpResponse)
                throw new PyNotImplementedError("HTTPError fp must be None or an HTTPResponse");
            return new PyHTTPError(PyUrlParse.Text(args[0]), PyIO.Index(args[1]), args[2], args[3], args[4] as PyHttpResponse);
        }
        // 这些专有构造协议尚未接入；不能把五参Unicode错误或errno选择伪装成普通args。
        if (name is "UnicodeDecodeError" or "UnicodeEncodeError")
            throw new PyNotImplementedError($"{name} dynamic construction is not implemented");
        if (PyExceptionHierarchy.IsSubclass(name, "OSError") && args.Length > 1)
            throw new PyNotImplementedError("dynamic OSError errno/filename construction is not implemented");
        if (keywords.Length != 0)
        {
            if (name is "ImportError" or "ModuleNotFoundError" or "NameError" or "AttributeError")
                throw new PyNotImplementedError($"{name} keyword construction is not implemented");
            throw new PyTypeError($"{name}() takes no keyword arguments");
        }
        PyBaseException error = name switch
        {
            // Code和Value也必须保原对象；多参SystemExit.code为tuple，StopIteration.value为首参。
            "SystemExit" => new PySystemExit(args.Length == 0 ? null : args.Length == 1 ? args[0] : new PyTuple((object?[])args.Clone())),
            "StopIteration" => new PyStopIteration(args.Length == 0 ? null : args[0]),
            _ => PyExceptionTypes.Create(name, ""),
        };
        error.InitializePositionalArguments(args);
        return error;
    }
}
