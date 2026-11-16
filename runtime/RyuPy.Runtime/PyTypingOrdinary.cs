// 11 个普通 legacy typing 根共享 _SpecialGenericAlias 的原始键池。
// 类型转换、arity 和构造 helper 仅在真冷正文消费；Tuple/Callable/native 仍走原入口。
namespace RyuPy.Runtime;

public static partial class PyTyping
{
    // 静态方法组只在池初始化时建立一次；热查找不分配闭包、参数数组或归一化键。
    private static readonly PyTypingRawCache OrdinaryCache = new(OrdinaryUncached);

    private static object OrdinaryUncached(PyTypingForm receiver, object? parameter)
    {
        object?[] args = parameter is PyTuple tuple ? tuple.Items : [parameter];
        var normalized = new object?[args.Length];
        // CP 先逐项 _type_check，再检查数量。复用有限 F0 检查，未知合法值明确 NI，
        // 不沿用旧 TypeArgumentCore 的笼统 TypeError，也不把新守卫扩给其它池。
        for (int i = 0; i < args.Length; i++)
            normalized[i] = SpecialTypeArgument(args[i], "Parameters to generic types must be types.");
        GuardHelper("_check_generic");
        int required = receiver.Name is "Dict" or "Mapping" or "MutableMapping" ? 2 : 1;
        if (args.Length != required)
            throw new PyTypeError($"Too {(args.Length > required ? "many" : "few")} arguments for {receiver.PythonRepr()};"
                + $" actual {args.Length}, expected {required}");
        GuardHelper("_GenericAlias");
        GuardHelper("_TypingEllipsis");
        GuardHelper("_collect_parameters");
        foreach (object? value in normalized) RequireOrdinaryParameters(value);
        // 不再调用 Intern：不同 raw miss 即使归一化后相等，也必须得到新对象。
        // receiver 是保存的真实根；公开 typing.List 等导出改绑不改变此次 self。
        return new PyTypingAlias(receiver.Name, Origin(receiver.Name), normalized);
    }

    // 现惰性收集器只递归本载体的 alias；不能对嵌套 Generic 专用载体或 tuple
    // 静默返回空 parameters。此处只看已有结构，不读普通类或标量的自定义属性。
    private static void RequireOrdinaryParameters(object? value)
    {
        if (value is PyTypingGenericAlias or PyTuple)
            throw new PyNotImplementedError("typing ordinary alias parameter collection is not supported for this argument");
        if (value is PyTypingAlias alias)
            foreach (object? argument in alias.Arguments) RequireOrdinaryParameters(argument);
    }
}
