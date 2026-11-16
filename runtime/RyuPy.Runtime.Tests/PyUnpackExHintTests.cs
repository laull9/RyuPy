// 星号剩余列表的局部长度提示：长度转换、属性绑定和调用失败必须分开检查。
using System;
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime.Tests;

public partial class PyUnpackExTests
{
    [Fact]
    public void LenUsesIndexButHintRequiresActualInteger()
    {
        int indexes = 0, hints = 0;
        var cls = (PyClass)PyOps.MakeClass("Index", [], null);
        cls.SetAttr("__index__", Fn("__index__", (_, _, _, _) => { indexes++; return 2L; }));
        object index = cls.Invoke([], [], [])!;
        var source = Values(1L, 2L, 3L);
        source.Class.SetAttr("__len__", Fn("__len__", (_, _, _, _) => index));
        source.Class.SetAttr("__length_hint__", Fn("hint", (_, _, _, _) => { hints++; return 0L; }));
        AssertParts(Unpack(source), 1L, [2L], 3L);
        Assert.Equal(1, indexes); Assert.Equal(0, hints);
        var hintSource = Values(1L, 2L, 3L);
        hintSource.Class.SetAttr("__length_hint__", Fn("hint", (_, _, _, _) => index));
        Assert.Equal("__length_hint__ must be an integer, not Index", Assert.Throws<PyTypeError>(() => Unpack(hintSource)).PyMessage);
        Assert.Equal(1, indexes);
        // 长度结果转换中的TypeError也应转入hint，而不是只捕获调用时的TypeError。
        cls.SetAttr("__index__", Fn("__index__", (_, _, _, _) => throw new PyTypeError("index")));
        var fallback = Values(1L, 2L, 3L);
        fallback.Class.SetAttr("__len__", Fn("len", (_, _, _, _) => index));
        fallback.Class.SetAttr("__length_hint__", Fn("hint", (_, _, _, _) => { hints++; return 0L; }));
        AssertParts(Unpack(fallback), 1L, [2L], 3L); Assert.Equal(1, hints);
    }

    [Fact]
    public void HintIntegerDomainAndCapacityEstimateNeverLimitActualElements()
    {
        // 超过预留上限但合法的估计只测试4097；不执行巨大有效hint分配实验。
        foreach (object value in new object[] { false, true, 0, 1L, new BigInteger(2), 4097L, PyNotImplemented.Instance })
        {
            var source = Values(1L, 2L, 3L, 4L);
            source.Class.SetAttr("__length_hint__", Fn("hint", (_, _, _, _) => value));
            AssertParts(Unpack(source), 1L, [2L, 3L], 4L);
        }
        // 实际元素超过4096仍完整采集，防止把预留上限误写成元素数量上限。
        var many = new object?[4100];
        for (int i = 0; i < many.Length; i++) many[i] = (long)i;
        var manySource = Values(many);
        manySource.Class.SetAttr("__length_hint__", Fn("hint", (_, _, _, _) => 0L));
        var manyResult = Unpack(manySource);
        Assert.Equal(0L, manyResult.Items[0]); Assert.Equal(4099L, manyResult.Items[2]);
        var manyMiddle = Assert.IsType<PyList>(manyResult.Items[1]);
        Assert.Equal(4098, manyMiddle.Count); Assert.Equal(4098L, manyMiddle.GetItem(4097));
        foreach (object value in new object[] { "bad", PyNone.Instance })
        {
            var source = Values(1L, 2L, 3L);
            source.Class.SetAttr("__length_hint__", Fn("hint", (_, _, _, _) => value));
            Assert.Throws<PyTypeError>(() => Unpack(source));
        }
        foreach (bool isLen in new[] { false, true })
        {
            var negative = Values(1L, 2L, 3L);
            negative.Class.SetAttr(isLen ? "__len__" : "__length_hint__", Fn("size", (_, _, _, _) => -1L));
            Assert.Equal(isLen ? "__len__() should return >= 0" : "__length_hint__() should return >= 0",
                Assert.Throws<PyValueError>(() => Unpack(negative)).PyMessage);
            foreach (int sign in new[] { -1, 1 })
            {
                var huge = Values(1L, 2L, 3L);
                huge.Class.SetAttr(isLen ? "__len__" : "__length_hint__", Fn("size", (_, _, _, _) => sign * (BigInteger.One << 100)));
                if (isLen && sign < 0) Assert.Throws<PyValueError>(() => Unpack(huge));
                else Assert.Equal(isLen ? "cannot fit 'int' into an index-sized integer" : "Python int too large to convert to C ssize_t",
                    Assert.Throws<PyOverflowError>(() => Unpack(huge)).PyMessage);
            }
        }
    }

    [Fact]
    public void HintPropertyLookupCallAndReturnValidationHaveDistinctBoundaries()
    {
        foreach (string mode in new[] { "lookup", "call", "result", "valid" })
        {
            var events = new List<string>(); var error = new PyTypeError(mode);
            var source = Values(1L, 2L, 3L);
            source.Class.SetAttr("__length_hint__", new PyProperty(Fn("get_hint", (_, _, _, _) =>
            {
                events.Add("lookup"); if (mode == "lookup") throw error;
                return Nullary("hint_body", () =>
                { events.Add("call"); return mode == "call" ? throw error : mode == "result" ? "bad" : 0L; });
            })));
            if (mode == "lookup") Assert.Same(error, Assert.Throws<PyTypeError>(() => Unpack(source)));
            else if (mode == "result")
                Assert.Equal("__length_hint__ must be an integer, not str", Assert.Throws<PyTypeError>(() => Unpack(source)).PyMessage);
            else AssertParts(Unpack(source), 1L, [2L], 3L);
            Assert.Equal(mode == "lookup" ? new[] { "lookup" } : new[] { "lookup", "call" }, events);
        }
    }

    [Fact]
    public void LenPropertyRunsGetterAndCallableBeforeHintFallback()
    {
        foreach (string mode in new[] { "lookup-type", "lookup-value", "call-type", "valid" })
        {
            var events = new List<string>(); var failure = new PyValueError("getter");
            var source = Values(1L, 2L, 3L);
            source.Class.SetAttr("__len__", new PyProperty(Fn("get_len", (_, _, _, _) =>
            {
                events.Add("lookup");
                if (mode == "lookup-type") throw new PyTypeError("getter");
                if (mode == "lookup-value") throw failure;
                return Nullary("len_body", () => { events.Add("call"); return mode == "call-type" ? throw new PyTypeError("call") : 0L; });
            })));
            source.Class.SetAttr("__length_hint__", Fn("hint", (_, _, _, _) => { events.Add("hint"); return 0L; }));
            if (mode == "lookup-value") Assert.Same(failure, Assert.Throws<PyValueError>(() => Unpack(source)));
            else AssertParts(Unpack(source), 1L, [2L], 3L);
            Assert.Equal(mode switch
            {
                "lookup-type" => new[] { "lookup", "hint" }, "lookup-value" => new[] { "lookup" },
                "call-type" => new[] { "lookup", "call", "hint" }, _ => new[] { "lookup", "call" }
            }, events);
        }
    }

    [Fact]
    public void HintBindingAndMutationAffectTailBeforeItsFirstRead()
    {
        foreach (bool classMethod in new[] { false, true })
        {
            var source = Values(1L, 2L, 3L); int called = 0;
            source.Class.SetAttr("__length_hint__", classMethod
                ? new PyClassMethod(Fn("hint", (_, args, _, _) => { Assert.Same(source.Class, args[0]); called++; return 0L; }))
                : new PyStaticMethod(Nullary("hint", () => { called++; return 0L; })));
            AssertParts(Unpack(source), 1L, [2L], 3L); Assert.Equal(1, called);
        }
        int tailIndex = 0, iters = 0, changedNext = 0;
        var tail = User(() => throw new PyAssertionError("旧next不能被预先缓存"));
        var first = User(() => 10L, self => ++iters == 1 ? self : tail);
        first.Class.SetAttr("__length_hint__", Fn("hint", (_, _, _, _) =>
        {
            tailIndex = 1;
            tail.Class.SetAttr("__next__", Fn("new_next", (_, _, _, _) =>
            { changedNext++; return tailIndex++ == 1 ? 60L : throw new PyStopIteration(); }));
            return 0L;
        }));
        AssertParts(Unpack(first), 10L, [], 60L); Assert.Equal(2, changedNext);
        // 普通可调用槽合法；一旦带自定义__get__，必须明确拒绝而非跳过描述符。
        foreach (string slot in new[] { "__len__", "__length_hint__" })
        {
            int calls = 0, getters = 0;
            var callableClass = (PyClass)PyOps.MakeClass("HintCallable", [], null);
            callableClass.SetAttr("__call__", Fn("__call__", (_, _, _, _) => { calls++; return 0L; }));
            var callable = (PyInstance)callableClass.Invoke([], [], [])!;
            var ordinary = Values(1L, 2L, 3L);
            ordinary.Class.SetAttr(slot, callable);
            AssertParts(Unpack(ordinary), 1L, [2L], 3L); Assert.Equal(1, calls);
            callableClass.SetAttr("__get__", new PyFunction("__get__",
                PySignature.Create("__get__", ["self", "instance", "owner"], ["pos", "pos", "pos"], ["0", "0", "0"]),
                (_, _, _, _) => { getters++; return Nullary("bound_hint", () => 0L); }, []));
            var unsupported = Values(1L, 2L, 3L);
            unsupported.Class.SetAttr(slot, callable);
            Assert.Equal($"starred unpack: custom descriptor for {slot} is not supported",
                Assert.Throws<PyNotImplementedError>(() => Unpack(unsupported)).PyMessage);
            Assert.Equal(1, calls); Assert.Equal(0, getters);
        }
    }
}
