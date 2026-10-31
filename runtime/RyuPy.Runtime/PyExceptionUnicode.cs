using System;

namespace RyuPy.Runtime;

/// <summary><c>UnicodeError</c>（<c>ValueError</c> 子类）。</summary>
public class PyUnicodeError : PyValueError
{
    /// <summary>构造 <c>UnicodeError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyUnicodeError(string message)
        : base("UnicodeError", message) { }

    /// <summary>供子类指定自己的 Python 类型名。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    protected PyUnicodeError(string typeName, string message)
        : base(typeName, message) { }

    /// <summary>供用户异常子类使用。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyUnicodeError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }
}

/// <summary><c>UnicodeDecodeError</c>。</summary>
public class PyUnicodeDecodeError : PyUnicodeError
{
    /// <summary>构造 <c>UnicodeDecodeError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyUnicodeDecodeError(string message)
        : base("UnicodeDecodeError", message) { }
    /// <summary>供用户异常子类使用（类型名 / 消息 / 位置参数）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyUnicodeDecodeError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }

}

/// <summary><c>UnicodeEncodeError</c>。</summary>
public class PyUnicodeEncodeError : PyUnicodeError
{
    /// <summary>构造 <c>UnicodeEncodeError</c>。</summary>
    /// <param name="message">消息。</param>
    public PyUnicodeEncodeError(string message)
        : base("UnicodeEncodeError", message) { }
    /// <summary>供用户异常子类使用（类型名 / 消息 / 位置参数）。</summary>
    /// <param name="typeName">Python 类型名。</param>
    /// <param name="message">消息。</param>
    /// <param name="args">位置参数。</param>
    protected PyUnicodeEncodeError(string typeName, string message, object?[] args)
        : base(typeName, message, args) { }

}

