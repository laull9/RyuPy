// cell 的共享引用和空值异常；函数工厂与多层环境由编译器 e2e 差分验证。
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyClosureTests
{
    [Fact]
    public void EmptyCellDistinguishesLocalAndFreeErrors()
    {
        object cell = PyOps.CellNew();
        var local = Assert.Throws<PyUnboundLocalError>(() => PyOps.CellGet(cell, "value"));
        var free = Assert.Throws<PyNameError>(() => PyOps.CellGetFree(new[] { cell }, 0, "value"));
        Assert.Contains("local variable 'value'", local.Message);
        Assert.Contains("free variable 'value'", free.Message);
    }

    [Fact]
    public void EnvironmentsShareCellsButFactoriesAllocateNewOnes()
    {
        object cell = PyOps.CellNew();
        object[] first = { cell };
        object[] second = { cell };
        object[] separate = { PyOps.CellNew() };
        PyOps.CellSet(cell, PyNone.Instance);
        Assert.Same(PyNone.Instance, PyOps.CellGetFree(first, 0, "value"));
        object value = new PyList();
        PyOps.CellSet(cell, value);
        Assert.Same(value, PyOps.CellGetFree(second, 0, "value"));
        Assert.Throws<PyNameError>(() => PyOps.CellGetFree(separate, 0, "value"));
        // except 绑定清理后，所有持有该 cell 的闭包都看见未赋值哨兵。
        PyOps.CellSet(cell, PyUnbound.Instance);
        Assert.Throws<PyNameError>(() => PyOps.CellGetFree(first, 0, "value"));
    }
}
