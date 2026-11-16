// typing.Callable 的外层形状检查与 list 快照位于缓存之外；inner 独占 128 项原始键 LRU。
// 冷正文先检查 result，再仅转换 args，最后消费真实构造与采参 helper，不使用旧驻留池。
namespace RyuPy.Runtime;

public static partial class PyTyping
{
    private static readonly PyTypingRawCache CallableCache = new(CallableUncached);

    private static object CallableSubscribe(PyTypingForm receiver, object? parameter)
    {
        const string shapeError = "Callable must be used as Callable[[arg, ...], result].";
        if (parameter is not PyTuple tuple || tuple.GetType() != typeof(PyTuple))
        {
            // CP 的 isinstance 可读取自定义 __class__；未知实例及 tuple/list 派生不能
            // 冒充无副作用形状错误。这里只拒绝已有载体可证明的非 tuple 内建值。
            if (parameter is null or PyNone or bool or int or long or System.Numerics.BigInteger
                or double or string or PyEllipsis or PyType or PyNativeType or PyClass
                or PyTypedDictType or PyTypingTypeVar or PyTypingForm or PyTypingAlias
                or PyTypingGenericAlias or PyList or PyDict or PySet or PyFrozenSet
                or PyBytes or PyByteArray or PyRange or PySlice or PyBaseObject)
                throw new PyTypeError(shapeError);
            throw new PyNotImplementedError("typing.Callable outer tuple protocol is not supported");
        }
        if (tuple.Length != 2) throw new PyTypeError(shapeError);
        object? arguments = tuple.Items[0];
        // PyList 是 exact 内建载体，用户 list 派生由 PyInstance 表示。每次（含热命中）
        // 只取一次快照；原 list 后续修改不能污染已有 key 或 alias。原 tuple/scalar 不改形状。
        if (arguments is PyList list) arguments = new PyTuple(list.Items.ToArray());
        return CallableCache.Subscribe(receiver, new PyTuple([arguments, tuple.Items[1]]));
    }

    private static object CallableUncached(PyTypingForm receiver, object? parameter)
    {
        // 仅外层成功后才能进入；装饰器 TypeError 重试复用这份 pair，不再快照 list。
        object?[] pair = ((PyTuple)parameter!).Items;
        object result = SpecialTypeArgument(pair[1], "Callable[args, result]: result must be a type.");
        object? arguments = pair[0];
        if (ReferenceEquals(arguments, PyEllipsis.Instance))
            throw new PyNotImplementedError("typing.Callable Ellipsis parameter expressions are not supported");
        object?[] values = arguments is PyTuple tuple ? tuple.Items : [arguments];
        var flattened = new object?[values.Length + 1];
        for (int i = 0; i < values.Length; i++) flattened[i] = CallableTypeConvert(values[i]);
        flattened[^1] = result;
        // _CallableType.copy_with 真实消费专用构造器；不读取 _check_generic 或公开根导出。
        GuardHelper("_CallableGenericAlias");
        // 即使没有参数，flattened 仍包含 result，因此构造阶段总会读取此 sentinel。
        GuardHelper("_TypingEllipsis");
        GuardHelper("_collect_parameters");
        foreach (object? value in flattened) RequireOrdinaryParameters(value);
        return new PyTypingAlias(receiver.Name, Origin(receiver.Name), flattened);
    }

    private static object CallableTypeConvert(object? value)
    {
        GuardHelper("_type_convert");
        if (value is null or PyNone) return PyType.Get("NoneType");
        if (value is string)
            throw new PyNotImplementedError("typing.Callable argument forward references are not supported");
        // 参数位不执行 _type_check：裸 Generic / Union / Optional / Literal 合法。
        // Generic[T] 与嵌套 tuple 暂交真正采参相位的有限守卫，不能误报参数类型非法。
        if (value is PyType or PyNativeType or PyClass or PyTypedDictType or PyTypingTypeVar
            or PyTypingAlias or PyTypingForm or PyTypingGenericAlias or PyTuple) return value;
        throw new PyNotImplementedError("typing.Callable argument conversion is not supported for this value");
    }

    // 仅登记本切已证的直接父边；不声称完整 typing 类 MRO 或可调用类型工厂已支持。
    internal static bool IsCallableAliasBase(string derived, string parent) =>
        derived == "_CallableGenericAlias" && parent == "_GenericAlias";
}
