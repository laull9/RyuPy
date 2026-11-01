using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>大字典字面量的运行时安全键守卫：只允许不会执行 Python hash/eq 回调的实际标量。</summary>
public static partial class PyOps
{
    /// <summary>key/value 已按源码顺序各求值一次；检测不调用用户协议，安全键才执行原插入。</summary>
    public static void DictSetScalarKey(object target, object? key, object? value)
    {
        if (key is not (null or PyNone or bool or int or long or BigInteger or double or string))
            throw new PyNotImplementedError("large dict literals with non-scalar keys are not supported");
        if (target is not PyDict dictionary) throw new PyTypeError("dict literal target must be a dict");
        dictionary.Set(key, value);
    }
}
