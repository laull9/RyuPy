// 已捕获绑定函数的位置参数合并：保留每次独立数组和目标快照，不复用PIC scratch。
namespace RyuPy.Runtime;

public static partial class PyPic
{
    // 捕获目标允许任意Body保留args；非空数组必须独立，不复用站点scratch或池。
    /// <summary>零参捕获调用；普通接收者保持原PIC路径。</summary>
    public static object? CallCapturedMethod0(int site, bool captured, object? target, string name)
    {
        if (!captured) return CallMethod0(site, target, name);
        if (target is PyBoundMethod { Target: PyFunction function } bound)
            return PyObject.InvokePrepared(function, bound.Owner, bound.Self, [bound.Self], [], []);
        return PyOps.CallValue(target, [], [], []);
    }

    /// <summary>单参捕获只对受控native标量入口免数组；任意Body保独立数组与重入。</summary>
    public static object? CallCapturedMethod1(int site, bool captured, object? target, string name, object? a0)
    {
        if (!captured) return CallMethod1(site, target, name, a0);
        if (!PyOps.IsStarredArgument(a0) && PyNativeFunctions.TryInvokeUnary(target, a0, out object? result))
            return result;
        // 只合并普通位置参数；星号仍由通用展开协议负责。
        if (!PyOps.IsStarredArgument(a0) && target is PyBoundMethod { Target: PyFunction function } bound)
            return PyObject.InvokePrepared(function, bound.Owner, bound.Self, [bound.Self, a0], [], []);
        return PyOps.CallValue(target, [a0], [], []);
    }

    /// <summary>双参捕获调用，不能缓存为某个包装类型的统一目标。</summary>
    public static object? CallCapturedMethod2(int site, bool captured, object? target, string name, object? a0, object? a1)
    {
        if (!captured) return CallMethod2(site, target, name, a0, a1);
        if (!PyOps.IsStarredArgument(a0) && !PyOps.IsStarredArgument(a1)
            && target is PyBoundMethod { Target: PyFunction function } bound)
            return PyObject.InvokePrepared(function, bound.Owner, bound.Self, [bound.Self, a0, a1], [], []);
        return PyOps.CallValue(target, [a0, a1], [], []);
    }

    /// <summary>三参捕获调用；只有显式标志为真时按原属性值调用。</summary>
    public static object? CallCapturedMethod3(int site, bool captured, object? target, string name,
        object? a0, object? a1, object? a2)
    {
        if (!captured) return CallMethod3(site, target, name, a0, a1, a2);
        if (!PyOps.IsStarredArgument(a0) && !PyOps.IsStarredArgument(a1) && !PyOps.IsStarredArgument(a2)
            && target is PyBoundMethod { Target: PyFunction function } bound)
            return PyObject.InvokePrepared(function, bound.Owner, bound.Self, [bound.Self, a0, a1, a2], [], []);
        return PyOps.CallValue(target, [a0, a1, a2], [], []);
    }
}
