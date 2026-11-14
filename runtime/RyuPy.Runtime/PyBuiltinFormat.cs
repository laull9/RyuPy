// 公共format保留str子类身份；精确载体快路与用户类型级特殊绑定分开，不改旧f-string。
using System;
using System.Numerics;

namespace RyuPy.Runtime;

internal static partial class PyBuiltinFormat
{
    internal static object Invoke(object?[] args, string[] names)
    {
        if (names.Length != 0) throw Own(new PyTypeError("format() takes no keyword arguments"));
        if (args.Length == 0) throw Own(new PyTypeError("format expected at least 1 argument, got 0"));
        if (args.Length > 2) throw Own(new PyTypeError($"format expected at most 2 arguments, got {args.Length}"));
        object? spec = args.Length == 2 ? args[1] : string.Empty;
        if (!PyStringLayout.TryGet(spec, out string text))
            throw Own(new PyTypeError($"format() argument 2 must be str, not {(spec is null or PyNone ? "None" : PyRepr.TypeName(spec))}"));
        return Format(args[0], spec!, text);
    }

    internal static object Format(object? value, object spec, string text)
    {
        // 精确内建不查询Python槽，不为每次调用创建参数/绑定方法包装。
        switch (value)
        {
            case string exact: return text.Length == 0 ? exact : Text(value, exact, Parse(text, "str"));
            case bool flag:
                return text.Length == 0 ? (flag ? "True" : "False")
                    : Integer(flag ? BigInteger.One : BigInteger.Zero, Parse(text, "bool"), "bool");
            case long or int or BigInteger:
                return Integer(PyInt.ToBigInteger(value), Parse(text, "int"), "int");
            case PyDecimal number: return number.Format(text);
            case double number: return Float(number, text);
            case float number: return Float(number, text);
        }
        // 当前str布局禁止__format__覆盖；继承默认字符串格式不是object格式。
        if (value is PyStringInstance str)
            return text.Length == 0 ? PyRepr.StrObject(value) : Text(value, str.Value, Parse(text, "str"));
        if (value is PyInstance instance)
        {
            if (TrySlot(instance.Class, instance, spec, out object? result)) return RequireResult(result);
            // Enum和其他原生类布局可能有专用默认格式槽，不能误按ordinary object处理。
            for (PyClass? current = instance.Class; current is not null; current = current.BaseClass)
                if (current.HasEnumMetadata || current.IsNativeRuntimeClass || current.BuiltinBase is not null)
                    throw Unsupported(value);
            return Default(value, text);
        }
        if (value is PyBaseException exception)
        {
            if (exception.PythonClass is { } cls && TrySlot(cls, exception, spec, out object? result))
                return RequireResult(result);
            return Default(value, text);
        }
        // 已知object默认槽的载体白名单；未知native不使用普通GetAttr猜特殊方法。
        if (value is null or PyNone or PyBaseObject or PyList or PyTuple or PyDict
            or PySet or PyFrozenSet or PyBytes or PyByteArray or PyRange or PySlice
            or PyType or PyClass or PyFunction or PyBuiltinFunction or PyModule)
            return Default(value, text);
        throw Unsupported(value);
    }

    private static object Float(double value, string text)
    {
        if (text.Length == 0) return PyFormat.FloatRepr(value);
        Spec spec = Parse(text, "float");
        Validate(spec, "float");
        if (spec.Precision > int.MaxValue)
            throw Own(new PyValueError("precision too big"));
        return GeneralFloat(value, spec);
    }

    private static object Default(object? value, string text)
    {
        if (text.Length != 0)
            throw Own(new PyTypeError($"unsupported format string passed to {PyRepr.TypeName(value)}.__format__"));
        return PyRepr.StrObject(value);
    }

    private static object RequireResult(object? value)
    {
        if (PyStringLayout.TryGet(value, out _)) return value!;
        throw Own(new PyTypeError($"__format__ must return a str, not {PyRepr.TypeName(value)}"));
    }

    private static bool TrySlot(PyClass cls, object self, object spec, out object? result)
    {
        if (!cls.TryLookup("__format__", out PyClass owner, out object? raw))
        { result = null; return false; }
        // 用户正文在Own之外；只有本层生成的校验/不支持错误由本层准备。
        if (raw is PyFunction or PyStaticMethod or PyClassMethod)
        {
            CheckKnownCallable(raw);
            result = PyObject.InvokeBound(raw, owner, self, [spec], [], []);
        }
        else
        {
            object? callable;
            if (raw is PyInstance descriptor && descriptor.Class.TryLookup("__get__", out _, out _))
                throw Own(new PyNotImplementedError("format: custom Python descriptors are not supported"));
            if (raw is PyProperty property)
            {
                if (property.Getter is null or PyNone)
                    throw Own(new PyAttributeError("property '__format__' has no getter"));
                CheckKnownCallable(property.Getter);
                callable = PyObject.InvokeBound(property.Getter, owner, self, [], [], []);
            }
            else callable = PyObject.Bind(raw, owner, self);
            // 非描述符callable不能自动注入self；只准备本层已知不可调用的错误。
            CheckKnownCallable(callable);
            result = PyOps.CallValue(callable, [spec], [], []);
        }
        return true;
    }

    // 只校验本层明确不可调用的目标；不使用全局callable()宽松判断，也不捕获正文错误。
    private static void CheckKnownCallable(object? target)
    {
        while (target is PyStaticMethod or PyClassMethod)
            target = target is PyStaticMethod method ? method.Value : ((PyClassMethod)target).Value;
        bool bad = target switch
        {
            PyInstance instance => !instance.Class.TryLookup("__call__", out _, out _),
            IPyNativeCallable or PyFunction or PyBuiltinFunction or PyNativeType or PyClass
                or PyBoundMethod or PyBuiltinMethod or PyType => false,
            _ => true,
        };
        if (bad) throw Own(new PyTypeError($"'{PyRepr.TypeName(target)}' object is not callable"));
    }

    private static PyBaseException Unsupported(object? value) =>
        Own(new PyNotImplementedError($"format: default protocol for {PyRepr.TypeName(value)} is not supported"));
    private static PyBaseException Own(PyBaseException error) => (PyBaseException)PyErrors.PrepareRaise(error);
}
