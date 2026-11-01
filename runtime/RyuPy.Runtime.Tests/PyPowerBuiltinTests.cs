using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>一等 pow 的签名、整模幂及模逆错误，不让部分函数绕过参数检查。</summary>
public class PyPowerBuiltinTests
{
    [Fact]
    public void ModularPowerAndInversePreserveNegativeModulus()
    {
        Assert.Equal(2L, PyOps.CallBuiltin("pow", [2L, 10L, 7L], [], []));
        Assert.Equal(5L, PyOps.CallBuiltin("pow", [3L, -1L, 7L], [], []));
        Assert.Equal(-3L, PyOps.CallBuiltin("pow", [3L, -2L, -7L], [], []));
        Assert.Equal(0L, PyOps.CallBuiltin("pow", [2L, -1L, 1L], [], []));
        Assert.Throws<PyValueError>(() => PyOps.CallBuiltin("pow", [2L, -1L, 4L], [], []));
        Assert.Throws<PyValueError>(() => PyOps.CallBuiltin("pow", [2L, 3L, 0L], [], []));
    }

    [Fact]
    public void FloatErrorsAndUnsupportedComplexAreExplicit()
    {
        Assert.Throws<PyZeroDivisionError>(() => PyOps.CallBuiltin("pow", [0.0, -1L], [], []));
        Assert.Throws<PyOverflowError>(() => PyOps.CallBuiltin("pow", [1e308, 2L], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallBuiltin("pow", [-1.0, 0.5], [], []));
        Assert.Equal(double.PositiveInfinity, PyPowerBuiltin.FloatPower(0.0, double.NegativeInfinity));
    }

    [Fact]
    public void KeywordsUseTheSameSignatureAsPositionalCalls()
    {
        Assert.Equal(81L, PyOps.CallBuiltin("pow", [], ["base", "exp"], [3L, 4L]));
        Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin("pow", [2L], ["base", "exp"], [3L, 4L]));
        Assert.Throws<PyTypeError>(() => PyOps.CallBuiltin("pow", [2L], [], []));
    }
}
