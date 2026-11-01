using System.Numerics;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

/// <summary>固定CPython 3.12.13随机消费序列；含跨32位、MT换轮、拒绝采样和独立实例。</summary>
public class PyRandomTests
{
    private static object New(object? seed) => PyOps.CallValue(PyRandomModule.CreateAttributes()["Random"], [seed], [], [])!;
    private static object? Call(object random, string name, params object?[] args) =>
        PyOps.CallValue(PyObject.GetAttr(random, name), args, [], []);
    private static object? Module(string name, params object?[] args) =>
        PyOps.CallValue(PyRandomModule.CreateAttributes()[name], args, [], []);

    [Fact]
    public void IntegerSeedAnd53BitFloatsMatchFixedBaseline()
    {
        object random = New(42L);
        Assert.Equal(0.6394267984578837, Call(random, "random"));
        Assert.Equal(0.025010755222666936, Call(random, "random"));
        Assert.Equal(0.27502931836911926, Call(random, "random"));
        object positive = New(BigInteger.Parse("1208925819614629174706183"));
        object negative = New(-BigInteger.Parse("1208925819614629174706183"));
        Assert.Equal(0.0074511875167878605, Call(positive, "random"));
        Assert.Equal(0.0074511875167878605, Call(negative, "random"));
    }

    [Fact]
    public void StringBytesAndBytearrayUseTheSameVersionTwoExpansion()
    {
        foreach (object seed in new object[] { "abc", new PyBytes([97, 98, 99]), new PyByteArray([97, 98, 99]) })
        {
            object random = New(seed);
            Assert.Equal(0.7720246314157545, Call(random, "random"));
            Assert.Equal(0.5581691373899791, Call(random, "random"));
        }
        Assert.Equal(0.9158553450749424, Call(New("雪"), "random"));
    }

    [Fact]
    public void GetrandbitsUsesHighPartialWordsAndZeroConsumesNothing()
    {
        object random = New(42L);
        long[] sizes = [0, 1, 31, 32, 33, 64, 65];
        string[] expected = ["0", "1", "239081663", "107420369", "3184935163", "4117511471858006928", "13585496030504862185"];
        for (int index = 0; index < sizes.Length; index++)
            Assert.Equal(expected[index], PyRepr.Str(Call(random, "getrandbits", sizes[index])));
        Assert.Equal(0.6766994874229113, Call(random, "random"));
        Assert.Equal(0.8921795677048454, Call(random, "random"));
        object zero = New(42L);
        Assert.Equal(0L, Call(zero, "getrandbits", 0L));
        Assert.Equal(2746317213L, Call(zero, "getrandbits", 32L));
    }

    [Fact]
    public void StateRolloverKeepsTheThousandWordSequence()
    {
        object random = New(17L);
        ulong digest = 0;
        for (int index = 0; index < 1000; index++)
            digest = unchecked(digest * 6364136223846793005UL ^ (uint)(long)Call(random, "getrandbits", 32L)!);
        Assert.Equal(153250662472235956UL, digest);
        Assert.Equal(1402051775L, Call(random, "getrandbits", 32L));
    }

    [Fact]
    public void RangesAndShufflePreserveRejectionSamplingConsumption()
    {
        object random = New(42L);
        Assert.Equal(3L, Call(random, "randrange", 16L));
        Assert.Equal(-100L, Call(random, "randrange", -100L, 100L, 7L));
        Assert.Equal(28L, Call(random, "randrange", 100L, -100L, -9L));
        Assert.Equal(-2L, Call(random, "randint", -9L, 9L));
        Assert.Equal(BigInteger.Parse("210210708956872545001808"), Call(random, "randrange", BigInteger.One << 80));
        random = New(42L);
        var values = new PyList(Enumerable.Range(0, 10).Select(value => (object?)(long)value));
        Assert.Same(PyNone.Instance, Call(random, "shuffle", values));
        Assert.Equal("[7, 3, 2, 8, 5, 6, 9, 4, 0, 1]", PyRepr.Str(values));
        Assert.Equal("d", Call(random, "choice", "abcd"));
        Assert.Equal(136505587L, Call(random, "getrandbits", 32L));
    }

    [Fact]
    public void ModuleFunctionsShareOneStateButInstancesRemainIndependent()
    {
        object independent = New(42L);
        Module("seed", 42L);
        Assert.Equal(Call(independent, "random"), Module("random"));
        Module("seed", 7L);
        Assert.Equal(0.025010755222666936, Call(independent, "random"));
        Assert.Equal(0.32383276483316237, Module("random"));
    }

    [Fact]
    public void VersionRejectsCustomEqualityWithoutCallingItOrChangingState()
    {
        var cls = new PyClass("CustomVersion");
        int calls = 0;
        cls.SetAttr("__eq__", PyRuntimeDataclassTests.Fn("__eq__", ["self", "other"], _ =>
        {
            calls++;
            return true;
        }));
        object random = New(42L);
        Assert.Throws<PyNotImplementedError>(() => Call(random, "seed", 7L, new PyInstance(cls)));
        Assert.Equal(0, calls);
        Assert.Equal(2746317213L, Call(random, "getrandbits", 32L));
        foreach (object version in new object[] { 2L, 2, new BigInteger(2), 2.0, 2.0f })
        {
            Call(random, "seed", 42L, version);
            Assert.Equal(2746317213L, Call(random, "getrandbits", 32L));
        }
    }

    [Fact]
    public void InvalidInputsDoNotConsumeStateAndUnsupportedOptionsFailExplicitly()
    {
        object random = New(42L);
        Assert.Throws<PyValueError>(() => Call(random, "getrandbits", -1L));
        Assert.Throws<PyTypeError>(() => Call(random, "getrandbits", 1.0));
        Assert.Throws<PyValueError>(() => Call(random, "randrange", 0L));
        Assert.Throws<PyValueError>(() => Call(random, "randrange", 1L, 9L, 0L));
        Assert.Throws<PyTypeError>(() => Call(random, "randrange", 9L, PyNone.Instance, true));
        Assert.Throws<PyIndexError>(() => Call(random, "choice", new PyList()));
        Assert.Throws<PyNotImplementedError>(() => Call(random, "seed", "abc", 1L));
        Assert.Throws<PyNotImplementedError>(() => Call(random, "seed", 1.0));
        Assert.Throws<PyNotImplementedError>(() => Call(random, "getstate"));
        Assert.Equal(2746317213L, Call(random, "getrandbits", 32L));
    }
}
