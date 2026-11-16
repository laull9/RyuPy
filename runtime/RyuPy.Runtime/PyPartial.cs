// partial 保留函数与位置实参的强引用，关键字字典为实时可变对象；它不是方法描述符。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>functools.partial 的显式原生可调用对象。</summary>
public sealed class PyPartial : PyNativeObject, IPyNativeCallable
{
    private readonly object? _function;
    private readonly PyTuple _args;
    private readonly PyDict _keywords;
    private PyDict? _attributes;
    /// <summary>创建部分应用；构造本身不调用目标函数。</summary>
    public PyPartial(object? function, object?[] args, string[] names, object?[] values)
    {
        if (!PyFunctools.IsCallable(function)) throw new PyTypeError("the first argument must be callable");
        _keywords = new PyDict();
        if (function is PyPartial inner && inner._attributes is null)
        {
            _function = inner._function;
            var merged = new object?[inner._args.Length + args.Length];
            Array.Copy(inner._args.Items, merged, inner._args.Length);
            Array.Copy(args, 0, merged, inner._args.Length, args.Length);
            _args = new PyTuple(merged); _keywords.Update(inner._keywords);
        }
        else { _function = function; _args = new PyTuple((object?[])args.Clone()); }
        for (int i = 0; i < names.Length; i++) _keywords.Set(names[i], values[i]);
    }
    /// <inheritdoc/>
    public override string TypeName => "partial";
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "func" => _function, "args" => _args, "keywords" => _keywords, "__dict__" => _attributes ??= new PyDict(),
        "__call__" => this,
        _ => _attributes is not null && _attributes.TryGet(name, out object? value) ? value : throw Missing(name),
    };
    /// <inheritdoc/>
    public override void SetAttr(string name, object? value)
    {
        if (name is "func" or "args" or "keywords") throw new PyAttributeError("readonly attribute");
        if (name == "__dict__" && value is PyOrderedDict) throw PyOrderedDict.Unsupported("partial metadata storage");
        if (name == "__dict__") _attributes = value as PyDict ?? throw new PyTypeError("__dict__ must be a dictionary");
        else (_attributes ??= new PyDict()).Set(name, value);
    }
    /// <inheritdoc/>
    public override void DelAttr(string name)
    {
        if (name is "func" or "args" or "keywords" or "__dict__") throw new PyAttributeError("readonly attribute");
        if (_attributes is null || !_attributes.TryGet(name, out _)) throw Missing(name);
        _attributes.Delete(name);
    }
    /// <inheritdoc/>
    public object? Invoke(object?[] args, string[] names, object?[] values)
    {
        var combined = new object?[_args.Length + args.Length];
        Array.Copy(_args.Items, combined, _args.Length); Array.Copy(args, 0, combined, _args.Length, args.Length);
        var keywords = new PyDict(); keywords.Update(_keywords);
        for (int i = 0; i < names.Length; i++) keywords.Set(names[i], values[i]);
        var mergedNames = new List<string>(); var mergedValues = new List<object?>();
        foreach (var (key, value) in keywords.Entries())
        {
            if (key is not string text) throw new PyTypeError("keywords must be strings");
            mergedNames.Add(text); mergedValues.Add(value);
        }
        return PyOps.CallValue(_function, combined, mergedNames.ToArray(), mergedValues.ToArray());
    }
}
