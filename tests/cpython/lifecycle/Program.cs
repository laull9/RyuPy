// CP1b独立宿主探针：确定释放、跨堆环显式断开、稳态注册和关机后迟到释放。
using RyuPy.CPython;
using RyuPy.Runtime;

if (args.Length != 2) throw new ArgumentException("需要配置路径和测试名称");
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
