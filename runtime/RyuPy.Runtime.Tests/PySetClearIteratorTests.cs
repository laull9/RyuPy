// set.clear 的独立迭代状态与已登记边界：真实耗尽/大小失效优先，同大小 clear 恢复不回放旧值。
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PySetClearIteratorTests
{
    private const string SizeMessage = "Set changed size during iteration";
    private const string BoundaryMessage = "set iteration after clear with restored size is not supported yet";
    private static void Clear(PySet set) => PyOps.CallMethod(set, "clear", [], [], []);
    private static PyRuntimeError SizeError(IPyIterator iterator)
    {
        var error = Assert.Throws<PyRuntimeError>(() => iterator.MoveNext());
        Assert.Equal(SizeMessage, error.PyMessage); return error;
    }
    private static PyNotImplementedError BoundaryError(IPyIterator iterator)
    {
        var error = Assert.Throws<PyNotImplementedError>(() => iterator.MoveNext());
        Assert.Equal(BoundaryMessage, error.PyMessage); return error;
    }

    [Fact]
    public void UntouchedPartialAndLastYieldedIteratorsRemainLiveUntilFalse()
    {
        foreach (int consumed in new[] { 0, 1, 2 })
        {
            var set = new PySet([1L, 2L]); IPyIterator iterator = PyIter.Create(set);
            for (int i = 0; i < consumed; i++) Assert.True(iterator.MoveNext());
            Clear(set);
            var first = SizeError(iterator); var second = SizeError(iterator);
            Assert.NotSame(first, second);
            set.Add(1L); set.Add(2L);
            // 大小已恢复、epoch已变化仍先报告既有 size-failed，不能转为 NI 或继续旧快照。
            var third = SizeError(iterator); Assert.NotSame(second, third);
        }
    }

    [Fact]
    public void OnlyConfirmedExhaustionPreventsResurrectionAfterClearAndAdd()
    {
        var set = new PySet([1L]); IPyIterator iterator = PyIter.Create(set);
        Assert.True(iterator.MoveNext()); Assert.False(iterator.MoveNext());
        Clear(set); set.Add(2L); set.Add(3L);
        Assert.False(iterator.MoveNext()); Assert.False(iterator.MoveNext());
        Assert.Same(iterator, PyIter.Create(iterator));
    }

    [Fact]
    public void EmptyClearDoesNotInvalidateButUnprobedEmptyGrowthStillFails()
    {
        var set = new PySet(); IPyIterator iterator = PyIter.Create(set);
        Clear(set); Clear(set);
        Assert.False(iterator.MoveNext());
        set.Add(1L); Assert.False(iterator.MoveNext());

        var unprobed = new PySet(); IPyIterator live = PyIter.Create(unprobed);
        Clear(unprobed); unprobed.Add(1L); SizeError(live);
        unprobed.Remove(1L); SizeError(live);
    }

    [Fact]
    public void ClearWithRestoredSizeIsExplicitBoundaryBeforeAnySnapshotAccess()
    {
        foreach (bool alreadyYieldedLast in new[] { false, true })
        {
            var set = new PySet([1L]); IPyIterator iterator = PyIter.Create(set);
            if (alreadyYieldedLast) Assert.True(iterator.MoveNext());
            Clear(set); set.Add(2L);
            // CP 的两个场景均能返回新成员 2；本切必须 NI，不能返回旧成员或提前耗尽。
            var first = BoundaryError(iterator); var second = BoundaryError(iterator);
            Assert.NotSame(first, second); Assert.Equal(1, set.Count); Assert.True(set.Contains(2L));
        }
        var sameMembers = new PySet([1L, 2L]); IPyIterator saved = PyIter.Create(sameMembers);
        Clear(sameMembers); sameMembers.Add(1L); sameMembers.Add(2L);
        BoundaryError(saved);
        // 新建迭代器只观察当前集合，不继承旧迭代器的边界状态。
        IPyIterator fresh = PyIter.Create(sameMembers);
        Assert.True(fresh.MoveNext()); Assert.True(fresh.MoveNext()); Assert.False(fresh.MoveNext());
    }

    [Fact]
    public void CurrentKeepsPreviouslyDeliveredValueWithoutAdditionalMutationCheck()
    {
        object value = 12345L; var set = new PySet([value]); IPyIterator iterator = PyIter.Create(set);
        Assert.True(iterator.MoveNext()); Assert.Same(value, iterator.Current());
        Clear(set);
        Assert.Same(value, iterator.Current()); SizeError(iterator);
    }

    [Fact]
    public void RegisteredNoClearSameSizeSnapshotBoundaryIsPreserved()
    {
        var set = new PySet([1L, 2L]); var original = set.Items.ToArray(); IPyIterator iterator = PyIter.Create(set);
        set.Remove(1L); set.Add(3L);
        // 已登记旧债，不是 CP 正例：本切不能把无 clear 的旧快照域擅自扩成 NI。
        foreach (object item in original)
        {
            Assert.True(iterator.MoveNext()); Assert.Same(item, iterator.Current());
        }
        Assert.False(iterator.MoveNext());
    }

    [Fact]
    public void EmptyClearAfterRemovalDoesNotCreateANewClearEpochBoundary()
    {
        var set = new PySet([1L]); IPyIterator iterator = PyIter.Create(set);
        set.Remove(1L); Clear(set); set.Add(2L);
        // clear 调用时确实为空；同大小 remove/add 的既有快照域继续保留。
        Assert.True(iterator.MoveNext()); Assert.Equal(1L, iterator.Current()); Assert.False(iterator.MoveNext());
    }

    [Fact]
    public void FailedSavedBindingDoesNotAdvanceCursorOrChangeClearEpoch()
    {
        foreach (bool keyword in new[] { false, true })
        {
            var set = new PySet([1L, 2L]); var original = set.Items.ToArray();
            IPyIterator iterator = PyIter.Create(set); Assert.True(iterator.MoveNext());
            object? method = PyOps.PrepareMethod(set, "clear");
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(method, [0L], keyword ? ["bad"] : [], keyword ? [0L] : []));
            Assert.Equal(2, set.Count); Assert.Same(original[0], iterator.Current());
            Assert.True(iterator.MoveNext()); Assert.Same(original[1], iterator.Current()); Assert.False(iterator.MoveNext());
        }
    }

    [Fact]
    public void SizeAndBoundaryErrorsCaptureEachCallsFrameAndAmbientContext()
    {
        foreach (bool restore in new[] { false, true })
        {
            var set = new PySet([1L]); IPyIterator iterator = PyIter.Create(set);
            Clear(set); if (restore) set.Add(2L);
            var outer = new PyValueError("正在处理"); object? previous = PyErrors.EnterHandler(outer);
            long depth = PyTraceback.PushFrame("set_iterator.py", "caller");
            try
            {
                PyTraceback.SetLine(41);
                PyBaseException first = restore ? BoundaryError(iterator) : SizeError(iterator);
                PyTraceback.SetLine(52);
                PyBaseException second = restore ? BoundaryError(iterator) : SizeError(iterator);
                Assert.NotSame(first, second); Assert.Same(outer, first.Context); Assert.Same(outer, second.Context);
                Assert.Contains(new PyStackFrame("set_iterator.py", "caller", 41), first.Frames);
                Assert.Contains(new PyStackFrame("set_iterator.py", "caller", 52), second.Frames);
                Assert.Null(first.Cause); Assert.Null(second.Cause);
                Assert.False(first.SuppressContextFlag); Assert.False(second.SuppressContextFlag);
                Assert.Same(outer, PyErrors.Current); Assert.Equal(depth, PyTraceback.CaptureDepth());
            }
            finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
        }
    }
}
