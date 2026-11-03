namespace RyuPy.Runtime;

public static partial class PyPic
{
    // 捕获目标允许任意Body保留args；非空数组必须独立，不复用站点scratch或池。
    /// <summary>零参捕获调用；普通接收者保持原PIC路径。</summary>
    public static object? CallCapturedMethod0(int site, bool captured, object? target, string name) =>
        captured ? PyOps.CallValue(target, [], [], []) : CallMethod0(site, target, name);

    /// <summary>单参捕获只对受控native标量入口免数组；任意Body保独立数组与重入。</summary>
    public static object? CallCapturedMethod1(int site, bool captured, object? target, string name, object? a0)
    {
        if (!captured) return CallMethod1(site, target, name, a0);
        if (!PyOps.IsStarredArgument(a0) && PyNativeFunctions.TryInvokeUnary(target, a0, out object? result))
            return result;
        return PyOps.CallValue(target, [a0], [], []);
    }

    /// <summary>双参捕获调用，不能缓存为某个包装类型的统一目标。</summary>
    public static object? CallCapturedMethod2(int site, bool captured, object? target, string name, object? a0, object? a1) =>
        captured ? PyOps.CallValue(target, [a0, a1], [], []) : CallMethod2(site, target, name, a0, a1);

    /// <summary>三参捕获调用；只有显式标志为真时按原属性值调用。</summary>
    public static object? CallCapturedMethod3(int site, bool captured, object? target, string name,
        object? a0, object? a1, object? a2) =>
        captured ? PyOps.CallValue(target, [a0, a1, a2], [], []) : CallMethod3(site, target, name, a0, a1, a2);
}
