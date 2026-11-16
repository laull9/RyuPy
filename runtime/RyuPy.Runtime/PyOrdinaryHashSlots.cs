// 已完成普通类的hash槽及空容器准入；不重算构造布局、不开放ABC或任意描述符。
namespace RyuPy.Runtime;

public sealed partial class PyClass
{
    // 复用类体完成事实，dataclass装饰不撤销；String、异常与专用native链不获此资格。
    // C#手造但未Finalize的PyClass仍保原TryMethod路径，不扩大或封死宿主API。
    internal bool HasOrdinaryHashSlots => _ordinaryFinalized;
}

public static partial class PyHash
{
    // 调用者已排除非PyInstance且确认容器为空。只拒原始None，不执行hash/eq/getattr。
    // 非空容器让comparer调用Of，避免正常用户键先筛后算造成两次类槽查询。
    internal static void RequireOrdinaryHashable(PyInstance instance)
    {
        if (instance is PyListInstance or PyDictInstance) throw OrdinaryUnhashable(instance);
        instance.Class.GuardInheritedGenericProtocol("__hash__");
        if (instance.Class.HasOrdinaryHashSlots
            && instance.Class.TryLookup("__hash__", out _, out object? raw)
            && raw is null or PyNone)
            throw OrdinaryUnhashable(instance);
    }

    // 一次版本化原始槽查找；缺失保身份fallback，用户函数沿原InvokeBound且不造绑定wrapper。
    private static long? OrdinaryHash(PyInstance instance)
    {
        if (!instance.Class.TryLookup("__hash__", out PyClass owner, out object? raw)) return null;
        if (owner.IsTypingGenericRoot) PyTypingGeneric.GuardProtocol("__hash__");
        if (raw is null or PyNone) throw OrdinaryUnhashable(instance);
        object? result = PyObject.InvokeBound(raw, owner, instance, [], [], []);
        // 返回归约与通用用户路径共用；不重调结果对象的转换或 hash 协议。
        return UserHashResult(result);
    }

    private static PyTypeError OrdinaryUnhashable(PyInstance instance) =>
        (PyTypeError)PyErrors.PrepareRaise(new PyTypeError($"unhashable type: '{instance.Class.Name}'"));
}
