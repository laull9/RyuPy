// ImportError.name共享读取与显式消费边界；普通读取保留已有Bind能力，不扩setter/descriptor。
namespace RyuPy.Runtime;

public static partial class PyObject
{
    internal static object? ReadImportErrorName(PyImportError exception, bool requireComparableName = false)
    {
        object? raw = null;
        PyClass? owner = null;
        bool classValue = exception.PythonClass is not null
            && exception.PythonClass.TryLookup("name", out owner, out raw);
        // 相对导入只消费可安全比较的精确名称；此限制不能缩减普通公开读取。
        // 其他类项仍走既有Bind，只有显式有界消费者在回调前拒绝。
        if (classValue && raw is not (null or PyNone or string))
        {
            if (requireComparableName)
                throw new PyNotImplementedError("relative import: ImportError.name descriptors and non-string class values are not implemented");
            return Bind(raw, owner!, exception);
        }
        if (exception.UserAttributes.TryGetValue("name", out object? value)) return value;
        return classValue ? raw : PyNone.Instance;
    }
}
