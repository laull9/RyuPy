// Python公开转换保留实际str子类返回身份；内部文本消费只展开本次结果，不二次调用用户槽。
namespace RyuPy.Runtime;

public static partial class PyRepr
{
    /// <summary>Python str转换；用户槽返回的真实字符串子类对象不剥离。</summary>
    public static object StrObject(object? value) => value switch
    {
        PyInstance instance => StrInstanceObject(instance),
        PyBaseException exception => StrExceptionObject(exception),
        _ => Str(value),
    };

    /// <summary>Python repr转换；普通用户槽返回的真实字符串子类对象不剥离。</summary>
    public static object ReprObject(object? value) => value is PyInstance instance
        ? ReprInstanceObject(instance) : Repr(value);

    private static object StrExceptionObject(PyBaseException exception)
    {
        if (PyObject.TryMethod(exception, "__str__", [], out object? result))
            return RequireStringObject(result, "__str__");
        return exception.PyMessage;
    }

    private static object StrInstanceObject(PyInstance instance)
    {
        if (PyEnum.IsMember(instance)) return PyEnum.MemberRepr(instance, false);
        if (PyObject.TryMethod(instance, "__str__", [], out object? result))
            return RequireStringObject(result, "__str__");
        // str布局默认槽与object的repr兜底不同；仅覆盖repr不能改变默认文本内容。
        if (instance is PyStringInstance text) return text.Value;
        if (PyObject.TryMethod(instance, "__repr__", [], out object? repr))
            return RequireStringObject(repr, "__repr__");
        if (instance is PyDictInstance dict) return DictRaw(dict, dict.Payload);
        return instance is PyListInstance list ? ListRaw(list, list.Payload) : instance.DefaultRepr();
    }

    private static object ReprInstanceObject(PyInstance instance)
    {
        if (PyEnum.IsMember(instance)) return PyEnum.MemberRepr(instance, true);
        if (PyObject.TryMethod(instance, "__repr__", [], out object? result))
            return RequireStringObject(result, "__repr__");
        if (instance is PyListInstance list) return ListRaw(list, list.Payload);
        if (instance is PyDictInstance dict) return DictRaw(dict, dict.Payload);
        return instance is PyStringInstance text ? Quote(text.Value) : instance.DefaultRepr();
    }

    private static object RequireStringObject(object? value, string method)
    {
        if (PyStringLayout.TryGet(value, out _)) return value!;
        throw PyStringLayout.Own(new PyTypeError($"{method} returned non-string (type {TypeName(value)})"));
    }
}
