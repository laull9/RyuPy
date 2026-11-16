// typing.NamedTuple内部私壳状态；本阶段没有公开根、模块导出或注解终结实现。
// 私壳永不作为普通类分配；真实namespace转换在后续调用Prepare前完成。
using System;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

internal static class PyTypingNamedTupleShell
{
    private enum State { Pending, Preparing, Prepared, Failed }
    private sealed class Builder
    {
        internal State Status;
        internal PyNamedTupleLayout.PreparedClass? Prepared;
    }
    private static readonly ConditionalWeakTable<PyClass, Builder> Builders = new();

    internal static PyClass Begin(string name, object? module)
    {
        // 标记在返回/写任何可观察槽之前设置；没有普通基链、IO/string/异常早返。
        var shell = new PyClass(name) { IsNativeRuntimeClass = true };
        Builders.Add(shell, new Builder());
        shell.SetAttr("__module__", module ?? PyNone.Instance);
        shell.SetAttr("__qualname__", name);
        shell.SetAttr("__annotations__", new PyDict());
        return shell;
    }

    internal static void RejectPending(PyClass value)
    {
        if (Builders.TryGetValue(value, out _))
            throw PyNamedTupleLayout.Unsupported("typing.NamedTuple class namespace is not finalized");
    }

    internal static PyNamedTupleLayout.PreparedClass Prepare(PyClass shell, string[] fields,
        object?[] defaults, object? module)
    {
        Builder builder = Require(shell);
        if (builder.Status != State.Pending)
            throw PyNamedTupleLayout.Unsupported("typing.NamedTuple namespace cannot be prepared again");
        builder.Status = State.Preparing;
        try
        {
            var prepared = PyNamedTupleLayout.PrepareClass(shell, fields, defaults, module);
            builder.Prepared = prepared;
            builder.Status = State.Prepared;
            return prepared;
        }
        catch
        {
            builder.Status = State.Failed;
            throw;
        }
    }

    // B2在注解/default/factory处理失败时调用，壳仍保special标记而不回到ordinary。
    internal static void Fail(PyClass shell)
    {
        Builder builder = Require(shell);
        builder.Status = State.Failed;
        builder.Prepared = null;
    }

    internal static void ValidateCommit(PyClass shell, PyNamedTupleLayout.PreparedClass prepared)
    {
        if (!Builders.TryGetValue(shell, out Builder? builder))
        {
            // collections内部工厂只有全新空类可提交；不允许普通已定义类借此改布局。
            if (shell.Attributes.Count != 0 || shell.BaseClass is not null || shell.BuiltinBase is not null
                || shell.ExceptionFactory is not null || shell.IsNativeRuntimeClass)
                throw PyNamedTupleLayout.Unsupported("namedtuple commit requires its private namespace or a fresh class");
            return;
        }
        if (builder.Status != State.Prepared || !ReferenceEquals(builder.Prepared, prepared))
            throw PyNamedTupleLayout.Unsupported("typing.NamedTuple commit requires its completed preparation");
    }

    internal static void CompleteCommit(PyClass shell)
    {
        if (!Builders.Remove(shell)) return;
        shell.IsNativeRuntimeClass = false;
    }

    private static Builder Require(PyClass shell) => Builders.TryGetValue(shell, out Builder? builder)
        ? builder : throw PyNamedTupleLayout.Unsupported("typing.NamedTuple operation requires a private namespace");
}
