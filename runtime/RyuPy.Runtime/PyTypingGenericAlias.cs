// Generic 根的 TypeVar-only 别名；与既有 typing.List[T] 共用 Python _GenericAlias 类型身份。
// 核心元数据改写及普通属性转发暂缓并显式 NI，不能静默保持错误的旧参数/缓存身份。
using System;

namespace RyuPy.Runtime;

internal sealed class PyTypingGenericAlias : PyNativeObject, IPyNativeCallable
{
    private readonly PyTuple _arguments;
    private readonly PyTuple _parameters;
    private readonly object? _module;
    internal PyTypingGenericAlias(object?[] arguments)
    {
        _arguments = new PyTuple((object?[])arguments.Clone());
        // args/parameters 内容相同但必须是两个稳定的 tuple 对象。
        _parameters = new PyTuple(_arguments.Items);
        _module = PyTypingGeneric.Root.NativeModuleValue;
    }
    internal PyTuple Arguments => _arguments;
    internal PyTuple Parameters => _parameters;
    public override string TypeName => "_GenericAlias";
    public override object? GetAttr(string name) => name switch
    {
        "__class__" => PyObject.TypeOf(this),
        "__origin__" => PyTypingGeneric.Root,
        "__args__" => _arguments, "__parameters__" => _parameters,
        "__module__" => _module,
        "__name__" or "__qualname__" => PyObject.GetAttr(PyTypingGeneric.Root, "__name__"),
        "__eq__" => PyNativeFunctions.Create("__eq__", ["other"], [null], 1, args => Equal(args[0])),
        "__ne__" => PyNativeFunctions.Create("__ne__", ["other"], [null], 1, args =>
            Equal(args[0]) is bool equal ? !equal : PyNotImplemented.Instance),
        "__hash__" => PyNativeFunctions.Create("__hash__", [], [], 0,
            _ => PyHash.Of(new PyTuple([PyTypingGeneric.Root, _arguments]))),
        "__getitem__" => PyNativeFunctions.Create("__getitem__", ["args"], [null], 1,
            _ => throw PyTypingGeneric.Error(new PyTypeError("Cannot subscript already-subscripted " + PythonRepr()))),
        "__mro_entries__" => PyNativeFunctions.Create("__mro_entries__", ["bases"], [null], 1, args => MroEntries(args[0])),
        "__call__" => new Call(this),
        "__repr__" or "__str__" => PyNativeFunctions.Create(name, [], [], 0, _ => PythonRepr()),
        "__instancecheck__" or "__subclasscheck__" => PyNativeFunctions.Create(name, ["cls"], [null], 1,
            _ => PyTypingGeneric.RejectRuntimeAlias()),
        "__dict__" => throw PyTypingGeneric.Unsupported("Generic alias dictionary reflection is not supported"),
        _ when !name.StartsWith("__", StringComparison.Ordinal) => PyObject.GetAttr(PyTypingGeneric.Root, name),
        _ => throw Missing(name),
    };
    public override void SetAttr(string name, object? value) =>
        throw PyTypingGeneric.Unsupported($"changing Generic alias attribute {name} is not supported");
    public override void DelAttr(string name) =>
        throw PyTypingGeneric.Unsupported($"deleting Generic alias attribute {name} is not supported");

    private object Equal(object? other)
    {
        if (other is not PyTypingGenericAlias alias) return PyNotImplemented.Instance;
        if (_arguments.Items.Length != alias._arguments.Items.Length) return false;
        for (int i = 0; i < _arguments.Items.Length; i++)
            if (!ReferenceEquals(_arguments.Items[i], alias._arguments.Items[i])) return false;
        return true;
    }

    // 首切只承诺 exact tuple 的单元素真实 alias 域，等价但非同一对象也须命中 index。
    internal object MroEntries(object? bases)
    {
        var attributes = PyTyping.CreateAttributes();
        PyTypingGeneric.GuardExport(attributes);
        if (attributes.ContainsKey("Protocol"))
            throw PyTypingGeneric.Unsupported("Generic mro_entries with a replaced Protocol global is not supported");
        if (bases is not PyTuple tuple || tuple.GetType() != typeof(PyTuple) || tuple.Items.Length != 1
            || tuple.Items[0] is not PyTypingGenericAlias)
            throw PyTypingGeneric.Unsupported("Generic __mro_entries__ currently requires a sole Generic alias in an exact tuple");
        if (Equal(tuple.Items[0]) is not true)
            throw PyTypingGeneric.Error(new PyValueError("tuple.index(x): x not in tuple"));
        return new PyTuple([PyTypingGeneric.Root]);
    }

    public object? Invoke(object?[] args, string[] names, object?[] values)
    {
        // CP 返回后写 __orig_class__；空根会拒绝该写入。未知 setattr 不得吞成普通 AttributeError。
        object result = PyTypingGeneric.Construct(args, names);
        PyTypingGeneric.GuardProtocol("__setattr__");
        if (PyTypingGeneric.Root.Attributes.TryGetValue("__orig_class__", out object? descriptor)
            && descriptor is PyProperty or IPyNativeDescriptor or PyInstance)
            throw PyTypingGeneric.Unsupported("Generic alias orig_class descriptor assignment is not supported");
        return result;
    }
    public override string PythonRepr()
    {
        // module 取当前根值，alias 自己的 module 仍是创建时快照。
        var parts = new string[_arguments.Items.Length];
        for (int i = 0; i < parts.Length; i++) parts[i] = PyTyping.Display(_arguments.Items[i]);
        object? module = PyTypingGeneric.Root.NativeModuleValue;
        // CP 先比较 module 与 builtins，再做空格式规范；不能以 __str__ 替代未知 __format__。
        if (module is PyInstance or PyNativeObject or PyBaseException)
            throw PyTypingGeneric.Unsupported("Generic alias module with custom comparison or formatting is not supported");
        string prefix = module is "builtins" ? "" : PyRepr.Str(module) + ".";
        return prefix + PyObject.GetAttr(PyTypingGeneric.Root, "__qualname__") + "[" + string.Join(", ", parts) + "]";
    }
    private sealed class Call(PyTypingGenericAlias owner) : PyNativeObject, IPyNativeCallable
    {
        public override string TypeName => "method";
        public override object? GetAttr(string name) => name == "__self__" ? owner : throw Missing(name);
        public object? Invoke(object?[] args, string[] names, object?[] values) => owner.Invoke(args, names, values);
    }
}
