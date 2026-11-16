// 实验records守卫：只完成精确内建成功路径；false从不调用用户协议，原MIR慢路负责一次回退。
using System.Runtime.CompilerServices;
namespace RyuPy.Runtime;

/// <summary>显式records实验的精确字典守卫；默认编译流水线不调用。</summary>
public static partial class PyRecordsIncrement
{
    // ephemeron弱键不延长dict生命周期；value引用owner由ConditionalWeakTable处理。
    // 不建立Python可见缓存，不以Count替代Version。外部变异沿原accessor预算失效。
    private static readonly ConditionalWeakTable<PyDict, PyDict.StringPairAccess> Accessors = new();

    /// <summary>尝试用原已物化键递增；拒绝时不执行用户协议或写入，由原MIR慢路继续。</summary>
    public static bool TryMaterialized(object? receiver, object? key)
    {
        bool result = receiver is PyDict dict && Accessors.GetValue(dict, static owner => new PyDict.StringPairAccess(owner))
            .TryIncrementMaterializedKey(key);
#if RECORDS_GUARD_DIAGNOSTICS
        Observe(result);
#endif
        return result;
    }
}
