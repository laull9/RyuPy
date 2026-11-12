using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>原生标准库类型的明确构造/类型判定表，不以函数对象伪装类型，也不逐实例反射。</summary>
public sealed class PyNativeType
{
    private static readonly ConcurrentDictionary<string, PyNativeType> Types = new(StringComparer.Ordinal);
    private readonly Func<object?[], string[], object?[], object?>? _factory;
    private readonly Func<object?, bool> _accepts;
    private readonly HashSet<string> _bases;
    private PyClass? _identityBase;
    private readonly Dictionary<string, object?> _attributes = new(StringComparer.Ordinal);
    /// <summary>Python类型短名称。</summary>
    public string Name { get; }
    /// <summary>类型定义模块。</summary>
    public string Module { get; }

    /// <summary>登记受支持的原生类型；bases为明确支持的全部祖先短名。</summary>
    public PyNativeType(string name, string module, Func<object?[], string[], object?[], object?>? factory,
        Func<object?, bool> accepts, params string[] bases)
    {
        Name = name; Module = module; _factory = factory; _accepts = accepts; _bases = new(bases, StringComparer.Ordinal);
        Types[name] = this;
    }

    // 内部真实根身份元数据；不接受公开字符串授权，不改变公开构造ABI。
    internal void ConfigureIdentityBase(PyClass root)
    {
        if (_identityBase is not null) throw new System.InvalidOperationException("native identity base already configured");
        _identityBase = root;
        _attributes["__base__"] = root;
        _attributes["__bases__"] = new PyTuple([root]);
        var nodes = new List<object?> { this };
        nodes.AddRange(root.MroTuple().Items);
        _attributes["__mro__"] = new PyTuple(nodes.ToArray());
    }
    internal bool IsIdentitySubclassOf(PyClass root) => _identityBase?.IsSubclassOf(root) == true;
    /// <summary>按实例协议声明的名字寻找已登记类型。</summary>
    public static PyNativeType? LookupByName(string name) => Types.TryGetValue(name, out var type) ? type : null;
    /// <summary>原生对象类型检查。</summary>
    public bool Accepts(object? value) => _accepts(value);
    /// <summary>受支持的原生类继承关系。</summary>
    public bool IsSubclassOf(PyNativeType other) => ReferenceEquals(this, other) || _bases.Contains(other.Name);
    /// <summary>原生类已明确登记的内建祖先，例如defaultdict的dict。</summary>
    public bool IsSubclassOfBuiltin(string name) => name == "object" || _bases.Contains(name);
    /// <summary>调用类构造器，未开放的构造明确拒绝。</summary>
    public object? Invoke(object?[] args, string[] names, object?[] values) => _factory is null
        ? throw new PyNotImplementedError($"construction of {Module}.{Name} is not supported")
        : _factory(args, names, values);
    /// <summary>读取类型名或显式登记的类方法。</summary>
    public object? GetAttr(string name) => name switch
    {
        "mro" or "__dict__" or "__flags__" or "__text_signature__" when ReferenceEquals(this, PyOrderedDict.Type)
            => throw PyOrderedDict.Unsupported("type reflection " + name),
        "__class__" when ReferenceEquals(this, PyLexicalSuper.Type) => PyType.Get("type"),
        "__new__" or "__init__" or "__get__" or "__mro__" or "mro" or "__dict__" or "__bases__" or "__base__"
            or "__thisclass__" or "__self__" or "__self_class__"
            when ReferenceEquals(this, PyLexicalSuper.Type)
            => throw new PyNotImplementedError($"super type reflection '{name}' is not supported"),
        "__name__" or "__qualname__" => Name, "__module__" => Module,
        _ => _attributes.TryGetValue(name, out object? value) ? value
            : throw new PyAttributeError($"type object '{Name}' has no attribute '{name}'"),
    };
    /// <summary>注册原生类方法；不是Python层任意属性修改入口。</summary>
    public void SetAttr(string name, object? value) => _attributes[name] = value;
    /// <inheritdoc />
    public override string ToString() => Module == "builtins"
        ? $"<class '{Name}'>" : $"<class '{Module}.{Name}'>";
}
