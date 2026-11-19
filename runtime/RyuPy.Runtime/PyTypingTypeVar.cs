// TypeVar 是每次独立创建的真实原生值；核心描述符、可写 extra 与隐式类型槽各自独立。
// 名称和约束保留原对象引用，模块可写删；绝不把变量冒充可实例化的 class。
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

internal sealed class PyTypingTypeVar : PyNativeObject
{
    internal static readonly PyNativeType Type = new("TypeVar", "typing", PyTypingTypeVarFactory.Invoke,
        static value => value is PyTypingTypeVar);
    static PyTypingTypeVar() { }
    private readonly object _name;
    private readonly object _bound;
    private readonly PyTuple _constraints;
    private readonly bool _covariant;
    private readonly bool _contravariant;
    private readonly bool _inferVariance;
    private object? _module;
    private bool _hasModule = true;
    private Dictionary<string, object?>? _extra;

    internal PyTypingTypeVar(object name, object bound, PyTuple constraints,
        bool covariant, bool contravariant, bool inferVariance, object module)
    {
        _name = name; _bound = bound; _constraints = constraints;
        _covariant = covariant; _contravariant = contravariant; _inferVariance = inferVariance;
        _module = module;
    }

    public override string TypeName => "typing.TypeVar";

    public override object? GetAttr(string name)
    {
        // 核心数据描述符先于实例字典；普通魔术属性仍可被 extra 显式遮蔽。
        switch (name)
        {
            case "__class__": return Type;
            case "__name__": return _name;
            case "__bound__": return _bound;
            case "__constraints__": return _constraints;
            case "__covariant__": return _covariant;
            case "__contravariant__": return _contravariant;
            case "__infer_variance__": return _inferVariance;
            case "__module__": return _hasModule ? _module : "typing";
        }
        if (_extra is not null && _extra.TryGetValue(name, out object? value)) return value;
        if (TryGetSpecial(name, out object? method)) return method;
        throw Missing(name);
    }

    public override void SetAttr(string name, object? value)
    {
        if (name == "__class__")
            throw Own(new PyTypeError("__class__ assignment only supported for mutable types or ModuleType subclasses"));
        GuardCore(name);
        if (name == "__module__") { _hasModule = true; _module = value; return; }
        (_extra ??= new(StringComparer.Ordinal))[name] = value;
    }

    public override void DelAttr(string name)
    {
        if (name == "__class__") throw Own(new PyTypeError("can't delete __class__ attribute"));
        GuardCore(name);
        if (name == "__module__")
        {
            if (!_hasModule) throw Missing(name);
            _hasModule = false; _module = null; return;
        }
        if (_extra is null || !_extra.Remove(name)) throw Missing(name);
    }

    private static void GuardCore(string name)
    {
        if (name is "__name__" or "__covariant__" or "__contravariant__" or "__infer_variance__")
            throw Own(new PyAttributeError("readonly attribute"));
        if (name is "__bound__" or "__constraints__")
            throw Own(new PyAttributeError($"attribute '{name}' of 'typing.TypeVar' objects is not writable"));
    }

    // 只读取真实类型槽，不读 extra；未知槽返回缺失，已有但未实现的运算明确 NI。
    internal bool TryGetSpecial(string name, out object? method)
    {
        if (name is "__eq__" or "__ne__" or "__hash__" or "__repr__" or "__str__"
            or "__or__" or "__ror__" or "__reduce__" or "__reduce_ex__")
        { method = new Method(this, name); return true; }
        method = null; return false;
    }

    public override string PythonRepr()
    {
        string text = _name is PyStringInstance instance ? instance.Value : (string)_name;
        return (_inferVariance ? "" : _covariant ? "+" : _contravariant ? "-" : "~") + text;
    }

    internal static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);

    private sealed class Method(PyTypingTypeVar owner, string name) : PyNativeObject, IPyNativeCallable
    {
        public override string TypeName => "method-wrapper";
        public override object? GetAttr(string attribute) => attribute switch
        {
            "__self__" => owner, "__name__" => name,
            _ => throw Missing(attribute),
        };
        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            if (name is "__or__" or "__ror__" or "__reduce__" or "__reduce_ex__")
                throw Own(new PyNotImplementedError("TypeVar union operators and serialization are not supported"));
            int count = name is "__eq__" or "__ne__" ? 1 : 0;
            if (names.Length != 0 || args.Length != count)
                throw Own(new PyNotImplementedError("TypeVar special method keyword and nonstandard arity binding is not supported"));
            if (name == "__hash__") return (long)RuntimeHelpers.GetHashCode(owner);
            if (name is "__repr__" or "__str__") return owner.PythonRepr();
            // 异对象必须留给右操作数真正的类型级比较，不能直接返回 false。
            return ReferenceEquals(owner, args[0]) ? name == "__eq__" : PyNotImplemented.Instance;
        }
    }
}
