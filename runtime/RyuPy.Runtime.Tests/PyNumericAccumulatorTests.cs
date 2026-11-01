using System.Numerics;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

/// <summary>延迟装箱累加槽保留溢出、动态类型、原地协议和返回对象身份。</summary>
public class PyNumericAccumulatorTests
{
    [Fact]
    public void IntegerBoxesAreCachedPerValueAndInitialIdentityIsPreserved()
    {
        object initial = 1000L;
        object state = PyNumericAccumulator.Create(initial);
        Assert.Same(initial, PyNumericAccumulator.Value(state));
        PyNumericAccumulator.Add(state, 1L);
        object? current = PyNumericAccumulator.Value(state);
        Assert.Equal(1001L, current);
        Assert.Same(current, PyNumericAccumulator.Value(state));
        PyNumericAccumulator.Add(state, 0L);
        Assert.Equal(1001L, PyNumericAccumulator.Value(state));
        Assert.NotSame(current, PyNumericAccumulator.Value(state));
    }

    [Theory]
    [InlineData(long.MaxValue, 1L)]
    [InlineData(long.MinValue, -1L)]
    public void OverflowFallsBackToBigInteger(long initial, long step)
    {
        object state = PyNumericAccumulator.Create(initial);
        PyNumericAccumulator.Add(state, step);
        Assert.Equal(new BigInteger(initial) + step,
            Assert.IsType<BigInteger>(PyNumericAccumulator.Value(state)));
        PyNumericAccumulator.Add(state, -step);
        Assert.Equal(initial, PyNumericAccumulator.Value(state));
    }

    [Fact]
    public void BooleanFloatAndFailureUseTheDynamicProtocol()
    {
        object state = PyNumericAccumulator.Create(4L);
        PyNumericAccumulator.Add(state, true);
        Assert.Equal(5L, PyNumericAccumulator.Value(state));
        PyNumericAccumulator.Add(state, 0.5);
        Assert.Equal(5.5, PyNumericAccumulator.Value(state));
        object? original = PyNumericAccumulator.Value(state);
        Assert.Throws<PyTypeError>(() => PyNumericAccumulator.Add(state, "bad"));
        Assert.Same(original, PyNumericAccumulator.Value(state));
    }

    [Fact]
    public void ReflectedAndInPlaceMethodsPreserveReturnedObjectIdentity()
    {
        var cls = new PyClass("Custom");
        var custom = new PyInstance(cls);
        object sharedInteger = 1000L;
        int calls = 0;
        cls.SetAttr("__radd__", PyRuntimeDataclassTests.Fn("__radd__", ["self", "other"], _ => custom));
        cls.SetAttr("__iadd__", PyRuntimeDataclassTests.Fn("__iadd__", ["self", "other"], _ =>
        {
            calls++;
            return sharedInteger;
        }));
        object state = PyNumericAccumulator.Create(1L);
        PyNumericAccumulator.Add(state, custom);
        Assert.Same(custom, PyNumericAccumulator.Value(state));
        PyNumericAccumulator.Add(state, 2L);
        Assert.Equal(1, calls);
        Assert.Same(sharedInteger, PyNumericAccumulator.Value(state));
        PyNumericAccumulator.Add(state, 3L);
        Assert.Equal(1003L, PyNumericAccumulator.Value(state));
    }
}
