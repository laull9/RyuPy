// 生成类可读取的有限tuple根方法；共享非绑定描述符，显式self接受整个tuple家族。
namespace RyuPy.Runtime;

internal static class PyNamedTupleTupleMethods
{
    private static readonly Method Count = new("count");
    private static readonly Method Index = new("index");
    private static readonly Method Length = new("__len__");

    internal static bool Has(string name) => name is "count" or "index" or "__len__";
    internal static object Unbound(string name) => name switch
    {
        "count" => Count,
        "index" => Index,
        "__len__" => Length,
        _ => throw new PyAttributeError($"type object 'tuple' has no attribute '{name}'"),
    };

    private sealed class Method(string name) : PyNativeObject, IPyNativeCallable, IPyNativeDescriptor
    {
        public override string TypeName => name == "__len__" ? "wrapper_descriptor" : "method_descriptor";
        public override object? GetAttr(string attribute) => attribute switch
        {
            "__name__" => name,
            "__qualname__" => "tuple." + name,
            "__objclass__" => PyType.Get("tuple"),
            "__class__" => PyType.Get(TypeName),
            "__dict__" or "__get__" or "__reduce__" or "__reduce_ex__" => throw PyNamedTupleLayout.Unsupported("tuple method descriptor reflection is not supported"),
            _ => throw Missing(attribute),
        };
        public override void SetAttr(string attribute, object? value) => throw PyNamedTupleLayout.Unsupported("changing tuple method descriptor metadata is not supported");
        public override void DelAttr(string attribute) => throw PyNamedTupleLayout.Unsupported("changing tuple method descriptor metadata is not supported");

        public object? Bind(PyClass owner, object? self) => self is null or PyNone
            ? this : PyOps.PrepareNamedTupleInheritedMethod(Receiver(self), name);

        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            if (args.Length == 0)
                throw PyNamedTupleLayout.Error(new PyTypeError($"descriptor '{name}' of 'tuple' object needs an argument"));
            PyTuple tuple = Receiver(args[0]);
            return PyOps.CallNamedTupleInheritedMethod(tuple, name, args[1..], names, values);
        }

        private PyTuple Receiver(object? value) => value as PyTuple
            ?? throw PyNamedTupleLayout.Error(new PyTypeError($"descriptor '{name}' for 'tuple' objects doesn't apply to a '{PyRepr.TypeName(value)}' object"));

        public override string PythonRepr() => name == "__len__"
            ? "<slot wrapper '__len__' of 'tuple' objects>" : $"<method '{name}' of 'tuple' objects>";
    }
}
