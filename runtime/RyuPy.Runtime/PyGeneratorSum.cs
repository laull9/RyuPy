// 生成器批量sum的显式能力：只有编译器提供专用恢复入口的实例可用，旧构造ABI保持不变。
namespace RyuPy.Runtime;

/// <summary>生成代码在原yield边发布状态后尝试累计；失败须返回尚未相加的current。</summary>
internal delegate object? PyResumeSum(object frame, PyGeneratorSumState sum);

/// <summary>编译代码的批量恢复ABI；累计载体仅按不透明对象传递，不能据此构造或替换私有状态。</summary>
public delegate object? PyResumeSumCompiled(object frame, object sum);

/// <summary>一次批量恢复的私有累计值；不保存或替换Python原start对象。</summary>
internal sealed class PyGeneratorSumState
{
    /// <summary>精确long的当前总值，只有成功消费才更新。</summary>
    public long Total { get; private set; }
    /// <summary>即使增加零或正负抵消，也必须记录已进行加法，不能用Total变化推断dirty。</summary>
    public bool ConsumedAny { get; private set; }

    internal PyGeneratorSumState(long total) => Total = total;

    /// <summary>拒绝异构与溢出，拒绝项完全未相加；由外层真实sum处理。</summary>
    public bool TryConsume(object? value)
    {
        if (value is not long item) return false;
        long next = unchecked(Total + item);
        if (((Total ^ next) & (item ^ next)) < 0) return false;
        Total = next;
        ConsumedAny = true;
        return true;
    }
}

internal readonly record struct PyGeneratorSumResult(bool Completed, long Total, bool ConsumedAny);

public sealed partial class PyGenerator
{
    private PyResumeSum? _resumeSum;

    /// <summary>专用能力构造器；普通六参构造器不附带能力，此内部重载供协议测试。</summary>
    internal PyGenerator(PyResume resume, object payload, long pc, string source, string name,
        object? origin, PyResumeSum sumResume) : this(resume, payload, pc, source, name, origin)
    {
        _resumeSum = sumResume;
    }

    /// <summary>编译器提供经过MIR守卫的批量恢复入口；保留普通恢复入口供完整协议回退。</summary>
    public PyGenerator(PyResume resume, object payload, long pc, string source, string name,
        object? origin, PyResumeSumCompiled sumResume) : this(resume, payload, pc, source, name, origin)
    {
        // 包装只发生在构造时；私有累计状态仍由TrySumBatch逐次创建并管理。
        _resumeSum = (frame, sum) => sumResume(frame, sum);
    }

    // 返回false表示尚未执行任何恢复；true且非Completed表示Current恰好是未相加的一项。
    // 能力绑定实例，不重新查找函数/内建名。trace V1与已结束实例沿原路径，不重复状态机。
    internal bool TrySumBatch(long total, out PyGeneratorSumResult result)
    {
        result = default;
        if (_resumeSum is null || _state is null || PyTraceProduct.Enabled) return false;
        var sum = new PyGeneratorSumState(total);
        bool yielded = MoveNextCore(PyNone.Instance, out _, sum: sum);
        result = new PyGeneratorSumResult(!yielded, sum.Total, sum.ConsumedAny);
        return true;
    }
}


public static partial class PyOps
{
    /// <summary>只接受真实sealed range游标，不调用__iter__/__next__，失败时零消费。</summary>
    public static bool GeneratorSumRangeGuard(object? iterator) => iterator is PyRangeIter;

    /// <summary>生成代码在发布原yield状态后尝试累计；拒绝项必须原样返回给普通sum。</summary>
    public static bool GeneratorSumTryConsume(object sum, object? value) =>
        sum is PyGeneratorSumState state && state.TryConsume(value);
}
