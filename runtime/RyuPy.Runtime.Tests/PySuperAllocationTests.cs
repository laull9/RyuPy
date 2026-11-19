// 普通 super 的热布局与实际分配预算：外部桥接状态不能增加每个本地代理的内存。
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PySuperAllocationTests
{
    [Fact]
    public void OrdinarySuper_KeepsTwoReferenceFields()
    {
        FieldInfo[] fields = typeof(PySuper).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.Equal(2, fields.Length);
        Assert.All(fields, field => Assert.False(field.FieldType.IsValueType));
    }

    [Fact]
    public void OrdinarySuper_AllocatesOnlyTheOriginalTwoReferenceObject()
    {
        var owner = new PyClass("AllocationOwner");
        var self = new object();
        var values = new PySuper[4096];
        // 数组、类和 JIT 冷启动全部先完成；对象写入数组让分配逃逸，不能被栈分配消除。
        _ = FillAndMeasure(values, owner, self);
        long allocated = FillAndMeasure(values, owner, self);
        // CLR 对象头两字长，原 Owner/Self 两引用共两字长；64 位上每个对象为 32 字节。
        Assert.Equal(values.LongLength * 4 * IntPtr.Size, allocated);
        Assert.Same(owner, values[0].Owner);
        Assert.Same(self, values[^1].Self);
        Assert.Null(values[0].ForeignObject);
        GC.KeepAlive(values);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long FillAndMeasure(PySuper[] values, PyClass owner, object self)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < values.Length; i++) values[i] = new PySuper(owner, self);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
