// 可写 TypeVar 和生成器上下文字段不参与隐式特殊协议；其余 native 维持既有类型入口。
namespace RyuPy.Runtime;

public static partial class PyObject
{
    internal static bool TryGetNativeSpecial(PyNativeObject value, string name, out object? callable)
    {
        if (value is PyTypingTypeVar variable) return variable.TryGetSpecial(name, out callable);
        try { callable = value is PyGeneratorContext context ? context.GetTypeMethod(name) : value.GetAttr(name); return true; }
        catch (PyAttributeError) { callable = null; return false; }
    }
}
