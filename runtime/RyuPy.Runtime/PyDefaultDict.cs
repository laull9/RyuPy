using System;

namespace RyuPy.Runtime;

/// <summary>defaultdict 只在缺键下标读取时调用无参数工厂，普通 get/in 不调用工厂。</summary>
public sealed class PyDefaultDict : PyCollectionsMapping
{
    private object? _factory;
    internal PyDefaultDict(object? factory) { ValidateFactory(factory); _factory = factory; }
    /// <inheritdoc />
    public override string TypeName => "defaultdict";
    private static void ValidateFactory(object? factory)
    {
        if (factory is not null and not PyNone && !PyOps.Truthy(PyOps.CallBuiltin("callable", [factory], [], [])))
            throw new PyTypeError("first argument must be callable or None");
    }
    /// <inheritdoc />
    protected override object? MissingKey(object? key)
    {
        if (_factory is null or PyNone) throw PyKeyError.ForKey(key);
        object? value = PyOps.CallValue(_factory, [], [], []);
        Dictionary.Set(key, value);
        return value;
    }
    /// <inheritdoc />
    protected override PyCollectionsMapping Copy()
    {
        var result = new PyDefaultDict(_factory); result.UpdateFrom(Dictionary); return result;
    }
    /// <inheritdoc />
    public override object? GetAttr(string name) => name == "default_factory" ? _factory ?? PyNone.Instance : base.GetAttr(name);
    /// <inheritdoc />
    public override void SetAttr(string name, object? value)
    {
        if (name != "default_factory") { base.SetAttr(name, value); return; }
        // CPython 属性可先赋任意值；非callable在实际缺键调用时才报错。
        _factory = value;
    }
    /// <inheritdoc />
    public override string PythonRepr() => ReprMapping(body => $"defaultdict({PyRepr.Repr(_factory)}, {body})");
}
