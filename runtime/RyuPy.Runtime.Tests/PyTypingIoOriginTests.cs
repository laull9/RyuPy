// typing IO 类定义来源的内部冷查询；显式模拟入口帧，不冒充编译后 Python 端到端验证。
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyTypingIoOriginTests
{
    private static string Name() => "typing_io_origin_" + Guid.NewGuid().ToString("N");

    // 反射只用于测试，不扩大生产 API；异常解包后保留原对象与堆栈。
    private static (bool Found, object? Value) Query(Type owner, object?[] arguments)
    {
        try
        {
            bool found = (bool)owner.GetMethod("TryGetClassDefinitionModule",
                BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, arguments)!;
            return (found, arguments[^1]);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    // 用非空哨兵初始化 out 参数，确保失败结果确实清空，而不是沿用调用方旧值。
    private static (bool Found, object? Value) Caller() => Query(typeof(PyTraceback), [new object()]);
    private static (bool Found, object? Value) Registered(string key) => Query(typeof(PyModules), [key, new object()]);

    private static void Found(object? expected, (bool Found, object? Value) result)
    {
        Assert.True(result.Found);
        Assert.Same(expected, result.Value);
    }

    private static void Unknown((bool Found, object? Value) result)
    {
        Assert.False(result.Found);
        Assert.Null(result.Value);
    }

    [Fact]
    public void EmptyStackAndMissingRegistrationReturnUnknownWithNullValue()
    {
        Assert.Equal(0, PyTraceback.CaptureDepth());
        string missing = Name();
        try
        {
            Unknown(Caller());
            Unknown(Registered(missing));
            PyTraceback.PushFrameWithOrigin("missing.py", "<module>",
                PyTraceback.CreateModuleExecutionSource(missing));
            Unknown(Caller());
            PyTraceback.Reset();
            Unknown(Caller());
            PyTraceback.PushFrame("static.py", "static");
            Unknown(Caller());
        }
        finally { PyTraceback.Reset(); }
    }

    [Fact]
    public void RegisteredQueryReadsLiveGlobalWithoutImportOrCachedNameFallback()
    {
        long saved = PyTraceback.CaptureDepth();
        string key = Name();
        object? current = key;
        int initialized = 0;
        int reads = 0;
        PyModules.Register(key, "", "live.py", () => initialized++, name =>
        {
            Assert.Equal("__name__", name);
            reads++;
            return current;
        }, (_, name, value) =>
        {
            Assert.Equal("__name__", name);
            current = value;
        });
        try
        {
            Found(key, Registered(key));
            PyTraceback.PushFrameWithOrigin("live.py", "<module>",
                PyTraceback.CreateModuleExecutionSource(key));
            Found(key, Caller());
            current = "changed.before.class";
            Found(current, Registered(key));
            Found(current, Caller());
            Assert.Equal(4, reads);
            Assert.Equal(0, initialized);

            // 普通模块属性仍强返注册名；类定义查询必须绕开它，读当前生成槽。
            PyModule module = PyModules.Import(key);
            Assert.Equal(1, initialized);
            module.SetAttr("__name__", "changed.after.import");
            Assert.Same(key, module.GetAttr("__name__"));
            Found(current, Registered(key));
            Found(current, Caller());
            Assert.Equal(1, initialized);
        }
        finally { PyTraceback.PopTo(saved + 1); }
    }

    [Fact]
    public void CurrentNameKeepsNonStringNoneFalseAndNullIdentityWithoutProtocols()
    {
        long saved = PyTraceback.CaptureDepth();
        string key = Name();
        object? current = null;
        var poison = new Poison();
        PyModules.Register(key, "", "raw.py", () => throw new Exception("不得触发导入"),
            _ => current, (_, _, _) => { });
        try
        {
            PyTraceback.PushFrameWithOrigin("raw.py", "<module>",
                PyTraceback.CreateModuleExecutionSource(key));
            foreach (object? value in new object?[] { poison, new PyList(), PyNone.Instance, false, "", 0L, null })
            {
                current = value;
                Found(value, Registered(key));
                Found(value, Caller());
            }
            Assert.Equal(0, poison.Reads);
        }
        finally { PyTraceback.PopTo(saved + 1); }
    }

    [Fact]
    public void UnknownFunctionAndNonStringTopNeverBorrowRegisteredOuterModule()
    {
        long saved = PyTraceback.CaptureDepth();
        string key = Name();
        int reads = 0;
        object moduleValue = new PyList();
        var poison = new Poison();
        PyModules.Register(key, "", "outer.py", () => throw new Exception("不得触发导入"),
            _ => { reads++; return moduleValue; }, (_, _, _) => { });
        var function = PyNativeFunctions.CreateVariadic("caller", _ => PyNone.Instance);
        PyOps.FunctionModule(function, key);
        try
        {
            PyTraceback.PushFrameWithOrigin("outer.py", "<module>",
                PyTraceback.CreateModuleExecutionSource(key));
            Found(moduleValue, Caller());
            long unknown = PyTraceback.PushFrame("static.py", "static");
            Unknown(Caller());
            PyTraceback.PopTo(unknown);
            foreach (object? origin in new object?[]
            {
                new object(), function, PyTraceback.CreateModuleExecutionSource(poison),
                PyTraceback.CreateModuleExecutionSource(PyNone.Instance),
                PyTraceback.CreateModuleExecutionSource(null),
                PyTraceback.CreateModuleExecutionSource(false),
                PyTraceback.CreateModuleExecutionSource(37L),
                PyTraceback.CreateModuleExecutionSource(Name()),
            })
            {
                long inner = PyTraceback.PushFrameWithOrigin("inner.py", "inner", origin);
                try { Unknown(Caller()); }
                finally { PyTraceback.PopTo(inner); }
            }
            Assert.Equal(1, reads);
            Assert.Equal(0, poison.Reads);
            Found(moduleValue, Caller());
            Assert.Equal(2, reads);
        }
        finally { PyTraceback.PopTo(saved + 1); }
    }

    [Fact]
    public void InnermostRegisteredModuleWinsAndPopRestoresOuterModule()
    {
        long saved = PyTraceback.CaptureDepth();
        string outer = Name(), inner = Name();
        object outerValue = new PyList(), innerValue = new PyDict();
        PyModules.Register(outer, "", "same.py", () => { }, _ => outerValue, (_, _, _) => { });
        PyModules.Register(inner, "", "same.py", () => { }, _ => innerValue, (_, _, _) => { });
        try
        {
            PyTraceback.PushFrameWithOrigin("same.py", "<module>",
                PyTraceback.CreateModuleExecutionSource(outer));
            long depth = PyTraceback.PushFrameWithOrigin("same.py", "<module>",
                PyTraceback.CreateModuleExecutionSource(inner));
            Found(innerValue, Caller());
            PyTraceback.PopTo(depth);
            Found(outerValue, Caller());
        }
        finally { PyTraceback.PopTo(saved + 1); }
    }

    [Fact]
    public void GetterFailuresPropagateOriginalExceptionInsteadOfReturningUnknown()
    {
        long saved = PyTraceback.CaptureDepth();
        string key = Name();
        Exception failure = new PyAttributeError("未绑定的 __name__");
        PyModules.Register(key, "", "failure.py", () => throw new Exception("不得触发导入"),
            _ => throw failure, (_, _, _) => { });
        try
        {
            PyTraceback.PushFrameWithOrigin("failure.py", "<module>",
                PyTraceback.CreateModuleExecutionSource(key));
            foreach (Exception error in new Exception[]
            {
                failure, new PyValueError("真实 getter 失败"), new InvalidOperationException("原始 CLR 失败"),
            })
            {
                failure = error;
                Assert.Same(error, Assert.Throws(error.GetType(), () => { Registered(key); }));
                Assert.Same(error, Assert.Throws(error.GetType(), () => { Caller(); }));
            }
        }
        finally { PyTraceback.PopTo(saved + 1); }
    }

    private sealed class Poison : PyNativeObject
    {
        internal int Reads;
        public override string TypeName => "ClassModulePoison";
        private PyValueError Touched()
        {
            Reads++;
            return new PyValueError("类来源查询不得调用用户协议");
        }
        public override object? GetAttr(string name) => throw Touched();
        public override string PythonRepr() => throw Touched();
        public override string PythonStr() => throw Touched();
        public override int GetHashCode() => throw Touched();
    }
}
