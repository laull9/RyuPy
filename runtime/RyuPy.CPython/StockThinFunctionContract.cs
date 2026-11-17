// ADR-0226 的候选中性合同：静态 core 与 owned F 分离；尚无 Host 注册或产品入口。
// 类型暂存 provider 内部，接线验收后再决定跨程序集 ABI，避免提前增加 Runtime 公共表面。
using System;

namespace RyuPy.CPython;

// 只保存不可变来源字符串与编译器身份；禁止放入模块 context、委托 owner 或 CP owned 对象。
internal sealed record StockThinFunctionDefinition(string EntryId, string Module, string Name,
    string SourcePath, int SourceLine);

// 参数已经按固定 f(x) 形状绑定；对象保持原身份，不能仿照 proof 强制转成 Int64。
internal delegate object? StockThinFunctionBody(IStockThinFunctionLease environment, object? argument);

// 实现仅在 provider 中：活动期间持 GIL、独立保活真实 F；D/builtins 从该 F 读取。
// 返回的值必须完成 owned 转换；绝不能把 borrowed CP 指针跨到用户回调之后。
internal interface IStockThinFunctionLease : IDisposable
{
    // 每次进入都验证宿主开放、F/core/glue/closure/默认值形状；失败抛明确能力异常。
    // 读取 __name__/__qualname__/__module__/annotations 等反射值时仍从 F 读取，不缓存副本。
    void Validate(StockThinFunctionDefinition definition);
    object? LoadGlobal(string name);
}

// owner 是 H 私有的 owned F；终结器只把引用交给宿主释放队列，不调用 C API。
// AcquireCall 先阻止关闭竞争，再取 GIL 与独立活动引用；失败自行回滚所有部分资源。
internal interface IStockThinFunctionOwner
{
    IStockThinFunctionLease AcquireCall();
}

// 仅 H -> owner -> owned F 是强边。core 只能弱持 H，不能缓存 owner/lease/执行结果。
internal sealed class StockThinFunctionCore
{
    internal StockThinFunctionDefinition Definition { get; }
    private readonly StockThinFunctionBody body;
    private WeakReference<StockThinFunctionHandle>? publicHandle;

    internal StockThinFunctionCore(StockThinFunctionDefinition definition, StockThinFunctionBody body)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(body);
        // 单一无捕获静态目标：连无状态实例方法和多播委托也不接纳，防止隐藏 owner 强回边。
        if (body.Target is not null || !body.Method.IsStatic || body.GetInvocationList().Length != 1)
            throw new ArgumentException("薄函数 core 只接受单一静态 CIL 目标", nameof(body));
        Definition = definition;
        this.body = body;
    }

    // 调用者必须在同一解释器 GIL 下完成原生 F 身份核验后进入；不以可复用地址作缓存键。
    // factory 只在缓存失效时消费一份新 owned F；命中路径不分配第二个 owner。
    internal StockThinFunctionHandle AcquirePublicHandle(Func<IStockThinFunctionOwner> factory)
    {
        if (publicHandle is not null && publicHandle.TryGetTarget(out var cached)) return cached;
        var handle = new StockThinFunctionHandle(this, factory());
        publicHandle = new(handle);
        return handle;
    }

    internal object? Invoke(IStockThinFunctionLease environment, object? argument)
    {
        environment.Validate(Definition);
        return body(environment, argument);
    }
}

internal sealed class StockThinFunctionHandle(StockThinFunctionCore core, IStockThinFunctionOwner owner)
{
    // 这只是未来直达适配入口；provider 未接线，不能从生产 PyFunction 或 ToPython 调用。
    // tracing/profiling 活跃时 provider 必须走真实 F，并由同一 core.Invoke 完成二次形状核验。
    internal object? Invoke(object? argument)
    {
        try
        {
            using var lease = owner.AcquireCall();
            return core.Invoke(lease, argument);
        }
        finally { GC.KeepAlive(this); }
    }
}
