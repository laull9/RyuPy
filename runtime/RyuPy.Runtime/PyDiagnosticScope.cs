// Runtime诊断的一次性权限请求：Report/SystemExitCode内部铸造，绑定当前provider，不能公开构造。
// provider只能消费属于自身的请求一次；scope只覆盖诊断产生的临时资源，不转移既有外部所有权。
using System;
using System.Threading;

namespace RyuPy.Runtime;

/// <summary>诊断临时资源的可选provider能力；普通嵌入provider可保持原生命周期。</summary>
public interface IPyDiagnosticScopeProvider
{
    /// <summary>消费Runtime的一次性请求；不能取得权限时返回null。</summary>
    IDisposable? EnterDiagnosticScope(PyDiagnosticScopeRequest request);
}

/// <summary>由Runtime诊断入口铸造、绑定目标provider的不透明单次请求。</summary>
public sealed class PyDiagnosticScopeRequest
{
    private object? target;
    private int consumed;
    internal PyDiagnosticScopeRequest(object provider) => target = provider ?? throw new ArgumentNullException(nameof(provider));
    /// <summary>仅目标provider可成功消费一次；不匹配的调用不会取得权限。</summary>
    public bool TryConsume(object provider)
    {
        if (!ReferenceEquals(Volatile.Read(ref target), provider)
            || Interlocked.CompareExchange(ref consumed, 1, 0) != 0) return false;
        Interlocked.Exchange(ref target, null);
        return true;
    }
    internal static IDisposable? Begin()
    {
        var current = PyProvider.Current;
        return current is IPyDiagnosticScopeProvider scope ? scope.EnterDiagnosticScope(new(current)) : null;
    }
}
