// 未求值ForwardRef的真实有限值：保存原注解文本/flags，提供稳定类型及相等、哈希和repr。
// 仅内部创建；不向typing导出占位。首次使用注册一次真实native type，不逐实例覆盖全局短名表。
using System;

namespace RyuPy.Runtime;

internal sealed class PyTypingForwardRef : PyNativeObject
{
    internal static readonly PyNativeType Type = new("ForwardRef", "typing",
        static (_, _, _) => throw Unsupported("explicit ForwardRef construction is not supported"),
        static value => value is PyTypingForwardRef);
    private static readonly PySignature Signature = PySignature.Create("ForwardRef.__init__",
        ["arg", "is_argument", "module", "is_class"], ["pos", "pos", "pos", "kwonly"], ["0", "1", "1", "1"]);
    private static readonly object?[] Defaults = [null, true, PyNone.Instance, false];
    // 显式静态构造确保任何Create先完成类型注册，不依赖beforefieldinit调度。
    static PyTypingForwardRef() { }
    private readonly string _argument;
    private readonly object _module;
    private readonly object _isArgument;
    private readonly object _isClass;

    private PyTypingForwardRef(string argument, object? isArgument, object? module, object? isClass)
    {
        _argument = argument;
        _module = module ?? PyNone.Instance;
        _isArgument = isArgument ?? PyNone.Instance;
        _isClass = isClass ?? PyNone.Instance;
    }

    // 与NamedTuple所需默认值一致；每次新建，不缓存相等文本或折叠两个字段的身份。
    internal static PyTypingForwardRef Create(string argument) => CreateChecked(argument, true, PyNone.Instance, false);

    // 内部有限绑定入口，便于后续真实导出复用；此阶段type(ref)(...)仍明确NI。
    internal static object Invoke(object?[] args, string[] names, object?[] values)
    {
        object?[] bound;
        // 只准备自有绑定错误，不catch正文/用户repr/资源错误，不增加假Python帧。
        try { bound = PyBinder.Bind(Signature, args, names, values, Defaults); }
        catch (PyTypeError error) { throw Own(error); }
        if (bound[0] is PyStringInstance)
            throw Unsupported("ForwardRef str subclass input is not supported");
        if (bound[0] is not string argument)
            throw Own(new PyTypeError("Forward reference must be a string -- got " + PyRepr.Repr(bound[0])));
        return CreateChecked(argument, bound[1], bound[2], bound[3]);
    }

    private static PyTypingForwardRef CreateChecked(string argument, object? isArgument, object? module, object? isClass)
    {
        PyTypingForwardRefSyntax.Validate(argument);
        // 先验证表达式，再保存module；有限域不调用用户相等/哈希/真值，也不偷偷str化。
        if (module is not (null or PyNone or string))
            throw Unsupported("ForwardRef module supports only None or an exact str");
        return new PyTypingForwardRef(argument, isArgument, module, isClass);
    }

    public override string TypeName => "ForwardRef";
    public override object? GetAttr(string name) => name switch
    {
        "__class__" => Type,
        "__forward_arg__" => _argument,
        "__forward_module__" => _module,
        "__forward_is_argument__" => _isArgument,
        "__forward_is_class__" => _isClass,
        "__forward_evaluated__" => false,
        "__forward_value__" => PyNone.Instance,
        "__eq__" or "__ne__" or "__hash__" or "__repr__" or "__str__" => new Method(this, name),
        "__forward_code__" or "_evaluate" or "__dict__" or "__reduce__" or "__reduce_ex__" or "__or__" or "__ror__"
            => throw Unsupported("ForwardRef evaluation and extended reflection are not supported"),
        _ => throw Missing(name),
    };
    public override void SetAttr(string name, object? value) => throw Unsupported("changing ForwardRef metadata is not supported");
    public override void DelAttr(string name) => throw Unsupported("deleting ForwardRef metadata is not supported");
    public override string PythonRepr() => "ForwardRef(" + PyRepr.Repr(_argument)
        + (_module is PyNone ? "" : ", module=" + PyRepr.Repr(_module)) + ")";

    private object Equal(object? other) => other is not PyTypingForwardRef reference ? PyNotImplemented.Instance
        : string.Equals(_argument, reference._argument, StringComparison.Ordinal) && PyHash.Equal(_module, reference._module);
    private long Hash() => PyHash.Of(new PyTuple([_argument, _module]));
    internal static PyNotImplementedError Unsupported(string message) => Own(new PyNotImplementedError(message));
    private static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);

    // 本地保护方法不暴露可写PyFunction伪元数据；每次取属性可以产生新的真实绑定对象。
    private sealed class Method(PyTypingForwardRef owner, string name) : PyNativeObject, IPyNativeCallable
    {
        private static readonly PySignature Unary = PySignature.Create("ForwardRef method", ["other"], ["pos"], ["0"]);
        private static readonly PySignature Nullary = PySignature.Create("ForwardRef method", [], [], []);
        public override string TypeName => "method";
        public override object? GetAttr(string attribute) => attribute switch
        {
            "__self__" => owner, "__name__" => name,
            _ => throw Unsupported("ForwardRef method reflection is not supported"),
        };
        public override void SetAttr(string attribute, object? value) => throw Unsupported("changing ForwardRef method metadata is not supported");
        public override void DelAttr(string attribute) => throw Unsupported("deleting ForwardRef method metadata is not supported");
        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            object?[] bound;
            try { bound = PyBinder.Bind(name is "__eq__" or "__ne__" ? Unary : Nullary, args, names, values, []); }
            catch (PyTypeError error) { throw Own(error); }
            if (name == "__hash__") return owner.Hash();
            if (name is "__repr__" or "__str__") return owner.PythonRepr();
            object equal = owner.Equal(bound[0]);
            return name == "__ne__" && equal is bool boolean ? !boolean : equal;
        }
    }
}
