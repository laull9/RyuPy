// 外部回调的借参数标记；只在已登记的静态 CIL 槽中流动，Python 读取得到普通真实值。
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>供 provider 实现的回调同步借值，不是可见 Python 对象。</summary>
public abstract class PyDeferredCallbackArgument
{
    private object? value;
    private bool resolved;

    /// <summary>编译器发射到真实参数的读点；普通托管直调用值不变。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? Read(object? slot) =>
        slot is PyDeferredCallbackArgument deferred ? deferred.Resolve() : slot;

    /// <summary>只有显式生成了读点的 owned 函数可接收固定位置借参数。</summary>
    public static bool Accepts(PyFunction function) =>
        function.Body.Target is PyFunctionOwner { DeferredArguments: true }
        && function.Signature.SimplePositional;

    private object? Resolve()
    {
        if (resolved) return value;
        value = Materialize();
        resolved = true;
        return value;
    }

    /// <summary>在借用期内生成正常的、可逃逸的 provider 值。</summary>
    protected abstract object? Materialize();
}
