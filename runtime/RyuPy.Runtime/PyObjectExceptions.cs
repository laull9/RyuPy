// 原生类型及异常的属性读取；独立于普通实例描述符，保留异常对象身份。
namespace RyuPy.Runtime;

public static partial class PyObject
{
    private static object? GetTypeAttr(PyType type, string name) =>
        ReferenceEquals(type, PyBaseObject.Type) ? PyBaseObject.TypeAttribute(name)
        : ReferenceEquals(type, PyType.Get("dict")) && PyDictDescriptors.IsAttribute(name) ? PyDictDescriptors.Unbound(name)
        : ReferenceEquals(type, PyType.Get("list")) && PyListDescriptors.IsAttribute(name) ? PyListDescriptors.Unbound(name)
        : ReferenceEquals(type, PySlice.Type) ? PySliceMembers.TypeAttribute(name)
        : PyBinaryTranslateDescriptors.Has(type, name) ? PyBinaryTranslateDescriptors.Unbound(type)
        : PyBinaryClassificationDescriptors.Has(type, name) ? PyBinaryClassificationDescriptors.Unbound(type, name)
        : ReferenceEquals(type, PyType.Get("str")) && PyStringDescriptors.IsMethod(name)
            ? PyStringDescriptors.Unbound(name) : name switch
        {
            "__class__" => PyType.Get("type"), // PyType载体的真实元类固定点，不扩展实例反射。
            "__name__" or "__qualname__" when type.Name == "zlib.error" => "error",
            "__name__" or "__qualname__" => type.Name,
            "__module__" when type.Name == "zlib.error" => "zlib",
            "__module__" when type.Name == "BadGzipFile" => "gzip",
            "__module__" when type.Name == "JSONDecodeError" => "json.decoder",
            "__module__" when type.Name == "error" => "re",
            "__module__" when type.Name == "BadZipFile" => "zipfile",
            "__module__" when type.Name == "ParseError" => "xml.etree.ElementTree",
            "__module__" when type.Name == "SyntaxError" => "builtins",
            "__module__" when type.Name == "GetoptError" => "getopt",
            "msg" or "opt" when type.Name == "GetoptError" => "",
            "__module__" when PyOptParseExceptions.IsType(type.Name) => "optparse",
            "__module__" when PyTyping.IsMetadataType(type.Name) => "typing",
            "__module__" when type.Name == "ellipsis" => "builtins",
            _ => throw new PyAttributeError($"type object '{type.Name}' has no attribute '{name}'"),
        };

    private static object? GetExceptionAttr(PyBaseException exception, string name)
    {
        if (name == "__suppress_context__") return exception.SuppressContextFlag;
        if (name == "name" && exception is PyImportError importError) return ReadImportErrorName(importError);
        // 用户异常类上定义的方法 / 属性优先（`e.describe()` 之类）。
        if (exception.PythonClass is not null
            && exception.PythonClass.TryLookup(name, out PyClass owner, out object? raw))
        {
            return Bind(raw, owner, exception);
        }
        if (exception.PythonClass is not null
            && exception.UserAttributes.TryGetValue(name, out object? value))
        {
            return value;
        }
        return name switch
        {
            "name" or "path" when exception is PyImportError =>
                exception.UserAttributes.TryGetValue(name, out object? importValue) ? importValue : PyNone.Instance,
            "msg" when exception is PyRegexError regex => regex.MessageText,
            "pattern" when exception is PyRegexError regex => regex.Pattern,
            "pos" when exception is PyRegexError regex => regex.Position,
            "lineno" when exception is PyRegexError regex => regex.LineNumber,
            "colno" when exception is PyRegexError regex => regex.ColumnNumber,
            "msg" when exception is PyJSONDecodeError json => json.Msg,
            "doc" when exception is PyJSONDecodeError json => json.Doc,
            "pos" when exception is PyJSONDecodeError json => json.Pos,
            "lineno" when exception is PyJSONDecodeError json => json.LineNo,
            "colno" when exception is PyJSONDecodeError json => json.ColNo,
            "args" => new PyTuple(exception.Args),
            "value" when exception is PyStopIteration stop => stop.Value ?? PyNone.Instance,
            "code" when exception is PySystemExit exit => exit.Code ?? PyNone.Instance,
            "__cause__" => (object?)exception.Cause ?? PyNone.Instance,
            "__context__" => (object?)exception.Context ?? PyNone.Instance,
            "__class__" => (object?)exception.PythonClass ?? PyType.Get(exception.PyTypeName),
            "__traceback__" => PyNone.Instance,
            _ when exception is PySyntaxError syntax => syntax.Field(name),
            _ when exception is PyGetoptError getopt => getopt.Field(name),
            _ when exception is PyOptParseError error => error.Field(name),
            _ => throw new PyAttributeError(
                $"'{exception.PyTypeName}' object has no attribute '{name}'"
            ),
        };
    }

}
