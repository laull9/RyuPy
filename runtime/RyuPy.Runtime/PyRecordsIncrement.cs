// 实验records守卫：只完成精确内建成功路径；false从不调用用户协议，原MIR慢路负责一次回退。
using System.Runtime.CompilerServices;
namespace RyuPy.Runtime;

public static class PyRecordsIncrement
{
    // ephemeron弱键不延长dict生命周期；value引用owner由ConditionalWeakTable处理。
    // 不建立Python可见缓存，不以Count替代Version。外部变异沿原accessor预算失效。
    private static readonly ConditionalWeakTable<PyDict, PyDict.StringPairAccess> Accessors = new();

    public static bool TryMaterialized(object? receiver, object? key)
    {
        if (receiver is not PyDict dict) return false;
        return Accessors.GetValue(dict, static owner => new PyDict.StringPairAccess(owner))
            .TryIncrementMaterializedKey(key);
    }
}
