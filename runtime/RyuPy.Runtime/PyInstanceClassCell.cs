// 实例词法class cell的冷能力边界：最终namespace确定后检查尚未实现的类创建回调。
// 不执行或模拟hook，不触及默认类/无classcell路径；普通内建property包装仍可使用。
namespace RyuPy.Runtime;

public static partial class PyModuleInstances
{
    /// <summary>普通终结成功后、填充class cell前检查真实类型级的未支持回调。</summary>
    public static void ClassCellGuard(object value)
    {
        if (value is not PyClass cls)
            throw new PyTypeError("class cell finalization requires an ordinary class");
        // CP只在字段值的真实类型链查__set_name__，不查值自身字典，也不按可调用性猜。
        foreach (object? attribute in cls.Attributes.Values)
        {
            PyClass? type = attribute switch
            {
                PyInstance instance => instance.Class,
                PyNamedTupleInstance tuple => tuple.Class,
                PyBaseException exception => exception.PythonClass,
                _ => null,
            };
            if (type is not null && type.TryLookup("__set_name__", out _, out _))
                throw new PyNotImplementedError("class cell definitions with __set_name__ hooks are not implemented");
        }
        // 只消费实际父链；本类新定义的__init_subclass__属于将来的派生类创建。
        if (cls.BaseClass is { } parent && parent.TryLookup("__init_subclass__", out _, out _))
            throw new PyNotImplementedError("class cell definitions with inherited __init_subclass__ hooks are not implemented");
    }
}
