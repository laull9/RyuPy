// 中性冷 ABI 机制验证：真实字典/来源/静态委托身份直达可选能力，不引入模块 owner 捕获。
// provider 替身仅用于 Runtime 分层；真实 CP owned/GC/JIT/AOT 由独立 Host 驱动验收。
using System;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyStockThinFunctionTests
{
    private static object? Read(IPyStockThinEnvironment environment, object? argument) =>
        environment.LoadGlobal("answer");

    [Fact]
    public void FactoryPassesTheActualGlobalsDefinitionAndStaticTargetWithoutWrapping()
    {
        var provider = new Provider();
        PyProvider.Install(provider);
        try
        {
            var dictionary = new PyDict();
            var definition = new PyStockThinDefinition("entry:read", "subject", "read", "subject.py", 7);
            PyStockThinBody target = Read;
            Assert.Same(provider.Result, PyStockThinFunctions.Create(dictionary, definition, target));
            Assert.Same(dictionary, provider.Globals);
            Assert.Same(definition, provider.Definition);
            Assert.Same(target, provider.Body);
            Assert.Null(provider.Body!.Target);
            Assert.Equal(1, provider.Calls);
        }
        finally { PyProvider.Remove(provider); }
    }

    [Fact]
    public void InstanceDefinitionBorrowsTheExactDictionaryWithoutCapturingItsContext()
    {
        var provider = new Provider();
        PyProvider.Install(provider);
        try
        {
            string name = "stock_cold_" + Guid.NewGuid().ToString("N");
            PyModules.RegisterInstanceSourceLocated(name, "", "stock_cold.py", 1, "/virtual/stock_cold.py", false,
                static context => PyModuleInstances.StoreName(context, "function",
                    PyModuleInstances.CreateStockThinFunction(context,
                        new("cold:f", "subject", "f", "subject.py", 1), Read)));
            var module = PyModuleInstances.Create(name);
            PyModuleInstances.Execute(module);
            Assert.Same(module.GetAttr("__dict__"), provider.Globals);
            Assert.Same(provider.Result, module.GetAttr("function"));
            Assert.Null(provider.Body!.Target);
        }
        finally { PyProvider.Remove(provider); }
    }

    [Fact]
    public void MissingCapabilityAndProviderFailureNeverCreateAnOldFunctionReplacement()
    {
        Assert.Null(PyProvider.Current);
        var definition = new PyStockThinDefinition("entry:f", "subject", "f", "subject.py", 1);
        Assert.Throws<PyNotImplementedError>(() => PyStockThinFunctions.Create(new PyDict(), definition, Read));
        var provider = new Provider { Failure = new PyValueError("同一个创建异常") };
        PyProvider.Install(provider);
        try
        {
            Assert.Same(provider.Failure, Assert.Throws<PyValueError>(() =>
                PyStockThinFunctions.Create(new PyDict(), definition, Read)));
            Assert.Equal(1, provider.Calls);
        }
        finally { PyProvider.Remove(provider); }
    }

    [Fact]
    public void HiddenEnvironmentReadsTheLiveValueAndKeepsItsErrorIdentity()
    {
        var environment = new Environment();
        Assert.Same(environment.Value, PyStockThinFunctions.LoadGlobal(environment, "answer"));
        environment.Value = new object();
        Assert.Same(environment.Value, PyStockThinFunctions.LoadGlobal(environment, "answer"));
        environment.Failure = new PyNameError("缺失名字");
        Assert.Same(environment.Failure, Assert.Throws<PyNameError>(() =>
            PyStockThinFunctions.LoadGlobal(environment, "missing")));
        Assert.Throws<PyRuntimeError>(() => PyStockThinFunctions.LoadGlobal(new object(), "answer"));
        // 编译体只获得读名能力，未获得 Dispose/owned 指针或模块实例的公开权利。
        Assert.Empty(typeof(IPyStockThinEnvironment).GetInterfaces());
    }

    private sealed class Environment : IPyStockThinEnvironment
    {
        internal object Value = new();
        internal PyNameError? Failure;
        public object? LoadGlobal(string name)
        {
            if (Failure is not null) throw Failure;
            return Value;
        }
    }

    private sealed class Provider : IPyModuleProvider, IPyStockThinFunctionProvider
    {
        internal readonly object Result = new();
        internal object? Globals;
        internal PyStockThinDefinition? Definition;
        internal PyStockThinBody? Body;
        internal PyValueError? Failure;
        internal int Calls;
        public object CreateStockThinFunction(object globals, PyStockThinDefinition definition, PyStockThinBody body)
        {
            Calls++; Globals = globals; Definition = definition; Body = body;
            if (Failure is not null) throw Failure;
            return Result;
        }
        public bool Owns(string name) => false;
        public object? SystemAttribute(string name) => throw new NotSupportedException();
        public PyModule Import(string name) => throw new NotSupportedException();
        public void Registered(string name) { }
        public void Initializing(PyModule module) { }
        public void Failed(string name) { }
    }
}
