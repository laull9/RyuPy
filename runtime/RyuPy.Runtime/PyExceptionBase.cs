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
    /// <summary>Python 异常类名，例如 <c>TypeError</c>。</summary>
    public string PyTypeName { get; }

    /// <summary>Python 侧消息（<c>str(exc)</c> 的内容）。</summary>
    public string PyMessage { get; }

    /// <summary><c>exc.args</c>：构造时的位置参数。</summary>
    public object?[] Args { get; }

    /// <summary><c>raise ... from ...</c> 的显式原因（<c>__cause__</c>）。</summary>
    public PyBaseException? Cause { get; private set; }

    /// <summary>
    /// 隐式上下文（<c>__context__</c>）：在 <c>except</c> 处理期间抛出新的异常时，
    /// 新异常自动挂上前一个异常。与 <see cref="Cause"/> 的区别在于它是自动建立的。
    /// </summary>
    public PyBaseException? Context { get; set; }

    /// <summary>抛出点的栈帧快照，最内层在前；由 <see cref="PyTraceback"/> 在抛出时填写。</summary>
    public PyStackFrame[] Frames { get; internal set; } = [];

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
        PyMessage = message;
        Args = args ?? (message.Length == 0 ? [] : [message]);
        Cause = cause;
    }

    /// <summary>供生成的用户异常子类使用：指定类型名、消息与位置参数。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    public PyBaseException(string typeName, string message, object?[] args)
        : this(typeName, message, args, null) { }

    /// <summary>
    /// 返回挂上显式原因的副本。
    /// </summary>
    /// <remarks>
    /// 必须用 <see cref="object.MemberwiseClone"/> 而不是「按类型名重新构造」：
    /// CLR 的 <c>catch PyRuntimeError</c> 依赖**运行时类型**，如果这里造出一个基类实例，
    /// Python 侧类型名看起来正确，但 `except RuntimeError` 永远捕获不到——
    /// 这种错误在 traceback 里完全看不出来。
    /// </remarks>
    /// <param name="cause">显式原因。</param>
    public PyBaseException WithCause(PyBaseException? cause)
    {
        var copy = (PyBaseException)MemberwiseClone();
        copy.Cause = cause;
        return copy;
    }

    /// <summary>按 <c>TypeName: message</c> 渲染，供 traceback 与错误退出使用。</summary>
    public string Render() => PyMessage.Length == 0 ? PyTypeName : $"{PyTypeName}: {PyMessage}";
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
