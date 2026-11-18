// 编译模块原生调用现场的可选能力：Runtime铸造一次性请求，provider只消费可信真实globals与来源。
// 普通Runtime无provider仍直接执行CIL；请求不改变函数体执行器，也不向业务dict注入胶水名字。
using System;
using System.Threading;

namespace RyuPy.Runtime;

/// <summary>为已具权威globals的编译模块提供原生C API调用现场。</summary>
public interface IPyCompiledExecutionProvider
{
    /// <summary>消费一次Runtime内部签发的请求并同步执行CIL模块体。</summary>
    void ExecuteCompiledModule(PyCompiledExecutionRequest request);
}

/// <summary>不可公开构造的模块执行能力，绑定目标provider与真实模块实例。</summary>
public sealed class PyCompiledExecutionRequest
{
    private object? provider;
    private Action? execute;
    private int consumed;
    /// <summary>本次执行的权威globals视图。</summary>
    public object Globals { get; }
    /// <summary>执行入口捕获的精确builtins字典视图；后续globals键变化不会改变此身份。</summary>
    public object Builtins { get; }
    /// <summary>已冻结的源文件位置。</summary>
    public string File { get; }
    internal PyCompiledExecutionRequest(object provider, object globals, object builtins, string file, Action execute)
    { this.provider = provider; Globals = globals; Builtins = builtins; File = file; this.execute = execute; }
    /// <summary>只有匹配provider可取出执行体一次；不匹配不会消耗请求。</summary>
    public bool TryConsume(object target, out Action? body)
    {
        body = null;
        if (!ReferenceEquals(Volatile.Read(ref provider), target)
            || Interlocked.CompareExchange(ref consumed, 1, 0) != 0) return false;
        Interlocked.Exchange(ref provider, null); body = Interlocked.Exchange(ref execute, null); return true;
    }
    internal static void Execute(object globals, object builtins, string file, Action body)
    {
        object? provider = PyProvider.Current;
        if (provider is IPyCompiledExecutionProvider foreign)
            foreign.ExecuteCompiledModule(new(provider, globals, builtins, file, body));
        else body();
    }
}
