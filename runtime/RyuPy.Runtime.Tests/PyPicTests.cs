using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>
/// 有界 PIC（M2.9）的正确性测试。
/// </summary>
/// <remarks>
/// PIC 只允许影响性能：类型变化、方法替换、实例属性遮蔽、站点越界与条目淘汰都必须回到
/// 通用路径并给出与关闭 PIC 相同的结果。这里直接调用运行时入口，不依赖编译器生成的调用点。
/// </remarks>
public class PyPicTests
{
    private static PyFunction Fn(string name, Func<object?[], object?> body)
    {
        string[] names = ["self"];
        var sig = PySignature.Create(
            name,
            names,
            names.Select(_ => "pos").ToArray(),
            names.Select(_ => "0").ToArray()
        );
        return new PyFunction(name, sig, (args, _, _) => body(args), []);
    }

    private static PyClass NewClass(string name) => (PyClass)PyOps.MakeClass(name, [], null);

    [Fact]
    public void BinaryMatchesGenericPathAcrossTypeChanges()
    {
        const int site = 0;
        Assert.Equal(3L, PyPic.Binary(site, PyOps.Add, 1L, 2L));
        Assert.Equal(3.5, PyPic.Binary(site, PyOps.Add, 1.5, 2.0));
        Assert.Equal("ab", PyPic.Binary(site, PyOps.Add, "a", "b"));
        Assert.Equal(2L, PyPic.Binary(site, PyOps.Add, true, true));
        // 小结果会从 BigInteger 归一化回 long，与通用路径一致。
        Assert.Equal(4L, PyPic.Binary(site, PyOps.Add, new BigInteger(2), new BigInteger(2)));
    }

    [Fact]
    public void BinaryEvictsEntriesAndKeepsResults()
    {
        const int site = 7;
        for (int round = 0; round < 100; round++)
        {
            Assert.Equal(3L, PyPic.Binary(site, PyOps.Add, 1L, 2L));
            Assert.Equal(3.5, PyPic.Binary(site, PyOps.Add, 1.5, 2.0));
            Assert.Equal("ab", PyPic.Binary(site, PyOps.Add, "a", "b"));
            Assert.Equal(2L, PyPic.Binary(site, PyOps.Add, true, true));
        }
    }

    [Fact]
    public void OutOfRangeSiteFallsBackToGenericPath()
    {
        Assert.Equal("ab", PyPic.Binary(1_000_000, PyOps.Add, "a", "b"));
        PyFunction function = Fn("f", _ => "ok");
        Assert.Equal("ok", PyPic.CallValue(1_000_000, function, [null], [], []));
    }

    [Fact]
    public void MethodReplacementInvalidatesCache()
    {
        const int site = 1;
        PyClass cls = NewClass("Counter");
        cls.SetAttr("bump", Fn("bump", _ => 1L));
        var instance = (PyInstance)cls.Invoke([], [], [])!;

        Assert.Equal(1L, PyPic.CallMethod(site, instance, "bump", [], [], []));

        // 替换类方法：全局类版本递增，缓存条目必须失效。
        cls.SetAttr("bump", Fn("bump", _ => 2L));
        Assert.Equal(2L, PyPic.CallMethod(site, instance, "bump", [], [], []));
    }

    [Fact]
    public void InstanceAttributeShadowInvalidatesMethodCache()
    {
        const int site = 2;
        PyClass cls = NewClass("Counter");
        cls.SetAttr("bump", Fn("bump", _ => 1L));
        var instance = (PyInstance)cls.Invoke([], [], [])!;

        Assert.Equal(1L, PyPic.CallMethod(site, instance, "bump", [], [], []));

        // 实例属性遮蔽方法：实例版本递增，缓存不能继续返回旧绑定。
        PyObject.SetAttr(instance, "bump", 5L);
        Assert.Throws<PyTypeError>(
            () => PyPic.CallMethod(site, instance, "bump", [], [], [])
        );

        PyObject.DelAttr(instance, "bump");
        Assert.Equal(1L, PyPic.CallMethod(site, instance, "bump", [], [], []));
    }

    [Fact]
    public void CallValueCachesOnlyStableCallables()
    {
        const int site = 3;
        PyClass cls = NewClass("Counter");
        cls.SetAttr("bump", Fn("bump", _ => 7L));
        var instance = (PyInstance)cls.Invoke([], [], [])!;
        object? bound = PyObject.GetAttr(instance, "bump");

        Assert.Equal(7L, PyPic.CallValue(site, bound, [], [], []));
        Assert.Equal(7L, PyPic.CallValue(site, bound, [], [], []));

        // 换一个可调用对象：引用不同，必须重新分派。
        Assert.Equal(
            "x",
            PyPic.CallValue(site, Fn("other", _ => "x"), [null], [], [])
        );
    }
}
