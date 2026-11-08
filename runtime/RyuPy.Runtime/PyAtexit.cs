using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>显式进程退出回调；保留注册槽位，支持LIFO、相等注销与固定3.12的有限注册变化语义。</summary>
public static class PyAtexit
{
    private sealed record Entry(object? Function, object?[] Args, string[] Names, object?[] Values);
    private static readonly List<Entry?> Entries = new();
    internal static bool HasCallbacks => Entries.Exists(entry => entry is not null);
    private static readonly Lazy<Dictionary<string, object?>> Attributes = new(Build);
    /// <summary>原生模块单例；不注册额外CLR ProcessExit事件，避免重复执行。</summary>
    public static Dictionary<string, object?> CreateAttributes() => Attributes.Value;
    private static Dictionary<string, object?> Build()
    {
        var signature = PySignature.Create("register", ["func", "args", "kwargs"],
            ["posonly", "varpos", "varkw"], ["0", "0", "0"]);
        var register = new PyFunction("register", signature, (self, args, names, values) =>
        {
            object?[] bound = PyBinder.Bind(signature, args, names, values, self.Defaults);
            object? function = bound[0];
            if (!PyOps.Truthy(PyOps.CallBuiltin("callable", [function], [], [])))
                throw new PyTypeError("the first argument must be callable");
            var kwargs = (PyDict)bound[2]!;
            var keys = new string[kwargs.Count]; var items = new object?[kwargs.Count]; int i = 0;
            foreach (var (key, value) in kwargs.Entries()) { keys[i] = (string)key!; items[i++] = value; }
            // 绑定器已复制*args元组；只保存对象引用，不深复制用户参数。
            Entries.Add(new Entry(function, ((PyTuple)bound[1]!).Items, keys, items));
            return function;
        }, [], false);
        return new(StringComparer.Ordinal)
        {
            ["register"] = register,
            ["unregister"] = PyNativeFunctions.Create("unregister", ["func"], [null], 1,
                a => { Unregister(a[0]); return PyNone.Instance; }, ["posonly"]),
            ["_clear"] = PyNativeFunctions.Create("_clear", [], [], 0, _ => { Clear(); return PyNone.Instance; }),
            ["_ncallbacks"] = PyNativeFunctions.Create("_ncallbacks", [], [], 0, _ => (long)Entries.Count),
            ["_run_exitfuncs"] = PyNativeFunctions.Create("_run_exitfuncs", [], [], 0, _ => { RunExitFunctions(); return PyNone.Instance; }),
        };
    }
    /// <summary>相等比较从旧到新；保留空槽，失败不回滚此前已经删除的回调。</summary>
    public static void Unregister(object? function)
    {
        for (int i = 0; i < Entries.Count; i++)
        {
            Entry? entry = Entries[i];
            if (entry is not null && PyHash.Equal(entry.Function, function))
            {
                // __eq__可以重入清表/注册；只能删除仍指向本entry的槽，不能误删新注册项。
                if (i < Entries.Count && ReferenceEquals(Entries[i], entry)) Entries[i] = null;
            }
        }
    }
    /// <summary>释放全部回调引用；不会关闭未注册的用户流。</summary>
    public static void Clear() => Entries.Clear();
    /// <summary>按本轮初始倒序下标执行；不提前移除当前回调，保留有限重入行为。</summary>
    public static void RunExitFunctions()
    {
        try
        {
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                if (i >= Entries.Count || Entries[i] is not Entry entry) continue;
                // EnterHandler(null)只读取当前token，不清空：手动运行仍继承调用者except上下文。
                long depth = PyTraceback.CaptureDepth(); object? context = PyErrors.EnterHandler(null);
                try { PyOps.CallValue(entry.Function, entry.Args, entry.Names, entry.Values); }
                catch (Exception failure)
                {
                    PyBaseException error = failure as PyBaseException ?? new PyRuntimeError(failure.Message);
                    PyErrors.PrepareRaise(error);
                    PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context);
                    Report(entry.Function, error, depth, context);
                }
                finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
            }
        }
        finally { Clear(); }
    }
    private static void Report(object? function, PyBaseException error, long depth, object? context)
    {
        try
        {
            string callable;
            try { callable = PyRepr.Repr(function); }
            catch (Exception) { callable = "<object repr() failed>"; }
            finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
            string traceback;
            try { traceback = PyTraceback.FormatSingle(error); }
            catch (Exception) { traceback = error.PyTypeName + ": <exception str() failed>\n"; }
            finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
            string text = "Exception ignored in atexit callback: " + callable + "\n" + traceback;
            try { PySystem.Write(PySystem.Stderr, text); PySystem.Flush(PySystem.Stderr); }
            catch (Exception)
            {
                // 用户stderr失败时只走宿主兜底，不递归调用坏流，也不改变原退出码。
                try { Console.Error.Write(text); Console.Error.Flush(); }
                catch (Exception) { }
            }
        }
        finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
    }
}
