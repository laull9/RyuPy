// 仅托管验证宿主：加载真实Session产物，不伪造source/signature、不从basename选择执行代码。
using System.Reflection;
using System.Runtime.ExceptionServices;
using RyuPy.Runtime;

static class Program
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static readonly PyList Events = new();
    static readonly object Missing = new();
    static T Managed<T>(Func<T> body)
    {
        long depth = PyTraceback.CaptureDepth();
        object? handler = PyErrors.EnterHandler(null);
        object? previous = PyErrors.Current;
        try { return body(); }
        catch (PyBaseException error) { PyTraceback.Attach(error); throw; }
        finally
        {
            PyTraceback.PopTo(depth + 1);
            PyErrors.LeaveHandler(handler);
            Require(PyTraceback.CaptureDepth() == depth && ReferenceEquals(PyErrors.Current, previous), "宿主未恢复原depth/handler");
        }
    }
    static object? Call(object? function, params object?[] values) => Managed(() => PyOps.CallValue(function, values, [], []));
    static object? Attr(object? value, string name) => PyObject.GetAttr(value, name);
    static void Out(params object?[] values) => Call(PyOps.BuiltinFunction("print"), values);
    static object? Repr(object? value) => Call(PyOps.BuiltinFunction("repr"), value);
    static object? TypeName(object? value) => Attr(Call(PyOps.BuiltinFunction("type"), value), "__name__");
    static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
    static object State(PyModule module)
    {
        object table = typeof(PyModuleInstances).GetField("States", Any)!.GetValue(null)!;
        object?[] args = [module, null];
        Require((bool)table.GetType().GetMethod("TryGetValue")!.Invoke(table, args)!, "真实States无目标");
        return args[1]!;
    }
    static object Code(PyModule module) => State(module).GetType().GetField("Code", Any)!.GetValue(State(module))!;
    static string CodeName(object code) => (string)code.GetType().GetProperty("Name", Any)!.GetValue(code)!;
    static void Execute(object code, PyModule target)
    {
        // 精确调用既有内部Execute(code,target)测试入口，不更改产品ABI或State.Code。
        var method = typeof(PyModuleInstances).GetMethod("Execute", Any, null, [code.GetType(), typeof(PyModule)], null)!;
        Managed(() =>
        {
            try { return method.Invoke(null, [code, target]); }
            catch (TargetInvocationException error) when (error.InnerException is { } original)
            { ExceptionDispatchInfo.Capture(original).Throw(); throw; }
        });
    }
    static PyInstance Marker(PyModule target, object? value)
    {
        var cls = new PyClass("EntryMarker");
        cls.SetAttr("__bool__", PyNativeFunctions.Create("__bool__", ["self"], [null], 1, _ =>
        {
            Events.Items.Add("entry-bool");
            if (ReferenceEquals(value, Missing)) PyObject.DelAttr(target, "__name__");
            else target.SetAttr("__name__", value);
            throw new PyAttributeError("entry-attribute");
        }));
        PyOps.FinalizeOrdinaryClass(cls);
        return new PyInstance(cls);
    }
    static int Main(string[] args)
    {
        Require(args.Length == 2 && args[0] is "old" or "new", "参数必须为角色和真实Session生成PE路径");
        bool newRole = args[0] == "new";
        var assembly = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        var entry = assembly.EntryPoint ?? throw new InvalidOperationException("缺实际入口");
        entry.Invoke(null, entry.GetParameters().Length == 0 ? null : [Array.Empty<string>()]);
        object sourceX = Code(PyModuleInstances.Create("source_x"));
        Require(CodeName(sourceX) == "source_x", "X代码身份错误");
        var cases = new (string, object?)[] { ("text", "entry-fallback"), ("empty", ""),
            ("zero", 0L), ("list", new PyList()), ("missing", Missing), ("none-control", PyNone.Instance) };
        foreach (var (label, value) in cases)
        {
            var target = PyModuleInstances.Create("source_y");
            object sourceY = Code(target);
            Require(CodeName(sourceY) == "source_y", "目标State.Code不是Y");
            Managed(() => { PyModuleInstances.Execute(target); return 0; });
            Events.Items.Clear();
            target.SetAttr("__name__", Marker(target, value));
            try
            {
                Execute(sourceX, target);
                object? actual = Attr(target.GetAttr("Top"), "__module__");
                Out("entry-fallback", label, "ok", TypeName(actual), Repr(actual), ReferenceEquals(actual, value), Events);
            }
            catch (PyBaseException error)
            { Out("entry-fallback", label, "error", TypeName(error), Call(PyOps.BuiltinFunction("str"), error), Events); }
            Require(ReferenceEquals(Code(target), sourceY), "X执行篡改目标State.Code");
        }
        var module = PyModuleInstances.Create("source_y");
        object preservedY = Code(module);
        object? loader = module.GetAttr("__loader__"), spec = module.GetAttr("__spec__");
        module.SetAttr("__name__", "entry-name");
        Execute(sourceX, module);
        object? old = module.GetAttr("emit");
        PyObject.SetAttr(old, "__module__", "old-module");
        var attributes = PyWarnings.CreateAttributes();
        object? context = PyOps.CallValue(attributes["catch_warnings"], [], ["record"], [true]);
        var records = (PyList)Call(Attr(context, "__enter__"))!;
        try
        {
            Call(attributes["simplefilter"], "module");
            Out("xy-top", module.GetAttr("source_marker"), Attr(module.GetAttr("Top"), "__module__"));
            Out("xy-old", Call(old));
            PyObject.SetAttr(old, "__module__", Call(module.GetAttr("NativeMarker")));
            try { Call(old); Out("native-truthy", "old-success", false); Require(!newRole, "native Truthy应抛ValueError"); }
            catch (PyValueError error)
            {
                bool found = error.Frames.Any(frame => frame.Function == "__bool__" && Path.GetFileName(frame.File) == "source_x.py");
                Out("native-truthy", "ValueError", found);
                Require(!newRole || found, "原生ValueError缺失真实__bool__帧");
            }
            PyObject.SetAttr(old, "__module__", "old-module");
            Out("native-recovery", Call(old));
            module.SetAttr("__name__", "reentry-name");
            Execute(sourceX, module);
            object? fresh = module.GetAttr("emit");
            PyObject.SetAttr(fresh, "__module__", "new-module");
            object? globals = module.GetAttr("__dict__");
            Out("xy-functions", Call(old), Call(fresh), ReferenceEquals(Attr(old, "__globals__"), globals),
                ReferenceEquals(Attr(fresh, "__globals__"), globals));
            Managed(() => { PyModuleInstances.Execute(module); return 0; });
            Out("xy-y", Call(module.GetAttr("emit_y")));
            Out("xy-count", (long)records.Count);
            Require(!ReferenceEquals(old, fresh), "旧函数被覆盖而非真实新定义");
        }
        finally { Call(Attr(context, "__exit__"), PyNone.Instance, PyNone.Instance, PyNone.Instance); }
        Require(ReferenceEquals(Code(module), preservedY) && CodeName(preservedY) == "source_y", "目标Code必须仍是Y");
        Require(ReferenceEquals(module.GetAttr("__loader__"), loader) && ReferenceEquals(module.GetAttr("__spec__"), spec), "X改了目标metadata");
        Managed(() => { PyModuleInstances.Execute(module); return 0; });
        Out("xy-default-code", module.GetAttr("source_marker"));
        Out("XY done");
        return 0;
    }
}
