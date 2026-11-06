// 可写 TypeVar 字段不参与隐式特殊协议；其余 native 维持既有显式 GetAttr 边界。
namespace RyuPy.Runtime;

public static partial class PyObject
{
    internal static bool TryGetNativeSpecial(PyNativeObject value, string name, out object? callable)
    {
        if (value is PyTypingTypeVar variable) return variable.TryGetSpecial(name, out callable);
        try { callable = value.GetAttr(name); return true; }
        catch (PyAttributeError) { callable = null; return false; }
    }
}
