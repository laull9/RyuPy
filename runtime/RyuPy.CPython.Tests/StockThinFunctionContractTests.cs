// ADR-0226 的纯托管协议测试：覆盖静态目标、回流缓存、验证门及异常释放；不加载解释器。
using System.Runtime.CompilerServices;
using RyuPy.Runtime;

namespace RyuPy.CPython.Tests;

public sealed class StockThinFunctionContractTests
{
    private static StockThinFunctionCore NewCore() => new(
        new("test:read", "subject", "read", "subject.py", 3), Read);
    private static object? Read(IStockThinFunctionLease environment, object? argument) =>
        environment.LoadGlobal("answer");
    private static object? ReadCompiled(IPyStockThinEnvironment environment, object? argument) =>
        PyStockThinFunctions.LoadGlobal(environment, "answer");

    [Fact]
    public void CompiledNeutralBodyKeepsStaticTargetAndUsesOnlyTheActiveLease()
    {
        var owner = new Owner();
        var core = StockThinFunctionCore.CreateCompiled(NewCore().Definition, ReadCompiled);
        var handle = core.AcquirePublicHandle(() => owner);
        Assert.Same(owner.Answer, handle.Invoke(null));
        Assert.Equal(1, owner.Validations);
        Assert.Equal(1, owner.Disposals);
        owner.Answer = new object();
        Assert.Same(owner.Answer, handle.Invoke(null));
        Assert.Throws<ArgumentException>(() => StockThinFunctionCore.CreateCompiled(core.Definition,
            (_, _) => owner.Answer));
        PyStockThinBody target = ReadCompiled;
        Assert.Throws<ArgumentException>(() => StockThinFunctionCore.CreateCompiled(core.Definition, target + target));
    }

    [Fact]
    public void CoreRejectsCapturedAndMulticastTargets()
    {
        var captured = new Owner();
        var definition = NewCore().Definition;
        Assert.Throws<ArgumentException>(() => new StockThinFunctionCore(definition,
            (_, _) => captured.Answer));
        StockThinFunctionBody target = Read;
        Assert.Throws<ArgumentException>(() => new StockThinFunctionCore(definition, target + target));
    }

    [Fact]
    public void SameCoreReturnsSameLiveHandleAndDoesNotAllocateAnotherOwner()
    {
        var core = NewCore();
        var owner = new Owner();
        var handle = core.AcquirePublicHandle(() => owner);
        Assert.Same(handle, core.AcquirePublicHandle(() => throw new Exception("重复 owner")));
        Assert.Same(owner.Answer, handle.Invoke(null));
        owner.Answer = new object();
        Assert.Same(owner.Answer, handle.Invoke(null));
        Assert.Equal(2, owner.Validations);
        Assert.Equal(2, owner.Disposals);
    }

    [Fact]
    public void AlreadyHeldHandleRevalidatesMutationBeforeBodyAndReleasesLease()
    {
        var core = NewCore();
        var owner = new Owner { Invalid = true };
        var handle = core.AcquirePublicHandle(() => owner);
        Assert.Throws<NotSupportedException>(() => handle.Invoke(null));
        Assert.Equal(0, owner.Reads);
        Assert.Equal(1, owner.Disposals);
        owner.Invalid = false;
        Assert.Same(owner.Answer, handle.Invoke(null));
    }

    [Fact]
    public void OwnerFactoryFailureLeavesWeakCacheAvailableForRetry()
    {
        var core = NewCore();
        Assert.Throws<InvalidOperationException>(() => core.AcquirePublicHandle(
            static () => throw new InvalidOperationException("owner 创建失败")));
        var owner = new Owner();
        Assert.Same(owner.Answer, core.AcquirePublicHandle(() => owner).Invoke(null));
    }

    [Fact]
    public void BodyExceptionKeepsIdentityAndDisposesLease()
    {
        var owner = new Owner { Failure = new InvalidOperationException("同一个异常") };
        var handle = NewCore().AcquirePublicHandle(() => owner);
        Assert.Same(owner.Failure, Assert.Throws<InvalidOperationException>(() => handle.Invoke(null)));
        Assert.Equal(1, owner.Disposals);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference MakeUnrootedHandle(StockThinFunctionCore core) =>
        new(core.AcquirePublicHandle(static () => new Owner()));

    [Fact]
    public void CoreOnlyWeaklyCachesHandleAndCanRecreateAfterCollection()
    {
        var core = NewCore();
        var weak = MakeUnrootedHandle(core);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.False(weak.IsAlive);
        var owner = new Owner();
        Assert.Same(owner.Answer, core.AcquirePublicHandle(() => owner).Invoke(null));
        GC.KeepAlive(core);
    }

    // 这里只模拟 lease 协议；真实 owned F/GIL/原生析构由后续产品集成门验证。
    private sealed class Owner : IStockThinFunctionOwner
    {
        internal object Answer = new();
        internal bool Invalid;
        internal InvalidOperationException? Failure;
        internal int Validations, Disposals, Reads;
        public IStockThinFunctionLease AcquireCall() => new Lease(this);
        private sealed class Lease(Owner owner) : IStockThinFunctionLease
        {
            public void Validate(StockThinFunctionDefinition definition)
            {
                owner.Validations++;
                if (owner.Invalid) throw new NotSupportedException("函数公开形状发生变化");
            }
            public object? LoadGlobal(string name)
            {
                owner.Reads++;
                if (owner.Failure is not null) throw owner.Failure;
                return owner.Answer;
            }
            public void Dispose() => owner.Disposals++;
        }
    }
}
