namespace RyuPy.Runtime;

public static partial class PyOps
{
    // 仅精确内建容器，用户/native对象沿已有类方法路径，不绕过其自定义__len__。
    internal static bool IsBuiltinSized(object? receiver) => receiver is
        PyList or PyTuple or PyDict or PySet or PyFrozenSet or PyDictView or PyBytes or PyByteArray or string;

    private static object? PreparedLength(object receiver, string name, object?[] args, string[] names, object?[] values)
    {
        RejectPreparedKeywords(receiver, name, names);
        return PyInt.Box(Len(receiver));
    }
}
