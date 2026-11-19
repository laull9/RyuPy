// 精确和派生str共用类型级索引协议；错误身份、巨大数原类型和调用次数不能丢失。
using System.Numerics;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyStringIndexProtocolTests
{
    private static PyFunction Fn(Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic("string_index", body);
    private static PyInstance Key(Func<object?> body, string name = "IndexValue")
    {
        var cls = new PyClass(name); cls.SetAttr("__index__", Fn(_ => body())); return new PyInstance(cls);
    }
    private static object[] Receivers()
    {
        var cls = (PyClass)PyOps.MakeClass("IndexText", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        return ["a😀猫", cls.Invoke(["a😀猫"], [], [])!];
    }
    [Fact]
    public void OneTypeSlotCallIgnoresInstanceShadowAndPreservesErrors()
    {
        foreach (object text in Receivers())
        {
            int calls = 0; var key = Key(() => { calls++; return 1L; });
            PyObject.SetAttr(key, "__index__", Fn(_ => throw new PyValueError("instance shadow")));
            Assert.Equal("😀", PyOps.GetItem(text, key)); Assert.Equal(1, calls);
            var failure = new PyValueError("body");
            Assert.Same(failure, Assert.Throws<PyValueError>(() => PyOps.GetItem(text, Key(() => throw failure))));
            int nestedCalls = 0; var nested = Key(() => { nestedCalls++; return 0L; });
            Assert.Equal("__index__ returned non-int (type IndexValue)",
                Assert.Throws<PyTypeError>(() => PyOps.GetItem(text, Key(() => nested))).PyMessage);
            Assert.Equal(0, nestedCalls);
            Assert.Equal("cannot fit 'HugeIndex' into an index-sized integer",
                Assert.Throws<PyIndexError>(() => PyOps.GetItem(text, Key(() => BigInteger.One << 100, "HugeIndex"))).PyMessage);
            Assert.Equal("cannot fit 'int' into an index-sized integer",
                Assert.Throws<PyIndexError>(() => PyOps.GetItem(text, BigInteger.One << 100)).PyMessage);
            Assert.Equal("😀", PyOps.GetItem(text, true));
            Assert.Equal("猫", PyOps.GetItem(text, Key(() => -1L)));
        }
    }
    [Fact]
    public void DescriptorGetterAndReturnedCallableEachRunOnce()
    {
        foreach (object text in Receivers())
        {
            var descriptorClass = new PyClass("Descriptor"); var descriptor = new PyInstance(descriptorClass);
            var indexClass = new PyClass("DescriptorIndex"); var key = new PyInstance(indexClass);
            int gets = 0, calls = 0;
            descriptorClass.SetAttr("__get__", Fn(args =>
            {
                gets++; Assert.Equal(3, args.Length); Assert.Same(key, args[1]); Assert.Same(indexClass, args[2]);
                return Fn(call => { calls++; Assert.Empty(call); return 1L; });
            }));
            indexClass.SetAttr("__index__", descriptor);
            Assert.Equal("😀", PyOps.GetItem(text, key)); Assert.Equal(1, gets); Assert.Equal(1, calls);
            var marker = new PyValueError("getter");
            descriptorClass.SetAttr("__get__", Fn(_ => throw marker));
            Assert.Same(marker, Assert.Throws<PyValueError>(() => PyOps.GetItem(text, key)));
            indexClass.SetAttr("__index__", null);
            Assert.Throws<PyTypeError>(() => PyOps.GetItem(text, key));
        }
    }
}
