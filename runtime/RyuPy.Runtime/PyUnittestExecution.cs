using System;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>测试生命周期与 LIFO 清理；只分类普通 Exception，退出类异常始终向调用者传播。</summary>
internal static class PyUnittestExecution
{
    private sealed class State { internal object? Result; internal bool Successful = true; }
    private static readonly ConditionalWeakTable<object, State> States = new();

    internal static object Run(object test, object? supplied)
    {
        bool ownResult = supplied is null or PyNone;
        object result = ownResult ? PyUnittest.Call(test, "defaultTestResult")! : supplied!;
        State state = States.GetOrCreateValue(test);
        object? oldResult = state.Result; bool oldSuccess = state.Successful;
        state.Result = result; state.Successful = true;
        if (ownResult) PyUnittest.Call(result, "startTestRun");
        PyUnittest.Call(result, "startTest", test);
        try
        {
            try
            {
                if (Attempt(test, () => PyUnittest.Call(test, "setUp")))
                {
                    try { Attempt(test, () => PyUnittest.Call(test, (string)PyObject.GetAttr(test, "_testMethodName")!)); }
                    finally { Attempt(test, () => PyUnittest.Call(test, "tearDown")); }
                }
            }
            finally { Cleanups(test); }
            if (state.Successful) PyUnittest.Call(result, "addSuccess", test);
            return result;
        }
        finally
        {
            try { PyUnittest.Call(result, "stopTest", test); }
            finally
            {
                state.Result = oldResult; state.Successful = oldSuccess;
                if (ownResult) PyUnittest.Call(result, "stopTestRun");
            }
        }
    }

    internal static void Install(PyClass cls)
    {
        var signature = PySignature.Create("addCleanup", ["self", "function", "args", "kwargs"],
            ["pos", "pos", "varpos", "varkw"], ["0", "0", "0", "0"]);
        cls.SetAttr("addCleanup", new PyFunction("addCleanup", signature, (self, args, names, values) =>
        {
            object?[] bound = PyBinder.Bind(signature, args, names, values, self.Defaults);
            ((PyList)PyObject.GetAttr(bound[0], "_cleanups")!).Append(new PyTuple([bound[1], bound[2], bound[3]]));
            return PyNone.Instance;
        }, [], false));
        cls.SetAttr("doCleanups", PyUnittest.Method("doCleanups", ["self"], [null], 1, a => Cleanups(a[0]!)));
    }

    private static bool Cleanups(object test)
    {
        var cleanups = (PyList)PyObject.GetAttr(test, "_cleanups")!;
        bool successful = true;
        while (cleanups.Count > 0)
        {
            var entry = (PyTuple)cleanups.GetItem(-1)!; cleanups.DelItem(-1);
            var kwargs = (PyDict)entry.Items[2]!;
            var names = new string[kwargs.Count]; var values = new object?[kwargs.Count]; int i = 0;
            foreach (var (key, value) in kwargs.Entries()) { names[i] = (string)key!; values[i++] = value; }
            if (!Attempt(test, () => PyOps.CallValue(entry.Items[0], ((PyTuple)entry.Items[1]!).Items, names, values)))
                successful = false;
        }
        return successful;
    }

    private static bool Attempt(object test, Func<object?> action)
    {
        long depth = PyTraceback.CaptureDepth();
        try { action(); return true; }
        catch (PyException error)
        {
            // 先保留抛错时的完整 Python 栈，再清掉展开的被调用帧，避免污染下个测试。
            PyErrors.PrepareRaise(error); PyTraceback.PopTo(depth + 1);
            object? previous = PyErrors.EnterHandler(error);
            try
            {
                State state = States.GetOrCreateValue(test); state.Successful = false;
                if (state.Result is not null)
                {
                    bool failure = PyUnittest.Matches(error, PyObject.GetAttr(test, "failureException"));
                    object? type = error.PythonClass ?? (object)PyType.Get(error.PyTypeName);
                    PyUnittest.Call(state.Result, failure ? "addFailure" : "addError", test,
                        new PyTuple([type, error, PyNone.Instance]));
                }
            }
            finally { PyErrors.LeaveHandler(previous); }
            return false;
        }
        catch (PyBaseException error)
        {
            PyErrors.PrepareRaise(error); PyTraceback.PopTo(depth + 1);
            throw;
        }
    }
}
