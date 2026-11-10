// 进程级首次初始化合同；专用CI每个场景只选这一个Fact并以TRX验证实跑，不重置产品状态。
using RyuPy.Runtime;
using Xunit.Abstractions;

namespace RyuPy.Runtime.Tests;

public sealed class DedentFreshFactAttribute : FactAttribute
{
    public DedentFreshFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RYUPY_TEXTWRAP_INIT_SCENARIO")))
            Skip = "须由dedent专用门逐场景启动fresh进程；普通make check不代签首次初始化。";
    }
}

[Collection("生成器帧栈")]
public sealed class PyTextDedentInitializationTests(ITestOutputHelper output)
{
    private static object? Call(object? function, params object?[] args) => PyOps.CallValue(function, args, [], []);
    private static PyFunction Raw(PyCall body) => new("proxy", PySignature.Create("proxy", [], [], []), body, [], false);
    private static readonly string[] Backends =
        ["_compile", "_compiler", "_parser", "_cache", "_cache2", "_MAXCACHE", "_MAXCACHE2", "_compile_template"];

    [DedentFreshFact]
    public void FreshProcessContract()
    {
        string scenario = Environment.GetEnvironmentVariable("RYUPY_TEXTWRAP_INIT_SCENARIO")!;
        PySystem.Initialize("dedent-initialization.py", []);
        object? modules = PyModules.Import("sys").GetAttr("modules");
        Assert.Equal(false, Call(PyObject.GetAttr(modules, "__contains__"), "textwrap"));
        Assert.Equal(false, Call(PyObject.GetAttr(modules, "__contains__"), "re"));
        if (scenario is "source_success" or "source_retry") Source(scenario == "source_retry");
        else Native(scenario);
        output.WriteLine("FRESH_OK " + scenario + " pid=" + Environment.ProcessId);
    }

    private static void Native(string scenario)
    {
        PyModule re = PyModules.Import("re"); var compile = Assert.IsType<PyFunction>(re.GetAttr("compile"));
        int callbacks = 0;
        switch (scenario)
        {
            case "stock_metadata":
                // 普通metadata不参与真实绑定；不能用假__defaults__属性代替宿主数组。
                PyObject.SetAttr(compile, "__name__", "renamed_compile");
                PyObject.SetAttr(compile, "__module__", "other");
                PyObject.SetAttr(compile, "__defaults__", new PyTuple([99L]));
                break;
            case "compile":
                re.SetAttr("compile", Raw((_, _, _, _) => { callbacks++; throw new PyValueError("unexpected compile proxy"); }));
                break;
            case "flags_value": re.SetAttr("MULTILINE", 4L); break;
            case "flags_object":
                var cls = new PyClass("Flags");
                cls.SetAttr("__index__", Raw((_, _, _, _) => { callbacks++; return 8L; }));
                re.SetAttr("MULTILINE", new PyInstance(cls)); break;
            case "signature_names": compile.Signature.Names[0] = "changed"; break;
            case "signature_kinds": compile.Signature.Kinds[0] = PyParamKind.KeywordOnly; break;
            case "signature_defaults": compile.Signature.HasDefault[0] = true; break;
            case "defaults": compile.Defaults[1] = 8L; break;
            default:
                Assert.StartsWith("backend", scenario);
                string backend = scenario["backend".Length..];
                Assert.Contains(backend, Backends);
                re.SetAttr(backend, PyNone.Instance); break;
        }
        PyModule module = PyModules.Import("textwrap");
        Assert.Same(re, module.GetAttr("re"));
        ProtectOld(module);
        if (scenario == "stock_metadata")
        {
            Assert.IsType<PyRegexPattern>(module.GetAttr("_whitespace_only_re"));
            Assert.IsType<PyRegexPattern>(module.GetAttr("_leading_whitespace_re"));
            Assert.Equal("x", Call(module.GetAttr("dedent"), "  x"));
        }
        else
        {
            Assert.Throws<PyNotImplementedError>(() => Call(module.GetAttr("dedent"), "  x"));
            Assert.Throws<PyAttributeError>(() => module.GetAttr("_whitespace_only_re"));
            Assert.Throws<PyAttributeError>(() => module.GetAttr("_leading_whitespace_re"));
            // 正常参数绑定优先，域外函数也不是无条件NI的伪callable。
            Assert.Throws<PyTypeError>(() => Call(module.GetAttr("dedent")));
        }
        Assert.Equal(0, callbacks);
    }

    private static void Source(bool retry)
    {
        int attempts = 0, reads = 0;
        var escaped = new List<PyModule>(); var marker = new PyRuntimeError("source failure");
        object written = new PyList([]);
        // 真实公开源码注册覆盖native，getter除来源标记外一律失败；不读取compile/flags协议。
        PyModules.Register("re", "", "source-re.py", () =>
        {
            attempts++; PyModule current = PyModules.Import("textwrap"); escaped.Add(current);
            if (retry && attempts == 1)
            {
                current.SetAttr("re", written);
                throw marker;
            }
        }, key =>
        {
            if (key == "source_re_marker") return "host-source";
            reads++; throw new PyAttributeError(key);
        }, (_, _, _) => throw new PyValueError("source setter must not run"));
        if (retry)
        {
            Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => PyModules.Import("textwrap")));
            Assert.Same(written, escaped[0].GetAttr("re"));
            Assert.Throws<PyAttributeError>(() => escaped[0].GetAttr("dedent"));
            Assert.Throws<PyAttributeError>(() => escaped[0].GetAttr("_whitespace_only_re"));
            Assert.Throws<PyAttributeError>(() => escaped[0].GetAttr("_leading_whitespace_re"));
        }
        PyModule module = PyModules.Import("textwrap"), re = PyModules.Import("re");
        Assert.Equal("host-source", re.GetAttr("source_re_marker"));
        Assert.Same(re, module.GetAttr("re"));
        Assert.Same(module, escaped[^1]);
        Assert.Equal(retry ? 2 : 1, attempts);
        Assert.Throws<PyNotImplementedError>(() => Call(module.GetAttr("dedent"), "  x"));
        ProtectOld(module);
        Assert.Equal(0, reads);
        if (retry)
        {
            Assert.NotSame(escaped[0], module);
            Assert.Same(written, escaped[0].GetAttr("re"));
            Assert.Throws<PyAttributeError>(() => escaped[0].GetAttr("dedent"));
        }
    }

    private static void ProtectOld(PyModule module)
    {
        Assert.Equal(new object?[] { "one", "two" }, Assert.IsType<PyList>(Call(module.GetAttr("wrap"), "one two", 4L)).Items);
        Assert.Equal("one\ntwo", Call(module.GetAttr("fill"), "one two", 4L));
        Assert.Same(PyTextWrap.CreateAttributes()["TextWrapper"], module.GetAttr("TextWrapper"));
    }
}
