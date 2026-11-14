// ClassVar 的有界协议：当前模块导出只在 _type_check 真正消费时读取，保存根永不跟随重绑。
// 新别名沿用 SpecialForm 共享缓存与现有 metadata holder；未知参数协议不冒充 Python 类型错误。
using System.Numerics;

namespace RyuPy.Runtime;

internal static class PyTypingClassVar
{
    // invalid_generic_forms 在转换之前创建；这里只读槽，不提前比较未知替换对象。
    internal static object? ReadExport()
    {
        if (!PyTyping.CreateAttributes().TryGetValue("ClassVar", out object? value))
            throw Error(new PyNameError("name 'ClassVar' is not defined"));
        return value;
    }

    internal static void ValidateAlias(object? value, object? current)
    {
        // 原生 GenericAlias 不属于 typing._GenericAlias，即使在本运行时共用 CLR 载体。
        if (value is not PyTypingAlias alias || alias.IsNativeAlias) return;
        if (!SafeOrigin(alias.Origin) || !SafeComparisonValue(current))
            throw Unsupported("ClassVar export comparison with an unknown alias origin or replacement is not supported");
        if (ReferenceEquals(current, alias.Origin) || PyHash.Equal(current, alias.Origin))
            throw Error(new PyTypeError(alias.PythonRepr() + " is not valid as type argument"));
    }

    private static bool SafeOrigin(object? value) => value is PyType or PyNativeType or PyClass or PyTypingForm;

    private static bool SafeComparisonValue(object? value) => SafeOrigin(value)
        || value is null or PyNone or bool or int or long or BigInteger or double or string or PyEllipsis;

    // CP 构造时收参数，不能延迟未知协议到首次 __parameters__ 读取。
    // 普通类的同名参数描述符被 CP 忽略；变量核心槽和已支持别名参数树则保持身份与顺序。
    internal static void RequireSafeParameters(object? value)
    {
        if (value is PyTypingAlias alias)
        {
            if (!SafeOrigin(alias.Origin))
                throw Unsupported("ClassVar parameter alias with unknown origin is not supported");
            foreach (object? argument in alias.Arguments) RequireSafeParameters(argument);
            return;
        }
        if (value is PyTypingGenericAlias)
            throw Unsupported("ClassVar indirect Generic alias parameter collection is not supported");
        if (value is PyType or PyNativeType or PyClass or PyTypedDictType or PyTypingTypeVar or PyTypingForm
            or null or PyNone or bool or int or long or BigInteger or double or string or PyEllipsis) return;
        throw Unsupported("ClassVar parameter collection with unknown typing protocols is not supported");
    }

    internal static object Resubscribe(PyTypingAlias alias, object? parameter)
    {
        // 闭合 alias 也先经过装饰器的原始键 hash；未知用户回调不能被已知错误吞掉。
        // 本切不实现任何成功替换，所以不引入一个无人命中的私有缓存。
        PyTypingSpecialFormCache.RequireKnown(parameter);
        try { _ = PyHash.Of(new PyTuple([alias, parameter])); }
        catch (PyTypeError) { /* _tp_cache 对 TypeError 回退正文，不重复执行参数 hash。 */ }
        PyTypingGeneric.GuardExport(PyTyping.CreateAttributes());
        PyTyping.GuardHelper("Protocol");
        if (alias.Parameters.Items.Length == 0)
            throw Error(new PyTypeError(alias.PythonRepr() + " is not a generic class"));
        throw Unsupported("ClassVar free-type-variable substitution is not supported");
    }

    internal static bool RejectRootCheck(bool subclass) => throw Error(new PyTypeError(
        "typing.ClassVar cannot be used with " + (subclass ? "issubclass()" : "isinstance()")));

    internal static object RejectBase(object value) => throw Error(new PyTypeError("Cannot subclass " + PyRepr.Repr(value)));

    // 显式 __call__ 与隐式调用都忽略传入实参数量，先产生 ClassVar 自身的禁止实例化错误。
    internal static object CallMethod(object owner) => new Call(owner);

    private sealed class Call(object owner) : PyNativeObject, IPyNativeCallable
    {
        public override string TypeName => "method";
        public override object? GetAttr(string name) => name == "__self__" ? owner : throw Missing(name);
        public object? Invoke(object?[] args, string[] names, object?[] values) =>
            throw Error(new PyTypeError("Cannot instantiate typing.ClassVar"));
    }

    internal static T Error<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
    private static PyNotImplementedError Unsupported(string message) => Error(new PyNotImplementedError(message));
}
