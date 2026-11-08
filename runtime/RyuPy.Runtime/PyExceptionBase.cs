using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>
/// Python 异常基类：承载 Python 侧的类型名、消息、<c>args</c>、显式原因（<c>__cause__</c>）、
/// 隐式上下文（<c>__context__</c>）与 traceback 位置列表。
/// </summary>
/// <remarks>
/// 与 CLR 的关系：继承 <see cref="Exception"/>，因此可以被 CIL 的 catch 子句捕获，
/// 也能通过 <c>PyBaseException</c> 一次性兜住所有 Python 异常。
/// <see cref="Exception.Message"/> 只放 Python 消息本身，不拼类型前缀——
/// <c>TypeError: message</c> 的渲染属于 traceback 的职责。
/// </remarks>
public class PyBaseException : Exception
{
    // 只有 V1 成功源操作绑定；构造异常本身不建立传播，也不授 NativeFresh 权限。
    internal TraceProductBinding? TraceProductBinding;
#if TRACE_ADAPTER_TEST
    // 构造期不读取scope；首次真实Source成功后才发布。
    internal TraceAdapterBinding? TraceAdapterBinding;
#endif
    /// <summary>Python 异常类名，例如 <c>TypeError</c>。</summary>
    public string PyTypeName { get; }

    /// <summary>Python 侧消息（<c>str(exc)</c> 的内容）。</summary>
    public virtual string PyMessage => positionalConstruction
        ? PyTypeName == "KeyError" && Args.Length == 1 ? PyRepr.Repr(Args[0]) : PyErrors.UserExceptionMessage(Args)
        : initialMessage;

    private readonly string initialMessage;
    private bool positionalConstruction;

    /// <summary>位置构造冷路延迟格式化；既有内部消息构造保持原CLR消息行为。</summary>
    public override string Message => positionalConstruction ? PyMessage : base.Message;

    /// <summary><c>exc.args</c>：构造时的位置参数。</summary>
    public object?[] Args { get; private set; }

    /// <summary><c>raise ... from ...</c> 的显式原因（<c>__cause__</c>）。</summary>
    public PyBaseException? Cause { get; private set; }

    /// <summary>显式 cause（含 from None）抑制隐式上下文的报告，但不删除 Context 本身。</summary>
    public bool SuppressContextFlag { get; set; }

    /// <summary>
    /// 隐式上下文（<c>__context__</c>）：在 <c>except</c> 处理期间抛出新的异常时，
    /// 新异常自动挂上前一个异常。与 <see cref="Cause"/> 的区别在于它是自动建立的。
    /// </summary>
    public PyBaseException? Context { get; set; }

    // 仅 Legacy 路径存储抛出点的栈帧快照。
    private PyStackFrame[] _legacyFrames = [];
    /// <summary>
    /// 栈帧列表，最内层在前；Legacy 返回抛出点快照，V1 由绑定 owner 的历史派生。
    /// </summary>
    public PyStackFrame[] Frames
    {
        get
        {
            if (TraceProductBinding is not { } binding) return _legacyFrames;
            var history = binding.Owner.Snapshot.History;
            var frames = new PyStackFrame[history.Count];
            for (int i = 0; i < frames.Length; i++)
            {
                var frame = history[history.Count - 1 - i];
                frames[i] = new(frame.File, frame.Function, frame.Line);
            }
            return frames;
        }
        internal set
        {
            if (TraceProductBinding is not null || PyTraceProduct.Enabled)
                throw new InvalidOperationException("trace V1 history has a single owner");
            _legacyFrames = value;
        }
    }

    /// <summary>用户异常类对应的 Python 类对象；内建异常为 <c>null</c>。</summary>
    public PyClass? PythonClass { get; private set; }

    /// <summary>用户异常实例的属性字典（`self.x = ...`）。</summary>
    public Dictionary<string, object?> UserAttributes { get; } = new(StringComparer.Ordinal);

    /// <summary>把异常对象绑定到用户类（codegen 工厂在构造时调用）。</summary>
    public void BindPythonClass(PyClass cls) => PythonClass = cls;

    /// <summary>用 Python 类型名与消息构造。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数；为 null 时按消息推断。</param>
    /// <param name="cause">显式原因。</param>
    public PyBaseException(
        string typeName,
        string message,
        object?[]? args = null,
        PyBaseException? cause = null
    )
        : base(message, cause)
    {
        PyTypeName = typeName;
        initialMessage = message;
        Args = args ?? (message.Length == 0 ? [] : [message]);
        Cause = cause;
        SuppressContextFlag = cause is not null;
    }

    // 仅真实PyType调用工厂使用；保CLR子类身份，原始参数浅拷贝且不提前运行用户str/repr。
    internal void InitializePositionalArguments(object?[] args)
    {
        Args = (object?[])args.Clone();
        positionalConstruction = true;
    }

    /// <summary>供生成的用户异常子类使用：指定类型名、消息与位置参数。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    public PyBaseException(string typeName, string message, object?[] args)
        : this(typeName, message, args, null) { }

    /// <summary>
    /// 在原实例上挂接显式原因并返回自身。
    /// </summary>
    /// <remarks>
    /// Python 的 `raise existing from cause` 不创建副本：异常身份、用户属性及运行时子类都必须保留。
    /// 因此既不能 MemberwiseClone，也不能按类型名重新构造；后者还会破坏 CLR 类型化 catch。
    /// `cause == null` 表示显式 from None，仍必须记录抑制标志，不能与从未指定 cause 混淆。
    /// Context 保留供程序读取，仅在报告时依据标志决定是否显示。
    /// </remarks>
    /// <param name="cause">显式原因。</param>
    public PyBaseException WithCause(PyBaseException? cause)
    {
        Cause = cause;
        SuppressContextFlag = true;
        return this;
    }

    /// <summary>按 <c>TypeName: message</c> 渲染，供 traceback 与错误退出使用。</summary>
    public string Render()
    {
        // 延迟消息可能调用用户 __str__，一次报告只求值一次。
        string message = PyRepr.Str(this);
        string name = PyNativeExceptionDisplay.Name(this);
        return message.Length == 0 ? name : $"{name}: {message}";
    }
}

/// <summary>Python <c>Exception</c>：所有常规异常的共同基类。</summary>
public class PyException : PyBaseException
{
    /// <summary>用 Python 类型名构造 <c>Exception</c> 家族的异常。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    /// <param name="cause">显式原因。</param>
    public PyException(
        string typeName,
        string message,
        object?[]? args = null,
        PyBaseException? cause = null
    )
        : base(typeName, message, args, cause) { }

    /// <summary>供生成的用户异常子类使用（类型名 / 消息 / 位置参数）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    public PyException(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }
}
