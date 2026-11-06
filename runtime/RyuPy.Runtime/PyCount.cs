// count 的真实惰性状态：普通整数 +1 使用 long 游标，越界转回现有任意精度加法。
// 慢路先完成一次加法才发布旧值；失败不推进，不物化、不以乘法重排浮点累加。
using System;

namespace RyuPy.Runtime;

internal sealed class PyCount : PyToolIterator
{
    private static readonly Lazy<PyNativeType> NativeType = new(() => new PyNativeType(
        "count", "itertools", PyCountBinding.Create, value => value is PyCount));
    private readonly object step;
    private readonly bool omitStep;
    private object nextValue;
    private object? current;
    private long nextInteger;
    private bool integerFast;

    internal PyCount(object start, object step)
    {
        this.step = step;
        omitStep = PyCountBinding.IsIntegerOne(step);
        integerFast = omitStep && PyCountBinding.TryFastStart(start, out nextInteger);
        // 快路不保留原 bool/整数对象；慢路必须保留 start 与 step 的原引用。
        nextValue = integerFast ? PyNone.Instance : start;
    }

    internal static PyNativeType Type => NativeType.Value;
    public override string TypeName => "itertools.count";

    public override object? GetAttr(string name) => name switch
    {
        "__class__" => Type,
        "__iter__" or "__next__" => base.GetAttr(name),
        "__reduce__" or "__reduce_ex__" or "__setstate__" => throw PyCountBinding.Unsupported("pickle state protocol"),
        _ => throw PyCountBinding.Error(Missing(name)),
    };

    public override void SetAttr(string name, object? value) => RejectMutation(name);
    public override void DelAttr(string name) => RejectMutation(name);
    private void RejectMutation(string name)
    {
        if (name is "__class__" or "__iter__" or "__next__" or "__reduce__" or "__reduce_ex__" or "__setstate__")
            throw PyCountBinding.Unsupported("changing iterator protocol attributes");
        throw PyCountBinding.Error(Missing(name));
    }

    public override bool MoveNext()
    {
        if (!integerFast) return AdvanceObject();
        object old = PyInt.Box(nextInteger);
        if (nextInteger == long.MaxValue)
        {
            // 先计算成功再切换模式；不回绕、不因机器整数边界耗尽。
            object following = PyInt.AddI64(nextInteger, 1);
            nextValue = following;
            integerFast = false;
        }
        else nextInteger++;
        current = old;
        return true;
    }

    private bool AdvanceObject()
    {
        object old = nextValue;
        object following;
        try { following = PyOps.Binary(PyOps.Add, old, step); }
        catch (PyBaseException error) { throw PyCountBinding.Error(error); }
        // Current 属于最近一次成功推进，算术失败不得清空它或修改下一值。
        nextValue = following;
        current = old;
        return true;
    }

    public override object? Current() => current;

    public override string PythonRepr()
    {
        object next = integerFast ? PyInt.Box(nextInteger) : nextValue;
        return omitStep ? $"count({PyRepr.Repr(next)})"
            : $"count({PyRepr.Repr(next)}, {PyRepr.Repr(step)})";
    }
}
