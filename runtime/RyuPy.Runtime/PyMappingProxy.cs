using System;

namespace RyuPy.Runtime;

/// <summary>
/// mappingproxy 的真实只读载体：保留底层引用、类型身份和读取回调，不作为 dict 派生存储公开。
/// 闭合局部读取、迭代和运算；消费者独立接线，尚不注册 types 模块。
/// </summary>
public sealed class PyMappingProxy : PyNativeObject
{
    /// <summary>稳定真实类型；Enum.__members__与显式代理使用同一载体。</summary>
    public static readonly PyNativeType Type = new("mappingproxy", "builtins", Create,
        value => value is PyMappingProxy);

    // 显式类型初始化保证首次 Create 之前已登记真实身份，不依赖 beforefieldinit 的延迟选择。
    static PyMappingProxy() { }

    /// <summary>只在运行时内部委托，不作为 Python 属性暴露；嵌套代理保持原层而不展开。</summary>
    internal object Mapping { get; }

    private PyMappingProxy(object mapping) => Mapping = mapping;

    /// <summary>原生构造绑定：CPython 先检查位置与关键字总数，再寻找 mapping。</summary>
    public static object Create(object?[] args, string[] names, object?[] values)
    {
        int count = args.Length + names.Length;
        if (count > 1)
            throw Error(new PyTypeError($"mappingproxy() takes at most 1 argument ({count} given)"));
        object? mapping;
        if (args.Length == 1) mapping = args[0];
        else if (names.Length == 1 && names[0] == "mapping") mapping = values[0];
        else throw Error(new PyTypeError("mappingproxy() missing required argument 'mapping' (pos 1)"));
        PyMappingProxyProtocol.Validate(mapping);
        return new PyMappingProxy(mapping!);
    }

    /// <inheritdoc />
    public override string TypeName => "mappingproxy";

    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "__class__" => Type,
        "__getitem__" => Method(name, ["key"], [null], 1,
            a => PyMappingProxyProtocol.Item(Mapping, a[0])),
        "__len__" => Method(name, [], [], 0, _ => PyOps.Len(Mapping)),
        "get" => Method(name, ["key", "default"], [null, PyNone.Instance], 1,
            a => PyMappingProxyProtocol.Get(Mapping, a[0], a[1])),
        "keys" or "values" or "items" or "copy" => Method(name, [], [], 0,
            _ => PyMappingProxyProtocol.Method(Mapping, name)),
        "__hash__" => Method(name, [], [], 0, _ => PyHash.Of(Mapping)),
        "__iter__" => Method(name, [], [], 0, _ => Iterate()),
        "__contains__" => Method(name, ["key"], [null], 1,
            a => PyMappingProxyIteration.Contains(Mapping, a[0])),
        "__reversed__" => Method(name, [], [], 0, _ => PyMappingProxyIteration.Reverse(Mapping)),
        "__eq__" or "__ne__" => Method(name, ["other"], [null], 1,
            a => PyMappingProxyOperators.Compare(Mapping, a[0], name == "__ne__")),
        "__or__" => Method(name, ["other"], [null], 1, a => PyMappingProxyOperators.Union(this, a[0])),
        "__ror__" => Method(name, ["other"], [null], 1, a => PyMappingProxyOperators.Union(a[0], this)),
        "__ior__" => Method(name, ["other"], [null], 1, _ => PyMappingProxyOperators.InPlace()),
        _ => throw Error(Missing(name)),
    };

    /// <inheritdoc />
    public override void SetAttr(string name, object? value) => throw Error(Missing(name));
    /// <inheritdoc />
    public override void DelAttr(string name) => throw Error(Missing(name));
    /// <inheritdoc />
    public override string PythonRepr() => "mappingproxy(" + PyRepr.Repr(Mapping) + ")";
    /// <inheritdoc />
    public override string PythonStr() => PyRepr.Str(Mapping);

    /// <summary>Python 公开 iter 返回原用户对象，内部适配器不通过此入口外泄。</summary>
    internal object Iterate() => PyMappingProxyIteration.Raw(Mapping);

    private static PyFunction Method(string name, string[] parameters, object?[] defaults,
        int required, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, parameters, defaults, required, body,
            Array.ConvertAll(parameters, _ => "posonly"));

    /// <summary>仅本 API 新建的错误在这里准备；用户回调异常不捕获、不替换帧或上下文。</summary>
    internal static PyBaseException Error(PyBaseException error) => (PyBaseException)PyErrors.PrepareRaise(error);
    internal static PyBaseException Unsupported(string operation) => Error(new PyNotImplementedError(
        "mappingproxy " + operation + " is not supported in the current runtime stage"));
}
