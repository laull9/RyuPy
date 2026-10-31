using System;
using System.Collections.Generic;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>
/// Python 类型对象的最小表示（<c>type(x)</c> 的结果）。
/// </summary>
/// <remarks>
/// 完整类型对象（<c>__dict__</c>、基类链、可调用构造）属于 M2.5 / M2.6 的东西。
/// 这里先提供 <c>__name__</c> 与 <c>__qualname__</c>，够异常用例与错误消息使用。
/// </remarks>
public sealed class PyType
{
    /// <summary>类型名，例如 <c>ValueError</c>。</summary>
    public string Name { get; }

    /// <summary>构造类型对象。</summary>
    /// <param name="name">类型名。</param>
    public PyType(string name) => Name = name;

    /// <summary>与 CPython 一致的 <c>repr</c> 形式。</summary>
    public override string ToString() => $"<class '{Name}'>";
}

/// <summary>
/// 动态对象的属性读取（<c>obj.name</c>）与工厂函数。
/// </summary>
/// <remarks>
/// 目前只覆盖异常对象与类型对象上的属性：<c>args</c>、<c>__cause__</c>、
/// <c>__context__</c>、<c>__name__</c>、<c>__class__</c>。普通 class 实例的属性查找
/// 在 M2.5 接入，届时这里会变成「先查 shape 缓存，再走通用查找」的入口。
/// </remarks>
public static class PyObject
{
    /// <summary><c>type(value)</c>。</summary>
    /// <param name="value">任意 Python 值。</param>
    public static object TypeOf(object? value) => new PyType(PyRepr.TypeName(value));

    /// <summary>读取属性；不存在的属性抛 <c>AttributeError</c>。</summary>
    /// <param name="obj">被访问的对象。</param>
    /// <param name="name">属性名。</param>
    public static object GetAttr(object? obj, string name)
    {
        switch (obj)
        {
            case PyType type:
                return GetTypeAttr(type, name);
            case PyBaseException exception:
                return GetExceptionAttr(exception, name);
            default:
                throw new PyAttributeError(
                    $"'{PyRepr.TypeName(obj)}' object has no attribute '{name}'"
                );
        }
    }

    private static object GetTypeAttr(PyType type, string name) =>
        name switch
        {
            "__name__" or "__qualname__" => type.Name,
            _ => throw new PyAttributeError($"type object '{type.Name}' has no attribute '{name}'"),
        };

    private static object GetExceptionAttr(PyBaseException exception, string name) =>
        name switch
        {
            "args" => new PyTuple(exception.Args),
            "__cause__" => (object?)exception.Cause ?? PyNone.Instance,
            "__context__" => (object?)exception.Context ?? PyNone.Instance,
            "__class__" => new PyType(exception.PyTypeName),
            "__traceback__" => PyNone.Instance,
            _ => throw new PyAttributeError(
                $"'{exception.PyTypeName}' object has no attribute '{name}'"
            ),
        };
}
