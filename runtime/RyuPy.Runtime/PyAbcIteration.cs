// ABC mixin 的有限生成器边界：用户体泄漏StopIteration必须PEP479转换，控制协议明确未支持。
namespace RyuPy.Runtime;

internal static class PyAbcIteration
{
    internal static PyBaseException EscapingStop(PyStopIteration stop)
    {
        PyErrors.PrepareRaise(stop);
        PyBaseException error = new PyRuntimeError("generator raised StopIteration").WithCause(stop);
        error.Context = stop;
        PyTraceback.Attach(error);
        return error;
    }
    internal static PyBaseException UnsupportedControl(string name) =>
        new PyNotImplementedError("ABC mixin generator control " + name + " is not supported");
}
