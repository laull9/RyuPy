// typing.Tuple 独占 _TupleType 的原始键 128 项 LRU，不与普通根或旧驻留池共享身份。
// 只在冷正文逐项检查类型并消费构造 helper；空参数和尾 Ellipsis 保留各自的读取次序。
namespace RyuPy.Runtime;

public static partial class PyTyping
{
    // 原 receiver/parameter 直接进入独立池；静态方法组仅在初始化时创建一次。
    private static readonly PyTypingRawCache TupleCache = new(TupleUncached);

    private static object TupleUncached(PyTypingForm receiver, object? parameter)
    {
        // RawCache 已拒绝 tuple 派生与未知键协议，此处只展开原始 exact tuple。
        object?[] args = parameter is PyTuple tuple ? tuple.Items : [parameter];
        bool trailingEllipsis = args.Length >= 2 && ReferenceEquals(args[^1], PyEllipsis.Instance);
        int checkedCount = trailingEllipsis ? args.Length - 1 : args.Length;
        string message = trailingEllipsis
            ? "Tuple[t, ...]: t must be a type."
            : "Tuple[t0, t1, ...]: each t must be a type.";
        var normalized = new object?[args.Length];
        // 尾控制符前可有多个类型，全部按序检查；单独或中间 Ellipsis 仍走有限检查器。
        // 空 tuple 不执行 _type_check，也不读取该检查器的 Generic/ClassVar 等依赖。
        for (int i = 0; i < checkedCount; i++) normalized[i] = SpecialTypeArgument(args[i], message);
        if (trailingEllipsis)
        {
            // 所有前缀成功后才读取 sentinel；不能提前盖过真实类型错误。
            GuardHelper("_TypingEllipsis");
            normalized[^1] = PyEllipsis.Instance;
        }
        // Tuple 没有 _check_generic 或固定 arity；copy_with 在这里才消费构造器。
        GuardHelper("_GenericAlias");
        if (normalized.Length != 0) GuardHelper("_TypingEllipsis");
        GuardHelper("_collect_parameters");
        foreach (object? value in normalized) RequireOrdinaryParameters(value);
        // 仅复用已验证的有限参数树；不同 raw miss 不通过 Intern 重新合并身份。
        return new PyTypingAlias(receiver.Name, Origin(receiver.Name), normalized);
    }
}
