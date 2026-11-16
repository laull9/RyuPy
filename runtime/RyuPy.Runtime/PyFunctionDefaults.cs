// 函数默认值的冷反射状态：保留公开tuple/dict身份，每次绑定读取当前值，绝不改写共享签名。
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RyuPy.Runtime;

internal sealed class PyFunctionDefaults
{
    private static ConditionalWeakTable<object?[], PyFunctionDefaults>? States;
    private readonly PySignature _signature;
    private readonly int _positionalCount;
    private PyTuple? _positional;
    private PyDict? _keywords;

    internal PyFunctionDefaults(PySignature signature, object?[] defaults)
    {
        _signature = signature;
        var positional = new List<object?>();
        for (int index = 0; index < signature.Names.Length; index++)
        {
            if (signature.Kinds[index] is PyParamKind.PositionalOnly or PyParamKind.PositionalOrKeyword)
                _positionalCount++;
            if (!signature.HasDefault[index]) continue;
            object? value = index < defaults.Length ? defaults[index] : PyNone.Instance;
            if (signature.Kinds[index] == PyParamKind.KeywordOnly)
                (_keywords ??= new PyDict()).Set(signature.Names[index], value);
            else if (signature.Kinds[index] is PyParamKind.PositionalOnly or PyParamKind.PositionalOrKeyword)
                positional.Add(value);
        }
        if (positional.Count != 0) _positional = new PyTuple(positional.ToArray());
    }

    internal static bool TryGet(object?[] defaults, out PyFunctionDefaults? state)
    {
        state = null;
        return States is { } states && states.TryGetValue(defaults, out state);
    }

    internal static void Register(object?[] defaults, PyFunctionDefaults state) =>
        LazyInitializer.EnsureInitialized(ref States).Add(defaults, state);

    internal object Get(string name) => name == "__defaults__"
        ? (object?)_positional ?? PyNone.Instance : (object?)_keywords ?? PyNone.Instance;

    internal void Set(string name, object? value)
    {
        if (name == "__defaults__") _positional = value as PyTuple;
        else _keywords = value as PyDict;
    }

    internal static void Validate(string name, object? value)
    {
        if (value is null or PyNone) return;
        // 子类布局的完整身份和槽协议另切；不将其剥成精确内建容器保存。
        if (name == "__kwdefaults__" && value is PyDictInstance or PyOrderedDict)
            throw new PyNotImplementedError("function keyword defaults with dict subclass storage is not supported");
        if (name == "__defaults__" ? value is not PyTuple : value is not PyDict)
            throw new PyTypeError(name + (name == "__defaults__"
                ? " must be set to a tuple object" : " must be set to a dict object"));
    }

    internal int RequiredPositionalCount => Math.Max(0, _positionalCount - (_positional?.Length ?? 0));

    // 公开字典可经任意别名原地增删/覆盖；Version只追踪结构，不能将它用于值缓存。
    // binder仅在真正缺省的槽查询，不为反射后的每次调用重建签名或分配默认值数组。
    internal bool TryDefault(int index, out object? value)
    {
        value = null;
        PyParamKind kind = _signature.Kinds[index];
        if (kind is PyParamKind.PositionalOnly or PyParamKind.PositionalOrKeyword)
        {
            // 位置参数在Python签名中连续位于最前；超长tuple使用最右侧的值。
            int offset = index + (_positional?.Length ?? 0) - _positionalCount;
            if (_positional is not null && offset >= 0)
            {
                value = _positional.Items[offset];
                return true;
            }
        }
        return kind == PyParamKind.KeywordOnly && _keywords is not null
            && _keywords.TryGet(_signature.Names[index], out value);
    }
}

public sealed partial class PyFunction
{
    internal object GetDefaultsMetadata(string name) => EnsureDefaultsMetadata().Get(name);

    internal void SetDefaultsMetadata(string name, object? value)
    {
        PyFunctionDefaults.Validate(name, value);
        EnsureDefaultsMetadata().Set(name, value);
    }

    private PyFunctionDefaults EnsureDefaultsMetadata()
    {
        if (PyFunctionDefaults.TryGet(Defaults, out PyFunctionDefaults? existing)) return existing!;
        var state = new PyFunctionDefaults(Signature, Defaults);
        // CLR空数组及外部传入数组可能被多个函数复用，冷状态必须以专属键隔离。
        var owned = new object?[Defaults.Length];
        Array.Copy(Defaults, owned, owned.Length);
        PyFunctionDefaults.Register(owned, state);
        Defaults = owned;
        return state;
    }
}
