using System;
using System.Linq;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>
/// 正确性修正的回归测试：覆盖空容器真值、字符串码点、字典版本语义、
/// 稳定排序、解包数量校验与集合迭代快照。
/// </summary>
/// <remarks>
/// 期望值全部以 CPython 3.12 的实际行为为准；集合迭代中修改是 CPython 的未定义行为，
/// 这里测试的是 RyuPy 已登记的确定性快照策略（见 docs/semantics.md §4.3）。
/// </remarks>
public class SemanticsFixTests
{
    [Fact]
    public void EmptyContainersAreFalsy()
    {
        Assert.False(PyOps.Truthy(new PyList()));
        Assert.False(PyOps.Truthy(new PyTuple([])));
        Assert.False(PyOps.Truthy(new PyDict()));
        Assert.False(PyOps.Truthy(new PySet()));
        Assert.False(PyOps.Truthy(new PyBytes([])));
        Assert.False(PyOps.Truthy(new PyByteArray([])));
        Assert.False(PyOps.Truthy(string.Empty));
        Assert.True(PyOps.Truthy(new PyList([0L])));
        Assert.True(PyOps.Truthy("x"));
    }

    [Fact]
    public void StringsUseCodePoints()
    {
        // "😀" 在 UTF-16 里占两个 code unit，在 Python 里是一个字符
        string text = "😀a";
        Assert.Equal(2L, PyText.Length(text));
        Assert.Equal("😀", PyText.At(text, 0));
        Assert.Equal("a", PyText.At(text, 1));
        Assert.Equal("😀", PyText.At(text, -2));
        Assert.Equal("😀", PyText.Slice(text, new PySlice(null, 1L, null)));
        Assert.Equal("a😀", PyText.Slice(text, new PySlice(null, null, -1L)));
        Assert.Equal(new object?[] { "😀", "a" }, PyText.Items(text));
    }

    [Fact]
    public void OverwritingDictKeyKeepsIterationValid()
    {
        var dict = new PyDict();
        dict.Set("a", 1L);
        long version = dict.Version;

        // 覆盖已有键不改变结构，因此不递增版本（CPython 允许迭代中赋值）
        dict.Set("a", 2L);
        Assert.Equal(version, dict.Version);
        Assert.Equal(2L, dict.Get("a"));
        Assert.Equal(1, dict.Count);

        // 新增键则递增版本，迭代器必须报 RuntimeError
        dict.Set("b", 3L);
        Assert.NotEqual(version, dict.Version);
        var iter = PyIter.Create(dict);
        Assert.True(iter.MoveNext());
        dict.Set("c", 4L);
        Assert.Throws<PyRuntimeError>(() => iter.MoveNext());
    }

    [Fact]
    public void DeletingFromDictKeepsInsertionOrder()
    {
        var dict = new PyDict();
        for (long i = 0; i < 5; i++)
        {
            dict.Set(i, i * 10);
        }
        dict.Delete(2L);
        dict.Set(5L, 50L);

        // 删除留墓碑，迭代顺序仍是余下键的插入顺序
        Assert.Equal(new object?[] { 0L, 1L, 3L, 4L, 5L }, dict.Keys.ToArray());
    }

    [Fact]
    public void DictViewsAreLiveAndSupportLenAndMembership()
    {
        var dict = new PyDict();
        dict.Set("a", 1L);
        var keys = new PyDictView(dict, PyDictViewKind.Keys);
        var values = new PyDictView(dict, PyDictViewKind.Values);
        var items = new PyDictView(dict, PyDictViewKind.Items);

        // 视图是活的：构造之后再插入也能看到
        dict.Set("b", 2L);
        Assert.Equal(2L, PyOps.Len(keys));
        Assert.True(PyOps.Contains(keys, "b"));
        Assert.True(PyOps.Contains(values, 2L));
        Assert.True(PyOps.Contains(items, new PyTuple(["b", 2L])));
        Assert.False(PyOps.Contains(items, "b"));
    }

    [Fact]
    public void UnhashableKeyInDictRaisesTypeError()
    {
        var dict = new PyDict();
        Assert.Throws<PyTypeError>(() => PyOps.Contains(dict, new PyList()));
    }

    [Fact]
    public void SetIterationUsesSnapshotEvenWhenGrown()
    {
        var set = new PySet([1L, 2L, 3L]);
        var iter = PyIter.Create(set);
        Assert.True(iter.MoveNext());
        set.Add(4L);

        int seen = 1;
        while (iter.MoveNext())
        {
            seen++;
        }
        // 快照只有最初的三个元素；新增元素不会让迭代越界
        Assert.Equal(3, seen);
    }

    [Fact]
    public void SortIsStableInBothDirections()
    {
        // 0.0 与 -0.0 相等，但 repr 不同：稳定排序能看出原始顺序
        var ascending = new PyList([0.0, -0.0, 0.0, -0.0, 0.0, -0.0]);
        PySort.Sort(ascending.Items, reverse: false);
        Assert.Equal(
            new object?[] { 0.0, -0.0, 0.0, -0.0, 0.0, -0.0 },
            ascending.Items.ToArray()
        );

        var descending = new PyList([0.0, -0.0, 0.0, -0.0, 0.0, -0.0]);
        PySort.Sort(descending.Items, reverse: true);
        Assert.Equal(
            new object?[] { 0.0, -0.0, 0.0, -0.0, 0.0, -0.0 },
            descending.Items.ToArray()
        );
    }

    [Fact]
    public void UnpackCheckRejectsExtraValuesWithoutStar()
    {
        var error = Assert.Throws<PyValueError>(() => PyOps.UnpackCheck(3, 2, hasStar: false));
        Assert.Contains("too many values to unpack (expected 2)", error.PyMessage);

        // 星号形态只要求下限
        PyOps.UnpackCheck(3, 2, hasStar: true);
        Assert.Throws<PyValueError>(() => PyOps.UnpackCheck(1, 2, hasStar: true));
    }

    [Fact]
    public void StaticIntegerOverflowBecomesPythonError()
    {
        Assert.Throws<PyOverflowError>(() => PyMath.FloorDivI64(long.MinValue, -1));
        Assert.Throws<PyOverflowError>(() => PyMath.AbsI64(long.MinValue));
        Assert.Throws<PyOverflowError>(() => PyMath.PowI64(2, 64));
        // 移位不能按 63 取模
        Assert.Throws<PyOverflowError>(() => PyMath.ShiftLeftI64(1, 100));
        Assert.Equal(1L << 62, PyMath.ShiftLeftI64(1, 62));
        Assert.Equal(-1L, PyMath.ShiftRightI64(-8, 100));
        Assert.Equal(0L, PyMath.ShiftRightI64(8, 100));
    }
}
