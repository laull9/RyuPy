using System;
using System.Collections.Generic;
using System.Reflection;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>
/// 布尔紧凑存储的单例身份、别名、异构物化、活迭代、复杂协议回退与重复回调顺序测试。
/// </summary>
public class PyBooleanListTests
{
    private static object? GetField(PyList list, string fieldName) =>
        typeof(PyList).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list);

    private static bool IsCompact(PyList list) =>
        GetField(list, "_booleans") is not null &&
        GetField(list, "_items") is null &&
        GetField(list, "_numeric") is null;

    private static bool IsObjectMode(PyList list) =>
        GetField(list, "_items") is not null &&
        GetField(list, "_booleans") is null &&
        GetField(list, "_numeric") is null;

    private static PyInstance CreateIndexCallbackInstance(Func<long> callback)
    {
        var cls = new PyClass("IndexCallbackClass");
        cls.SetAttr("__index__", PyNativeFunctions.CreateVariadic("index_callback", _ => callback()));
        return new PyInstance(cls);
    }

    [Fact]
    public void BinaryMultiplication_ExactBool_CreatesCompactListWithSingletons()
    {
        // 正向与反向乘法
        var forward = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([true]), 6L));
        var reverse = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, 6L, new PyList([false])));

        Assert.True(IsCompact(forward));
        Assert.True(IsCompact(reverse));
        Assert.Equal(6, forward.Count);
        Assert.Equal(6, reverse.Count);

        for (int i = 0; i < 6; i++)
        {
            Assert.Same(PyOps.TrueObject, forward.GetItem(i));
            Assert.Same(PyOps.FalseObject, reverse.GetItem(i));
        }

        // Python 中 True == 1 为真，但对象身份绝非同一个
        Assert.True(PyOps.Compare(PyOps.Eq, forward.GetItem(0)!, 1L) is true);
        object oneBox = 1L;
        Assert.False(ReferenceEquals(forward.GetItem(0), oneBox));
    }

    [Fact]
    public void IntegerOne_IsNotCompressed()
    {
        // 整数 1 绝不能因与 True 相等而压为布尔
        var list = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([1L]), 5L));
        Assert.True(IsObjectMode(list));
        Assert.Equal(5, list.Count);
        Assert.Equal(1L, list.GetItem(0));
    }

    [Fact]
    public void MultiElementList_IsNotCompressed()
    {
        // 仅 Count==1 且为 exact bool 的重复使用紧凑存储
        var list = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([true, false]), 3L));
        Assert.True(IsObjectMode(list));
        Assert.Equal(6, list.Count);
    }

    [Fact]
    public void ZeroAndNegativeRepetition_ReturnsOrdinaryEmptyList()
    {
        var zero = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([true]), 0L));
        var negative = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([false]), -3L));

        Assert.True(IsObjectMode(zero));
        Assert.True(IsObjectMode(negative));
        Assert.Equal(0, zero.Count);
        Assert.Equal(0, negative.Count);
    }

    [Fact]
    public void RepetitionOverflow_ThrowsPyOverflowError()
    {
        Assert.Throws<PyOverflowError>(() =>
            PyOps.Binary(PyOps.Mul, new PyList([true]), (long)Array.MaxLength + 1L));
    }

    [Fact]
    public void RootListMultiply_AndCallbackOrder()
    {
        // 1. 回调中修改为 False -> 仍满足 bool 紧凑条件
        var source1 = new PyList([true]);
        var indexInst1 = CreateIndexCallbackInstance(() =>
        {
            source1.SetItem(0, false);
            return 3L;
        });
        var res1 = Assert.IsType<PyList>(PyListProtocols.Invoke("__mul__", source1, [indexInst1]));
        Assert.True(IsCompact(res1));
        Assert.Equal(3, res1.Count);
        Assert.Same(PyOps.FalseObject, res1.GetItem(0));
        Assert.Same(PyOps.FalseObject, res1.GetItem(2));

        // 2. 回调中修改为异构对象 -> 退化为普通对象列表
        var source2 = new PyList([true]);
        var indexInst2 = CreateIndexCallbackInstance(() =>
        {
            source2.SetItem(0, "hetero");
            return 2L;
        });
        var res2 = Assert.IsType<PyList>(PyListProtocols.Invoke("__mul__", source2, [indexInst2]));
        Assert.True(IsObjectMode(res2));
        Assert.Equal(2, res2.Count);
        Assert.Equal("hetero", res2.GetItem(0));

        // 3. 回调中清空 source -> 产出普通空列表
        var source3 = new PyList([true]);
        var indexInst3 = CreateIndexCallbackInstance(() =>
        {
            source3.Clear();
            return 4L;
        });
        var res3 = Assert.IsType<PyList>(PyListProtocols.Invoke("__mul__", source3, [indexInst3]));
        Assert.True(IsObjectMode(res3));
        Assert.Equal(0, res3.Count);
    }

    [Fact]
    public void BooleanOperations_RemainCompact()
    {
        var flags = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([true]), 6L));
        Assert.True(IsCompact(flags));

        // SetItem(bool)
        flags.SetItem(2, false);
        Assert.True(IsCompact(flags));
        Assert.Same(PyOps.FalseObject, flags.GetItem(2));
        Assert.Same(PyOps.TrueObject, flags.GetItem(1));

        // Append(bool)
        flags.Append(false);
        Assert.True(IsCompact(flags));
        Assert.Equal(7, flags.Count);
        Assert.Same(PyOps.FalseObject, flags.GetItem(6));

        // Copy()
        var copy = flags.Copy();
        Assert.True(IsCompact(copy));
        Assert.Equal(7, copy.Count);

        // Reverse()
        copy.Reverse();
        Assert.True(IsCompact(copy));
        Assert.Same(PyOps.FalseObject, copy.GetItem(0));

        // Pop()
        object? poppedLast = copy.Pop();
        Assert.True(IsCompact(copy));
        Assert.Same(PyOps.TrueObject, poppedLast);

        object? poppedFirst = copy.Pop(0);
        Assert.True(IsCompact(copy));
        Assert.Same(PyOps.FalseObject, poppedFirst);

        // Clear() and Append(bool)
        copy.Clear();
        Assert.True(IsCompact(copy));
        Assert.Equal(0, copy.Count);
        copy.Append(true);
        Assert.True(IsCompact(copy));
        Assert.Same(PyOps.TrueObject, copy.GetItem(0));
    }

    [Fact]
    public void HeterogeneousWriteAndAppend_MaterializesAndPreservesItemsAlias()
    {
        var flags = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([true]), 4L));
        Assert.True(IsCompact(flags));

        // 异构写
        object one = 1L;
        flags.SetItem(0, one);
        Assert.True(IsObjectMode(flags));
        Assert.Same(one, flags.GetItem(0));
        Assert.Same(PyOps.TrueObject, flags.GetItem(1));

        // 异构追加
        flags.Append("tail");
        Assert.Equal("tail", flags.GetItem(flags.Count - 1));

        // 公开 Items 别名持久同步
        var itemsAlias = flags.Items;
        flags.SetItem(1, "updated_via_list");
        Assert.Equal("updated_via_list", itemsAlias[1]);
        itemsAlias[2] = "updated_via_items";
        Assert.Equal("updated_via_items", flags.GetItem(2));
        Assert.Same(itemsAlias, flags.Items);
    }

    [Fact]
    public void LiveIterator_DirectRead_AndMaterializationDuringIteration()
    {
        var live = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([true]), 3L));
        var it = PyIter.Create(live);

        // 首次读取：仍为紧凑存储
        Assert.True(it.MoveNext());
        Assert.Same(PyOps.TrueObject, it.Current());
        Assert.True(IsCompact(live));

        // 迭代途中发生异构写和追加
        live.SetItem(1, "changed");
        live.Append(PyOps.FalseObject);
        Assert.True(IsObjectMode(live));

        // 游标继续推进，正确观察到物化后的新旧内容
        Assert.True(it.MoveNext());
        Assert.Equal("changed", it.Current());

        Assert.True(it.MoveNext());
        Assert.Same(PyOps.TrueObject, it.Current());

        Assert.True(it.MoveNext());
        Assert.Same(PyOps.FalseObject, it.Current());

        // 耗尽
        Assert.False(it.MoveNext());

        // 耗尽后追加新元素，迭代器必须保持耗尽状态 (stays exhausted)
        live.Append(true);
        Assert.False(it.MoveNext());
    }

    [Fact]
    public void LiveIterator_PureBooleanAppend_StaysCompact()
    {
        var live = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([false]), 2L));
        var seen = new List<object?>();
        var it = PyIter.Create(live);

        while (it.MoveNext())
        {
            seen.Add(it.Current());
            if (seen.Count == 1)
            {
                live.Append(true);
            }
        }

        Assert.Equal(3, seen.Count);
        Assert.Same(PyOps.FalseObject, seen[0]);
        Assert.Same(PyOps.FalseObject, seen[1]);
        Assert.Same(PyOps.TrueObject, seen[2]);
        Assert.True(IsCompact(live));
    }

    [Fact]
    public void SelfExtend_AndComplexProtocolFallback()
    {
        var flags = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([true]), 3L));
        flags.Extend(flags);
        Assert.True(IsCompact(flags));
        Assert.Equal(6, flags.Count);

        // 复杂切片操作 fallback 到 Items
        flags.SetItem(new PySlice(1L, 4L, 2L), new PyList([PyOps.FalseObject, PyOps.FalseObject]));
        Assert.True(IsObjectMode(flags));
        Assert.Same(PyOps.FalseObject, flags.GetItem(1));
        Assert.Same(PyOps.FalseObject, flags.GetItem(3));

        // 排序与插入/删除
        flags.Insert(1, "inserted");
        Assert.Equal(1L, flags.IndexOf("inserted"));
        flags.Remove("inserted");
        Assert.Equal(-1L, flags.IndexOf("inserted"));

        // 自引用
        flags.Append(flags);
        Assert.Same(flags, flags.GetItem(flags.Count - 1));
    }

    [Fact]
    public void NumericListInterop_TryReadF64AndAppendFreshF64()
    {
        var flags = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([true]), 2L));
        Assert.True(IsCompact(flags));

        // TryReadF64 在 bool 返回 false 且不物化
        Assert.False(flags.TryReadF64(0, out double val));
        Assert.Equal(0.0, val);
        Assert.True(IsCompact(flags));

        // AppendFreshF64 在 bool 安全物化并追加
        flags.AppendFreshF64(42.5);
        Assert.True(IsObjectMode(flags));
        Assert.Equal(3, flags.Count);
        Assert.Equal(42.5, flags.GetItem(2));
    }

    [Fact]
    public void LargeAllocation_CompactStorageVerification()
    {
        const int size = 100_000;
        var flags = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([true]), (long)size));
        Assert.True(IsCompact(flags));
        Assert.Equal(size, flags.Count);

        // 验证内部 List<bool> 确实占用较小连续存储
        var booleans = Assert.IsType<List<bool>>(GetField(flags, "_booleans"));
        Assert.Equal(size, booleans.Count);

        // 写入与读取后仍 compact
        flags.SetItem(size - 1, false);
        Assert.True(IsCompact(flags));
        Assert.Same(PyOps.FalseObject, flags.GetItem(size - 1));
        Assert.Same(PyOps.TrueObject, flags.GetItem(0));
    }
    [Fact]
    public void DirectItemsExposureAndFreshHeterogeneousAppendKeepPermanentAliases()
    {
        var list = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([true]), 3L));
        List<object?> exposed = list.Items;
        Assert.True(IsObjectMode(list));
        list.Clear(); list.Append(PyOps.FalseObject);
        PyListProtocols.Invoke("__imul__", list, [2L]);
        Assert.Same(exposed, list.Items);
        Assert.Equal(2, exposed.Count);
        Assert.Same(PyOps.FalseObject, exposed[0]);
        var fresh = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([true]), 2L));
        fresh.Append(fresh);
        Assert.True(IsObjectMode(fresh));
        Assert.Same(fresh, fresh.GetItem(2));
    }

    [Fact]
    public void LargeRepeatAllocatesByteStorageAndIterationDoesNotMaterialize()
    {
        var source = new PyList([PyOps.TrueObject]);
        PyOps.Binary(PyOps.Mul, source, 1L); // 预热工厂，排除首次初始化。
        long before = GC.GetAllocatedBytesForCurrentThread();
        var flags = Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, source, 100_000L));
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        // 允许对象头与测试调用余量，仍严格排除8B引用数组的物化结果。
        Assert.InRange(allocated, 100_000L, 200_000L);
        flags.SetItem(99_999L, false);
        var iterator = PyIter.Create(flags);
        int count = 0;
        while (iterator.MoveNext()) { Assert.IsType<bool>(iterator.Current()); count++; }
        Assert.Equal(100_000, count);
        Assert.True(IsCompact(flags));
    }

}
