using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>普通动态基类的能力守卫；不推断跨模块ClassId，不实现任意元类协议。</summary>
public static class PyRuntimeBases
{
    /// <summary>验证真实普通类及完整祖先链，原样返回同一类对象。</summary>
    /// <param name="value">在类定义处仅求值一次的基类值。</param>
    /// <returns>通过验证的原类对象。</returns>
    public static object Validate(object? value)
    {
        // 只许可真实驻留object，不放开任意内建布局或按名称假冒的类。
        if (ReferenceEquals(value, PyBaseObject.Type)) return PyBaseObject.Type;
        if (value is not PyClass cls)
            throw Unsupported();
        // 仅定义时遍历；不向实例构造、属性访问等热路径增加守卫。
        // 引用身份集合不调用用户hash/eq，也保证损坏的内部继承环不会挂死。
        var seen = new HashSet<PyClass>(ReferenceEqualityComparer.Instance);
        for (PyClass? current = cls; current is not null; current = current.BaseClass)
        {
            if (!seen.Add(current) || current.BuiltinBase is not null
                || current.ExceptionFactory is not null || current.HasEnumMetadata
                || current.DataclassActive || current.IsNativeRuntimeClass)
                throw Unsupported();
        }
        return cls;
    }

    private static PyNotImplementedError Unsupported() => new(
        "runtime bases currently require an ordinary user class; special classes and metaclass protocols are not supported");
}

public sealed partial class PyClass
{
    // 原生普通类即使没有BuiltinBase也不应通过任意动态继承绕开其专用编译入口。
    internal bool IsNativeRuntimeClass { get; set; }
}
