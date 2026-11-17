// 独立真实程序集 Host 首切验收：每个进程只创建/关闭一个 stock 宿主，不使用共享 xUnit fixture。
// 控制脚本属于测试域；候选业务体是本文件的静态 C# 叶子，原生层只负责协议和所有权。
using System.Runtime.CompilerServices;
using RyuPy.CPython;
using RyuPy.Runtime;

namespace RyuPy.CPython.Tests;

internal static class StockThinProviderRunner
{
    private static long bodies;
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
    private static T Throws<T>(Action body) where T : Exception
    {
        try { body(); } catch (T error) { return error; }
        throw new InvalidOperationException("应当失败：" + typeof(T).Name);
    }
    private static object? Read(IStockThinFunctionLease env, object? argument)
    { bodies++; return env.LoadGlobal("offset"); }
    private static object? ReadBuiltin(IStockThinFunctionLease env, object? argument)
    { bodies++; return env.LoadGlobal("marker"); }
    private static object? NeutralRead(IPyStockThinEnvironment env, object? argument)
    { bodies++; return PyStockThinFunctions.LoadGlobal(env, "answer"); }
    private static object? NeutralEcho(IPyStockThinEnvironment env, object? argument)
    { bodies++; return argument; }
    private static object? Echo(IStockThinFunctionLease env, object? argument)
    { bodies++; return argument; }
    private static object? Fail(IStockThinFunctionLease env, object? argument)
    { bodies++; throw (PyBaseException)argument!; }
    private static object? ClrFail(IStockThinFunctionLease env, object? argument) =>
        throw new InvalidOperationException("CLR 叶子失败");
    private static StockThinFunctionDefinition Definition(string name) => new("runner:" + name,
        "subject", name, "candidate.py", 1);
    private static void Run(CPythonHost host, string source)
    {
        using var lease = host.Enter();
        if (Native.rp_run_entry("<stock-thin-test-control>", source) < 0) host.ThrowError();
    }
    private static nint MainModule(CPythonHost host) => host.Check(Native.rp_op(1, s: "__main__"));
    private static object? Get(CPythonHost host, string name) => host.PythonCall(() =>
    {
        nint main = MainModule(host);
        try { return host.FromOwned(Native.rp_op(2, main, s: name)); }
        finally { Native.rp_decref(main); }
    });
    private static void Put(CPythonHost host, string name, object value) => host.PythonCall(() =>
    {
        nint main = MainModule(host), pointer = host.ToPython(value);
        try { Native.rp_decref(host.Check(Native.rp_op(3, main, pointer, s: name))); }
        finally { Native.rp_decref(pointer); Native.rp_decref(main); }
        return 0;
    });
    private static void Collect(CPythonHost host)
    {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        host.DrainReferences(); Run(host, "gc.collect()");
    }
    private static void Baseline(CPythonHost host, long minimumDestroyed)
    {
        for (int i = 0; i < 5; i++) Collect(host);
        var stats = host.StockThinStatistics;
        Require(stats.NativeLive == 0 && stats.CoreRoots == 0 && stats.OwnedHandles == 0
            && stats.LiveCores == 0 && stats.Destroyed >= minimumDestroyed,
            "finalize 前 native/core 根回零");
        Require(host.Statistics.ManagedRoots == 0 && host.Statistics.PendingReleases == 0,
            "普通强根/owned 队列回零");
        Console.WriteLine($"PASS baseline destroyed={stats.Destroyed} calls={stats.Calls} roots={stats.CoreRoots} owned={stats.OwnedHandles} weak_core={stats.LiveCores} queue={host.Statistics.PendingReleases}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CpOnly(CPythonHost host)
    {
        Run(host, "d={'offset':42}");
        var d = (PythonObject)Get(host, "d")!;
        var f = host.CreateStockThinFunction(d, Definition("read"), Read);
        d.WriteItem("f", f, false); Put(host, "f", f);
        Run(host, "import weakref\nclass Sentinel: pass\nd['sentinel']=Sentinel()\nwatch_f=weakref.ref(f)\nwatch_d=weakref.ref(d['sentinel'])");
        Require(ReferenceEquals(f, Get(host, "f")), "原 F/H 往返身份");
        Require((long)f.Invoke(1L)! == 42, "动态 globals CIL 读");
        Require((long)f.Invoke([], ["x"], [1L])! == 42, "真实 keyword binder");
        long before = bodies;
        Throws<PyTypeError>(() => f.Invoke([], [], []));
        Throws<PyTypeError>(() => f.Invoke([1L], ["x"], [2L]));
        Require(bodies == before, "binder 失败前不进入 CIL body");
        f.SetAttr("__name__", "公开名称");
        Run(host, "import types\nassert type(f) is types.FunctionType\nassert f.__globals__ is d\nassert len(f.__closure__)==1\nassert f.__code__.co_freevars==('p',)\nassert all(not callable(c) for c in f.__code__.co_consts)\nassert f.__code__.co_filename=='<ryupy-stock-thin-A>'\nassert f.__name__=='公开名称'\nf.__module__='public'\nf.__annotations__={'x':list}\nf.marker=d\nassert f.marker is d\nassert f(x=[]) == 42\nd['offset']=43\nassert f(0)==43");
        d.Dispose();
        return new(f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ReflowAndMutation(CPythonHost host)
    {
        var f = (StockThinFunctionHandle)Get(host, "f")!;
        Require(ReferenceEquals(f, Get(host, "f")), "回流重建 H 复用");
        f.Dispose();
        var fresh = (StockThinFunctionHandle)Get(host, "f")!;
        Require(!ReferenceEquals(f, fresh), "disposed H 不复用");
        Throws<ObjectDisposedException>(() => f.Invoke(1L));
        Run(host, "f.__defaults__=()\n");
        Throws<PyNotImplementedError>(() => fresh.Invoke(1L));
        Run(host, "try:\n f(1)\nexcept NotImplementedError:\n pass\nelse:\n raise AssertionError('CP dispatch应拒绝')\nf.__defaults__=None\nf.__kwdefaults__={}\n");
        Throws<PyNotImplementedError>(() => fresh.Invoke(1L));
        Run(host, "f.__kwdefaults__=None\nsaved=f.__code__\ntry:\n f.__code__=saved\nexcept NotImplementedError:\n pass\nelse:\n raise AssertionError('code赋值应拒绝')\nassert f.__code__ is saved\ndel saved\ncopied=types.FunctionType(f.__code__, {'offset':999}, closure=f.__closure__)\ntry:\n copied(0)\nexcept NotImplementedError:\n pass\nelse:\n raise AssertionError('不同globals复制应拒绝')\ndel copied\nassert f(0)==43");
        Throws<PyNotImplementedError>(() => fresh.SetAttr("__code__", fresh.GetAttr("__code__")));
        Run(host, "def ordinary(x): return x\nordinary.__code__=ordinary.__code__.replace()\nassert ordinary(7)==7\ndel ordinary");
        long beforeCellWrite = host.StockThinStatistics.Calls;
        Run(host, "saved_p=f.__closure__[0].cell_contents\nf.__closure__[0].cell_contents=abs\nassert f(-7)==7");
        Throws<PyNotImplementedError>(() => fresh.Invoke(-7L));
        Require(host.StockThinStatistics.Calls == beforeCellWrite, "改 cell 后 CP 按 abs 执行，H 明确拒绝原 core");
        Run(host, "f.__closure__[0].cell_contents=saved_p\ndel saved_p\nassert f(0)==43");
        // H 单独保留真实 F 和原 D；删除 CP 公开根后仍可调用。
        Run(host, "del d, f; gc.collect()");
        Require((long)fresh.Invoke(0L)! == 43, "CLR-only 保留旧 D");
        fresh.Dispose();
        host.DrainReferences();
        var pendingCycle = host.StockThinStatistics;
        Require(pendingCycle.NativeLive == 1 && pendingCycle.CoreRoots == 1 && pendingCycle.OwnedHandles == 0,
            "H 已释放后真实 D→F 环仍需 CP 循环 GC");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ModuleTransactions(CPythonHost host)
    {
        Run(host, "import types,sys,importlib,importlib.util\nm=types.ModuleType('thin_candidate')\nm.offset=20\nsys.modules['thin_candidate']=m\nd=m.__dict__");
        var d = (PythonObject)Get(host, "d")!;
        var old = host.CreateStockThinFunction(d, Definition("old"), Read);
        d.WriteItem("read", old, false);
        Run(host, "del sys.modules['thin_candidate']\ndel m,d\nclass Loader:\n def find_spec(self,name,path=None,target=None):\n  if name=='thin_candidate': return importlib.util.spec_from_loader(name,self)\n def create_module(self,spec): return None\n def exec_module(self,module): module.offset=100\nloader=Loader()\nsys.meta_path.insert(0,loader)\ntry:\n m=importlib.import_module('thin_candidate')\nfinally:\n sys.meta_path.remove(loader)\nd=m.__dict__");
        var freshDictionary = (PythonObject)Get(host, "d")!;
        var fresh = host.CreateStockThinFunction(freshDictionary, Definition("fresh"), Read);
        freshDictionary.WriteItem("read", fresh, false);
        Require(!ReferenceEquals(d, freshDictionary), "真实 importlib 重导创建新 D");
        Require((long)old.Invoke(0L)! == 20 && (long)fresh.Invoke(0L)! == 100, "重导新旧 globals 独立");
        freshDictionary.WriteItem("offset", 200L, false);
        Require((long)old.Invoke(0L)! == 20 && (long)fresh.Invoke(0L)! == 200, "新 D 写入不污染旧 H");
        Run(host, "candidate=m\ndel sys.modules['thin_candidate']\nclass FailingLoader(Loader):\n def create_module(self,spec): return candidate\n def exec_module(self,module): raise RuntimeError('事务失败')\nloader=FailingLoader()\nsys.meta_path.insert(0,loader)\ntry:\n try: importlib.import_module('thin_candidate')\n except RuntimeError as error: assert str(error)=='事务失败'\n else: raise AssertionError('导入应失败')\nfinally:\n sys.meta_path.remove(loader)\nassert 'thin_candidate' not in sys.modules\ndel m,d,candidate,loader,Loader,FailingLoader");
        Require((long)fresh.Invoke(0L)! == 200, "失败回滚后外逃 H 仍持旧 D");
        old.Dispose(); fresh.Dispose(); d.Dispose(); freshDictionary.Dispose();
    }

    // F 捕获定义时 builtins；D 重新绑定仅影响后续 F，字典内部修改仍实时可见。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void BuiltinEnvironment(CPythonHost host)
    {
        Run(host, "b={'marker':[]}\nd={'__builtins__':b}");
        var d = (PythonObject)Get(host, "d")!;
        var old = host.CreateStockThinFunction(d, Definition("old_builtins"), ReadBuiltin);
        Put(host, "f", old);
        Run(host, "assert f(0) is b['marker']\nassert f.__builtins__ is b\nd['__builtins__']={'marker':{}}\nassert f(0) is b['marker']\nb['marker']=None\nassert f(0) is None\nb['marker']=[]\nd['marker']={}\nassert f(0) is d['marker']\ndel d['marker']");
        var fresh = host.CreateStockThinFunction(d, Definition("new_builtins"), ReadBuiltin);
        Put(host, "g", fresh);
        Run(host, "assert g(0) is d['__builtins__']['marker']\nassert g.__builtins__ is d['__builtins__']\nassert g.__builtins__ is not f.__builtins__\ndel b['marker']\ntry:\n f(0)\nexcept NameError:\n pass\nelse:\n raise AssertionError('两层均缺失必须NameError')\ndel b,d,f,g");
        old.Dispose(); fresh.Dispose(); d.Dispose();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DynamicValuesAndFailures(CPythonHost host)
    {
        Run(host, "d={}\nargument=[]");
        var d = (PythonObject)Get(host, "d")!;
        var echo = host.CreateStockThinFunction(d, Definition("echo"), Echo);
        var argument = (PythonObject)Get(host, "argument")!;
        Require(ReferenceEquals(argument, echo.Invoke(argument)), "原生列表往返同身份");
        object scalar = 1000L;
        Require(ReferenceEquals(scalar, echo.Invoke(scalar)), "CLR 标量回流保留原盒身份");
        Require(ReferenceEquals(scalar, echo.Invoke([], ["x"], [scalar])), "CLR keyword 标量回流身份");
        Put(host, "cp_echo", echo);
        Run(host, "for value in (1000,-1000,2**80,1.25,'动态字符串',[],object(),None,True):\n assert cp_echo(value) is value\ndel cp_echo,value");
        d.WriteItem("offset", argument, false);
        var read = host.CreateStockThinFunction(d, Definition("read_alias"), Read);
        Require(ReferenceEquals(argument, read.Invoke(0L)), "globals 返回值别名");
        Put(host, "cp_read", read);
        Run(host, "d['offset']=1000\nassert cp_read(None) is d['offset']\ndel cp_read");
        Run(host, "events=[]\nimport sys\ndef trace(frame,event,arg):\n if frame.f_code.co_filename=='<ryupy-stock-thin-A>': events.append(event)\n return trace\nsys.settrace(trace)");
        var traced = host.CreateStockThinFunction(d, Definition("traced"), Echo);
        Run(host, "assert events==[]");
        Require(ReferenceEquals(argument, traced.Invoke(argument)), "真实 trace 胶水调用身份");
        Run(host, "sys.settrace(None)\nassert events[0]=='call' and events[-1]=='return'\ndel trace,events");
        var list = new PyList([1L]);
        Require(ReferenceEquals(list, echo.Invoke(list)), "托管列表动态身份");
        host.Unregister(list);
        Collect(host);
        var fail = host.CreateStockThinFunction(d, Definition("fail"), Fail);
        var original = new PyValueError("原异常");
        Require(ReferenceEquals(original, Throws<PyValueError>(() => fail.Invoke(original))), "异常原身份");
        host.ReleaseObject(original);
        var clr = host.CreateStockThinFunction(d, Definition("clr_fail"), ClrFail);
        Throws<PyRuntimeError>(() => clr.Invoke(0L));
        clr.Dispose(); fail.Dispose(); echo.Dispose(); read.Dispose(); traced.Dispose(); argument.Dispose();
        // 原生三个阶段注入 + 非精确 dict：所有失败都消费唯一 token。
        for (int stage = 1; stage <= 3; stage++)
            Throws<PyRuntimeError>(() => host.CreateStockThinFunction(d, Definition("injected"), Echo, stage));
        Throws<PyTypeError>(() => host.CreateStockThinFunction(1L, Definition("bad_globals"), Echo));
        Throws<PyTypeError>(() => host.CreateStockThinFunction(d, Definition("bad_name") with { Name = null! }, Echo));
        Throws<ArgumentException>(() => host.CreateStockThinFunction(d, Definition("captured"), (_, _) => d));
        d.Dispose(); Run(host, "del d, argument");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PrepareEscape(CPythonHost host, int kind)
    {
        Run(host, "d={}\nclass EscapeSentinel: pass\nd['sentinel']=EscapeSentinel()");
        var d = (PythonObject)Get(host, "d")!;
        var f = host.CreateStockThinFunction(d, Definition("escaped"), Echo);
        Put(host, "f", f);
        string expression = kind switch
        {
            0 => "f.__closure__[0].cell_contents",
            1 => "f.__closure__[0].cell_contents.__self__",
            _ => "f.__closure__[0]"
        };
        Run(host, "escaped=" + expression + "\nwatch_f=weakref.ref(f)\nwatch_d=weakref.ref(d['sentinel'])\ndel f,d");
        f.Dispose(); d.Dispose();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void EscapedOwnerRoots(CPythonHost host, int kind)
    {
        PrepareEscape(host, kind); Collect(host);
        var stats = host.StockThinStatistics;
        Require(stats.NativeLive == 1 && stats.CoreRoots == 1 && stats.OwnedHandles == 0,
            "P/self/cell 外逃正式保活 F 和纯 core");
        Run(host, "value=[]\nassert watch_f() is not None and watch_d() is not None\nassert watch_f()(value) is value\nnothing=None");
        Require(ReferenceEquals(PyNone.Instance, Get(host, "nothing")), "None 保持原身份");
        if (kind == 0)
            Run(host, "try:\n escaped([])\nexcept NotImplementedError:\n pass\nelse:\n raise AssertionError('直接 P 必须核对真实 caller frame')");
        Throws<InvalidOperationException>(host.Dispose);
        Run(host, "del escaped,value,nothing");
        Baseline(host, 13 + kind);
        Run(host, "assert watch_f() is None and watch_d() is None\ndel watch_f,watch_d,EscapeSentinel");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PrepareResurrection(CPythonHost host)
    {
        Run(host, "import weakref\nsaved=[]\nclass Keeper:\n def __init__(self,f): self.f=f\n def __del__(self):\n  value=[]\n  assert self.f(value) is value\n  saved.append(self.f)\nd={}");
        var d = (PythonObject)Get(host, "d")!;
        var f = host.CreateStockThinFunction(d, Definition("revived"), Echo);
        Put(host, "f", f);
        Run(host, "watch=weakref.ref(f)\nf.marker=Keeper(f)\ndel f,d");
        f.Dispose(); d.Dispose();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Resurrection(CPythonHost host)
    {
        for (int first = 0; first < 3; first++) ResurrectionCycle(host, first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ResurrectionCycle(CPythonHost host, int first)
    {
        PrepareResurrection(host); Collect(host);
        Run(host, "assert len(saved)==1\nassert watch() is None\nresurrected=saved[0]\nvalue=[]");
        if (first == 0) Run(host, "assert resurrected(value) is value");
        if (first == 1) Run(host, "try:\n resurrected.__code__=resurrected.__code__\nexcept NotImplementedError:\n pass\nelse:\n raise AssertionError('复活后首次setter必须拒绝')");
        var f = (StockThinFunctionHandle)Get(host, "resurrected")!;
        Require(ReferenceEquals(f, Get(host, "resurrected")), "复活 F 回流仍为原 core 的 H");
        Throws<PyNotImplementedError>(() => f.SetAttr("__code__", f.GetAttr("__code__")));
        Run(host, "del resurrected.marker\nsaved.clear()\ndel resurrected,saved,watch,value,Keeper");
        f.Dispose();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ClosingRefusal(CPythonHost host)
    {
        Run(host, "d={}");
        var d = (PythonObject)Get(host, "d")!;
        var f = host.CreateStockThinFunction(d, Definition("close"), Echo);
        Throws<InvalidOperationException>(host.Dispose);
        Require(host.Statistics.State == "running" && (long)f.Invoke(7L)! == 7, "关闭受阻后可用并可释放");
        f.Dispose(); d.Dispose(); Run(host, "del d");
    }

    // 使用生成程序集将调用的 public 中性 ABI，不通过内部业务委托或捕获 context 的适配器。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void NeutralCompiledApi(CPythonHost host)
    {
        Run(host, "neutral_d={'__name__':'neutral_subject', 'answer':[]}");
        var dictionary = (PythonObject)Get(host, "neutral_d")!;
        var read = PyStockThinFunctions.Create(dictionary,
            new("neutral:read", "neutral_subject", "neutral_read", "neutral_subject.py", 2), NeutralRead);
        var echo = PyStockThinFunctions.Create(dictionary,
            new("neutral:echo", "neutral_subject", "neutral_echo", "neutral_subject.py", 5), NeutralEcho);
        Put(host, "neutral_read", read); Put(host, "neutral_echo", echo);
        Run(host, "assert neutral_read.__globals__ is neutral_d\nassert neutral_read.__name__=='neutral_read'\nassert neutral_read.__module__=='neutral_subject'\nassert neutral_read(0) is neutral_d['answer']\nneutral_d['answer']={}\nassert neutral_read(x=0) is neutral_d['answer']\nassert neutral_echo(neutral_d) is neutral_d\ndel neutral_read,neutral_echo,neutral_d");
        object value = new PyList([]);
        Require(ReferenceEquals(value, PyOps.CallValue(echo, [value], [], [])), "public CIL body/H 保留 CLR 输入身份");
        host.ReleaseObject(read); host.ReleaseObject(echo); dictionary.Dispose();
    }

    public static int Main(string[] args)
    {
        Require(RuntimeFeature.IsDynamicCodeSupported == (Environment.GetEnvironmentVariable("RYUPY_STOCK_THIN_MODE") != "aot"), "真实 JIT/NativeAOT 运行模式");
        var host = CPythonHost.Start(args[0], ["stock-thin-provider"]);
        Run(host, "import gc\ngc.disable()");
        if (args.Length > 1 && args[1] == "blocked")
        {
            Run(host, "import sys\ndef blocker(event,args):\n if event=='sys.addaudithook': raise RuntimeError('拒绝新增hook')\nsys.addaudithook(blocker)");
            Throws<PyNotImplementedError>(host.EnableStockThinFunctions);
            Require(host.StockThinStatistics.CoreRoots == 0, "拒绝安装无 core 根");
            host.Dispose(); Console.WriteLine("PASS blocked-audit finalize=0"); return 0;
        }
        if (args.Length > 1 && args[1] == "watcher-full")
        {
            using (host.Enter()) Require(ThinTestTools.thin_test_reserve() == 8, "测试占满八个 watcher 槽位");
            host.EnableStockThinFunctions(); ClosingRefusal(host); Baseline(host, 1);
            using (host.Enter()) Require(ThinTestTools.thin_test_free_slots() == 8, "组件不占用、不清除别人的 watcher ID");
            host.Dispose(); Console.WriteLine("PASS watcher-independent finalize=0"); return 0;
        }
        // 禁止隐式默认启用。
        Throws<PyNotImplementedError>(() => host.CreateStockThinFunction(1L, Definition("disabled"), Echo));
        host.EnableStockThinFunctions();
        if (args.Length > 1 && args[1] == "neutral")
        {
            PyProvider.Install(host);
            try { NeutralCompiledApi(host); Baseline(host, 2); host.Dispose(); }
            finally { PyProvider.Remove(host); }
            Console.WriteLine("PASS neutral-compiled-ABI finalize=0"); return 0;
        }
        if (args.Length > 1 && args[1] == "resurrect")
        {
            Resurrection(host); Baseline(host, 3); host.Dispose();
            Console.WriteLine("PASS normal-finalizer-resurrection finalize=0"); return 0;
        }
        WeakReference weak = CpOnly(host);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Require(!weak.IsAlive && CPythonHost.StockThinOwnedHandles == 0,
            "CP-only 时 H/owned holder 已收集");
        Require(host.Statistics.PendingReleases > 0, "F 的 owned 引用先进入队列");
        Collect(host);
        Run(host, "assert f([])==43");
        ReflowAndMutation(host); Baseline(host, 1);
        Run(host, "assert watch_f() is None and watch_d() is None\ndel watch_f,watch_d,Sentinel");
        ModuleTransactions(host); Baseline(host, 3);
        BuiltinEnvironment(host); Baseline(host, 5);
        DynamicValuesAndFailures(host); Baseline(host, 12);
        for (int kind = 0; kind < 3; kind++) EscapedOwnerRoots(host, kind);
        ClosingRefusal(host); Baseline(host, 16);
        Require(bodies >= 10, "真实静态 CIL body 调用");
        host.Dispose();
        Console.WriteLine($"RESULT bodies={bodies} finalize=0 state={host.Statistics.State}");
        return 0;
    }
}
