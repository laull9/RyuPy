// collections.namedtuple 的真实公开入口；对象无 schema 状态，只直达已闭合的本 API 工厂。
// 不创建伪 Python 帧或第二套绑定器；保护未实现的函数反射，避免看似可写但行为不变。
namespace RyuPy.Runtime;

internal sealed class PyNamedTupleExport : PyNativeObject, IPyNativeCallable, IPyNativeDescriptor
{
    internal static readonly PyNamedTupleExport Instance = new();
    private PyNamedTupleExport() { }
    public override string TypeName => "function";
    public object? Invoke(object?[] args, string[] names, object?[] values) => PyNamedTupleFactory.Invoke(args, names, values);
    public override object? GetAttr(string name) => name switch
    {
        "__name__" or "__qualname__" => "namedtuple",
        "__module__" => "collections",
        "__defaults__" => PyNone.Instance,
        "__class__" => PyType.Get("function"),
        "__doc__" or "__dict__" or "__code__" or "__closure__" or "__annotations__" or "__kwdefaults__"
            or "__get__" or "__reduce__" or "__reduce_ex__" => throw PyNamedTupleLayout.Unsupported("namedtuple factory reflection is not supported"),
        _ => throw Missing(name),
    };
    public object? Bind(PyClass owner, object? self) => self is null or PyNone ? this
        : throw PyNamedTupleLayout.Unsupported("binding namedtuple factory as an instance method is not supported");
    public override void SetAttr(string name, object? value) => throw PyNamedTupleLayout.Unsupported("changing namedtuple factory metadata is not supported");
    public override void DelAttr(string name) => throw PyNamedTupleLayout.Unsupported("changing namedtuple factory metadata is not supported");
    public override string PythonRepr() => "<function namedtuple>";
}
