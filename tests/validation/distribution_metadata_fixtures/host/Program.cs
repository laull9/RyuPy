// 每个命令由独立dotnet进程执行；不通过reset篡改单Runtime域生命周期。
using RyuPy.Runtime;
static class Owner { }
static class OtherOwner { }
static class Program
{
    static readonly string Empty = "{\"schema_version\":1,\"mode\":\"closed_snapshot\",\"roots\":[],\"distributions\":[]}";
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Rejected(Action action)
    {
        try { action(); } catch (PyNotImplementedError) { return; }
        throw new Exception("要求明确NotImplementedError");
    }
    static object? Call(object? function, params object?[] args) => PyOps.CallValue(function, args, [], []);
    static PyModule Metadata() => PyModules.Import("importlib.metadata");
    static object? Points() => Call(Metadata().GetAttr("entry_points"));
    static void Main(string[] args)
    {
        string mode = args[0];
        // 轻量host首先自检真实运行时加载与参数路由，不配置/导入metadata。
        if (mode == "smoke") { Check(typeof(PyDistributionSnapshot).Assembly.GetName().Name == "RyuPy.Runtime", "runtime"); Console.WriteLine("smoke ok"); return; }
        string payload = File.ReadAllText(args[1]);
        PySystem.Initialize("metadata-host.py", []);
        switch (mode)
        {
            case "unconfigured":
                Metadata(); Rejected(() => Points()); break;
            case "empty":
                PyDistributionSnapshot.Configure(typeof(Owner), Empty);
                Check(PyOps.Len(Points()) == 0L && !PyOps.Truthy(Points()), "配置空必须真实为空"); break;
            case "late":
                Metadata(); Rejected(() => PyDistributionSnapshot.Configure(typeof(Owner), payload));
                Rejected(() => Points()); break;
            case "same":
                PyDistributionSnapshot.Configure(typeof(Owner), payload);
                var metadata = Metadata(); var marker = new object(); metadata.SetAttr("import_module", marker);
                PyDistributionSnapshot.Configure(typeof(Owner), payload);
                Check(ReferenceEquals(metadata.GetAttr("import_module"), marker), "幂等不恢复重绑"); break;
            case "owner-conflict":
            case "payload-conflict":
                PyDistributionSnapshot.Configure(typeof(Owner), payload);
                Rejected(() => PyDistributionSnapshot.Configure(mode == "owner-conflict" ? typeof(OtherOwner) : typeof(Owner),
                    mode == "owner-conflict" ? payload : Empty));
                Check(PyOps.Len(Points()) == 6L, "冲突污染旧快照"); break;
            case "failed-init": FailedMetadata(payload, true); break;
            case "failed-init-unconfigured": FailedMetadata(payload, false); break;
            case "failed-target": FailedTarget(payload); break;
            case "source-parent": SourceParent(); break;
            default: throw new Exception("未知host模式");
        }
        Console.WriteLine(mode + " ok");
    }
    // host-only故障注入；不是CP用户getter语义，不改变native/source准入。
    sealed class OnceComparer : IEqualityComparer<string>
    {
        public Action? Armed;
        public bool Equals(string? left, string? right) => StringComparer.Ordinal.Equals(left, right);
        public int GetHashCode(string key)
        {
            if (key == "import_module" && Armed is { } callback)
            {
                Armed = null; // 关闭后才回调，重试及嵌套操作不会再次注入。
                callback();
            }
            return StringComparer.Ordinal.GetHashCode(key);
        }
    }
    static void FailedMetadata(string payload, bool configured)
    {
        if (configured) PyDistributionSnapshot.Configure(typeof(Owner), payload);
        PyModule? escaped = null; var marker = new object();
        var real = PyNativeFunctions.Create("import_module", ["name", "package"], [null, PyNone.Instance], 1,
            values => PyImportlib.ImportModule(values[0], values[1]));
        var comparer = new OnceComparer();
        var attributes = new Dictionary<string, object?>(comparer) { ["import_module"] = real };
        PyModules.RegisterNative("importlib", attributes);
        // 先导入父，再armed，唯一注入点是真实metadata初始化读取父import_module。
        PyModules.Import("importlib");
        comparer.Armed = () =>
        {
            escaped = Metadata(); escaped.SetAttr("import_module", marker);
            throw new PyValueError("host-only first initialization");
        };
        try { Metadata(); throw new Exception("初次必须失败"); } catch (PyValueError) { }
        Check(comparer.Armed is null && escaped is not null, "故障未命中真实父绑定读取");
        Rejected(() => PyDistributionSnapshot.Configure(typeof(OtherOwner), payload));
        var current = Metadata();
        Check(!ReferenceEquals(escaped, current), "失败缓存未移除");
        Check(ReferenceEquals(escaped!.GetAttr("import_module"), marker), "重试改写旧partial模块");
        Check(ReferenceEquals(current.GetAttr("import_module"), real), "重试未捕获新绑定");
        if (configured) Check(PyOps.Len(Points()) == 6L, "重试发现错误");
        else Rejected(() => Points());
    }
    static void SourceParent()
    {
        PyModules.Register("importlib", "importlib", "", () => { },
            _ => throw new Exception("source父不得进入绑定getter"),
            (module, name, value) => PyModules.SetDynamic(module, name, value));
        Rejected(() => Metadata());
    }
    static void FailedTarget(string payload)
    {
        PyDistributionSnapshot.Configure(typeof(Owner), payload);
        int count = 0; PyModule? partial = null;
        PyModules.RegisterNative("probe_pkg", new Dictionary<string, object?>());
        var result = PyNativeFunctions.Create("factory", [], [], 0, _ => 42L);
        PyModules.Register("probe_pkg.target", "probe_pkg", "", () =>
        {
            count++; partial = PyModules.Import("probe_pkg.target");
            if (count == 1) throw new PyValueError("target first initialization");
        }, name => name == "factory" ? result : throw new PyAttributeError(name),
            (module, name, value) => PyModules.SetDynamic(module, name, value));
        object? point = PyOps.GetItem(Points(), "same");
        try { Call(PyObject.GetAttr(point, "load")); throw new Exception("初次必须失败"); } catch (PyValueError) { }
        PyModule? oldPartial = partial;
        object? loaded = Call(PyObject.GetAttr(point, "load"));
        Check(count == 2 && !ReferenceEquals(oldPartial, partial), "target缓存失败重试");
        Check(Equals(Call(loaded), 42L), "普通调用返回对象失败");
        Check(ReferenceEquals(loaded, Call(PyObject.GetAttr(point, "load"))) && count == 2, "重复load重新初始化");
    }
}
