// CP1b独立宿主探针：确定释放、跨堆环显式断开、稳态注册和关机后迟到释放。
using RyuPy.CPython;
using RyuPy.Runtime;

if (args.Length != 2) throw new ArgumentException("需要配置路径和测试名称");
// 在安装provider前完成原生冷注册并保留格式化入口，专门检验合并双域帧的limit合同。
if (args[1] == "mixed-limit") PyNativeModules.Register();
object? nativeFormat = args[1] == "mixed-limit"
    ? PyObject.GetAttr(PyModules.Import("traceback"), "format_exc") : null;
Bootstrap.Start(args[0], ["生命周期验收"]);
CPythonHost host = Bootstrap.Host;
object fixture = host.Import("bridge_fixture");
object hold = PyObject.GetAttr(fixture, "hold_callback")!;
object clear = PyObject.GetAttr(fixture, "clear_holder")!;

static object? Call(object target, params object?[] values) => PyOps.CallValue(target, values, [], []);
static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

if (args[1] == "steady-state")
{
    HostStatistics before = host.Statistics;
    for (int index = 0; index < 256; index++)
    {
        // holder→Python持有托管callback→闭包持有holder，构成真实跨堆环。
        object? holder = null;
        PyFunction callback = PyNativeFunctions.Create("cycle", [], [], 0, _ => holder);
        holder = Call(hold, callback)!;
        Require(host.Statistics.ManagedRoots == before.ManagedRoots + 1, "Python持有callback时缺少托管根");
        Call(clear, holder);
        host.Unregister(callback);
        host.ReleaseObject(holder);
        holder = null;
        host.DrainReferences();
        HostStatistics after = host.Statistics;
        Require(after.ManagedRoots == before.ManagedRoots, "显式断环后托管注册未恢复稳态");
        Require(after.Handles == before.Handles, "显式释放后句柄未恢复稳态");
        Require(after.PendingReleases == 0 && after.ActiveLeases == 0, "批次结束后仍有释放队列或租约");
    }
    host.ReleaseObject(hold);
    host.ReleaseObject(clear);
    host.ReleaseObject(fixture);
    host.DrainReferences();
    Bootstrap.Stop();
    Require(host.Statistics.ManagedRoots == 0 && host.Statistics.Handles == 0, "关闭后仍有注册或句柄");
    Console.WriteLine("CP1b 跨堆环显式断开及256批稳态通过");
}
else if (args[1] == "exception-release")
{
    object invoke = PyObject.GetAttr(fixture, "call_noargs")!;
    void FailAndRelease(string message, bool releaseMessage)
    {
        var marker = new PyValueError(message);
        PyFunction callback = PyNativeFunctions.Create("throws", [], [], 0, _ => throw marker);
        try
        {
            Call(invoke, callback);
            throw new InvalidOperationException("回调应抛错");
        }
        catch (PyBaseException error)
        {
            Require(ReferenceEquals(marker, error), "异常释放路径失去原始实例");
            host.ReleaseObject(error);
        }
        host.Unregister(callback);
        if (releaseMessage) host.ReleaseObject(message);
        host.DrainReferences();
    }
    // 消息是跨批次共享的原始对象；其首次身份缓存属于消息作用域，独立暖机后冻结稳态。
    const string sharedMessage = "异常释放";
    FailAndRelease(sharedMessage, false);
    HostStatistics before = host.Statistics;
    for (int index = 0; index < 128; index++)
    {
        FailAndRelease(sharedMessage, false);
        HostStatistics after = host.Statistics;
        Require(after.Handles == before.Handles && after.ManagedRoots == before.ManagedRoots,
            $"异常路径释放后句柄/回调根未恢复稳态：批次{index}，之前{before}，之后{after}");
        Require(after.PendingReleases == 0, "异常路径仍有释放积压");
    }
    host.ReleaseObject(sharedMessage);
    host.DrainReferences();
    before = host.Statistics;
    for (int index = 0; index < 128; index++)
    {
        // 每批唯一消息对象必须显式结束自身作用域，异常释放不能递归误伤共享参数。
        string uniqueMessage = "动态异常:" + index;
        FailAndRelease(uniqueMessage, true);
        HostStatistics after = host.Statistics;
        Require(after.Handles == before.Handles && after.ManagedRoots == before.ManagedRoots
            && after.PendingReleases == 0,
            $"动态唯一异常消息作用域未恢复稳态：批次{index}，之前{before}，之后{after}");
    }
    Bootstrap.Stop();
    Console.WriteLine("CP1b 128批回调异常确定释放通过");
}
else if (args[1] == "numpy-lifetime")
{
    object numpy = host.Import("numpy");
    object factory = PyObject.GetAttr(numpy, "frompyfunc")!;
    // arity是跨批次共享的原始托管对象，先固定其身份，最终显式结束作用域。
    object arity = 1L;
    void RunUfunc(bool fail, string message)
    {
        var marker = new PyValueError(message);
        int calls = 0;
        PyFunction callback = PyNativeFunctions.Create("numpy_callback", ["x"], [null], 1, _ =>
        {
            calls++;
            if (fail) throw marker;
            return PyNone.Instance;
        });
        object ufunc = Call(factory, callback, arity, arity)!;
        try
        {
            Call(ufunc, PyNone.Instance);
            Require(!fail, "NumPy异常回调应抛错");
        }
        catch (PyBaseException error)
        {
            Require(fail && ReferenceEquals(marker, error), "NumPy异常实例未往返");
            host.ReleaseObject(error);
            host.ReleaseObject(message);
        }
        Require(calls == 1, "NumPy未实际执行一次托管回调");
        host.ReleaseObject(ufunc);
        host.Unregister(callback);
        host.DrainReferences();
    }
    RunUfunc(false, "暖机");
    HostStatistics before = host.Statistics;
    for (int index = 0; index < 128; index++)
    {
        RunUfunc(false, "成功");
        RunUfunc(true, "NumPy唯一消息:" + index);
        HostStatistics after = host.Statistics;
        Require(after.Handles == before.Handles && after.ManagedRoots == before.ManagedRoots
            && after.PendingReleases == 0,
            $"真实NumPy ufunc生命周期未恢复稳态：批次{index}，之前{before}，之后{after}");
    }
    host.ReleaseObject(arity);
    Bootstrap.Stop();
    Console.WriteLine("CP3 128批真实NumPy成功/异常回调生命周期通过");
}
else if (args[1] == "disposal")
{
    object retain = PyObject.GetAttr(fixture, "retain_object")!;
    object get = PyObject.GetAttr(fixture, "get_retained")!;
    object drop = PyObject.GetAttr(fixture, "drop_retained")!;
    object value = Call(retain)!;
    object alias = value;
    host.ReleaseObject(value);
    host.DrainReferences();
    int handles = host.Statistics.Handles;
    host.ReleaseObject(alias);
    host.DrainReferences();
    Require(host.Statistics.Handles == handles, "重复释放改变句柄数量");
    try
    {
        PyOps.GetItem(alias, "value");
        throw new InvalidOperationException("已释放别名仍可调用");
    }
    catch (ObjectDisposedException) { }
    object fresh = Call(get)!;
    Require(!ReferenceEquals(fresh, value), "弱身份缓存返回已释放代理");
    Require(PyCast.ToInt64(PyOps.GetItem(fresh, "value")) == 42, "重新取得的代理不可使用");
    Call(drop);
    host.ReleaseObject(fresh);
    host.ReleaseObject(retain);
    host.ReleaseObject(get);
    host.ReleaseObject(drop);
    Bootstrap.Stop();
    Console.WriteLine("CP1b 确定释放、别名拒绝与弱缓存更新通过");
}
else if (args[1] == "mixed-limit")
{
    object chain = PyObject.GetAttr(fixture, "chain")!;
    var marker = new PyValueError("limit叶异常");
    long initialDepth = PyTraceback.CaptureDepth();
    PyTraceback.PushFrame("compiled_limit.py", "compiled_entry");
    PyTraceback.SetLine(10);
    PyFunction callback = PyNativeFunctions.Create("compiled_leaf", [], [], 0, _ =>
    {
        long depth = PyTraceback.CaptureDepth();
        PyTraceback.PushFrame("compiled_limit.py", "compiled_leaf");
        PyTraceback.SetLine(20);
        try { throw (PyBaseException)PyErrors.PrepareRaise(marker); }
        finally { PyTraceback.PopTo(depth + 1); }
    });
    try
    {
        Call(chain, callback);
        throw new InvalidOperationException("包装异常未传播");
    }
    catch (PyRuntimeError error)
    {
        object? previous = PyErrors.EnterHandler(error);
        try
        {
            foreach (long limit in new long[] { 0, 1, -1 })
            {
                string text = (string)Call(nativeFormat!, limit, false)!;
                int frames = text.Split('\n').Count(line => line.StartsWith("  File ", StringComparison.Ordinal));
                Require(frames == (limit == 0 ? 0 : 1), $"双域总帧limit={limit}错误：{text}");
                Require(text.Contains("RuntimeError: Python 包装异常", StringComparison.Ordinal), "limit删除了异常本体");
                Require(!text.Contains("limit叶异常", StringComparison.Ordinal), "chain=False仍展示cause");
                if (limit == 1) Require(text.Contains("in compiled_entry", StringComparison.Ordinal), "limit=1未选最外编译帧: " + text);
                if (limit == -1) Require(text.Contains("in chain", StringComparison.Ordinal), "limit=-1未选最内解释帧: " + text);
            }
        }
        finally { PyErrors.LeaveHandler(previous); }
        host.ReleaseObject(error);
    }
    finally { PyTraceback.PopTo(initialDepth + 1); }
    host.Unregister(callback);
    host.ReleaseObject(marker);
    Bootstrap.Stop();
    Console.WriteLine("CP1b 混合栈总帧limit0/1/-1通过");
}
else if (args[1] == "late-release")
{
    object late = Call(hold, PyNone.Instance)!;
    Bootstrap.Stop();
    long before = host.Statistics.LateReleases;
    host.ReleaseObject(late);
    HostStatistics after = host.Statistics;
    Require(after.State == "closed" && after.LateReleases == before + 1, "迟到释放没有只登记托管计数");
    Require(after.ActiveLeases == 0 && after.PendingReleases == 0, "关闭后迟到释放重新进入解释器");
    try
    {
        PyOps.GetItem(late, "callback");
        throw new InvalidOperationException("释放后仍允许对象操作");
    }
    catch (ObjectDisposedException) { }
    Console.WriteLine("CP1b 关机后迟到释放通过");
}
else
{
    Bootstrap.Stop();
    throw new ArgumentException("未知生命周期测试：" + args[1]);
}
