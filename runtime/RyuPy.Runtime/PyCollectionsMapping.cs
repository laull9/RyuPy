using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>defaultdict/Counter 共享有序存储与普通映射方法；缺键策略只进入下标读取。</summary>
public abstract class PyCollectionsMapping : PyNativeObject, IPyNativeMapping
{
    /// <inheritdoc />
    public PyDict Dictionary { get; } = new();
    private static readonly object Absent = new();
    [ThreadStatic] private static HashSet<PyCollectionsMapping>? _repr;
    /// <summary>子类型定义的缺键行为。</summary>
    protected abstract object? MissingKey(object? key);
    /// <summary>保留子类型的浅复制。</summary>
    protected abstract PyCollectionsMapping Copy();
    /// <summary>有签名的普通映射方法。</summary>
    protected static PyFunction Method(string name, string[] parameters, object?[] defaults, int required, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, parameters, defaults, required, body, Array.ConvertAll(parameters, _ => "posonly"));
    /// <summary>读取已有键；缺键才进入 __missing__。</summary>
    protected object? Read(object? key) => Dictionary.TryGet(key, out object? result) ? result : MissingKey(key);
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "__getitem__" => Method(name, ["key"], [null], 1, a => Read(a[0])),
        "__missing__" => Method(name, ["key"], [null], 1, a => MissingKey(a[0])),
        "__setitem__" => Method(name, ["key", "value"], [null, null], 2, a => { Dictionary.Set(a[0], a[1]); return null; }),
        "__delitem__" => Method(name, ["key"], [null], 1, a => { Dictionary.Delete(a[0]); return null; }),
        "__len__" => Method(name, [], [], 0, _ => (long)Dictionary.Count),
        "__iter__" => Method(name, [], [], 0, _ => PyIter.Create(Dictionary)),
        "__contains__" => Method(name, ["key"], [null], 1, a => Dictionary.Contains(a[0])),
        "get" => Method(name, ["key", "default"], [null, PyNone.Instance], 1, a => Dictionary.GetOrDefault(a[0], a[1])),
        "setdefault" => Method(name, ["key", "default"], [null, PyNone.Instance], 1, a => Dictionary.SetDefault(a[0], a[1])),
        "pop" => Method(name, ["key", "default"], [null, Absent], 1, a => Dictionary.Pop(a[0], !ReferenceEquals(a[1], Absent), a[1])),
        "popitem" => Method(name, [], [], 0, _ => PopItem()),
        "clear" => Method(name, [], [], 0, _ => { Dictionary.Clear(); return null; }),
        "keys" => Method(name, [], [], 0, _ => new PyDictView(Dictionary, PyDictViewKind.Keys)),
        "values" => Method(name, [], [], 0, _ => new PyDictView(Dictionary, PyDictViewKind.Values)),
        "items" => Method(name, [], [], 0, _ => new PyDictView(Dictionary, PyDictViewKind.Items)),
        "copy" or "__copy__" => Method(name, [], [], 0, _ => Copy()),
        "__eq__" => Method(name, ["other"], [null], 1, a => MapEqual(a[0])),
        "__hash__" => PyNone.Instance,
        "update" => UpdateFunction(name, (source, keywords) => { if (!ReferenceEquals(source, Absent)) UpdateFrom(source); Dictionary.Update(keywords); }),
        _ => throw Missing(name),
    };
    /// <summary>update/subtract 共用一个位置参数与任意关键字绑定。</summary>
    protected PyFunction UpdateFunction(string name, Action<object?, PyDict> update) =>
        PyNativeFunctions.Create(name, ["iterable", "kwargs"], [Absent, null], 0,
            a => { update(a[0], (PyDict)a[1]!); return null; }, ["posonly", "varkw"]);
    /// <summary>判断 update 的位置来源是否被省略。</summary>
    protected static bool IsAbsent(object? value) => ReferenceEquals(value, Absent);
    /// <summary>逐项提交来源，不能先耗尽整个生成器再更新映射。</summary>
    internal void UpdateFrom(object? source)
    {
        if (source is PyOrderedDict) throw PyOrderedDict.Unsupported("defaultdict update");
        if (source is PyMappingProxy proxy) { PyMappingProxyConsumers.CopyInto(Dictionary, proxy); return; }
        // 默认 dict 派生的 iter 槽已受守卫，来源复制走根存储而非 keys 覆盖。
        if (source is PyDictInstance) { PyDictProtocols.CallUpdate(Dictionary, [source], [], []); return; }
        PyDict? mapping = source as PyDict ?? (source as IPyNativeMapping)?.Dictionary;
        if (mapping is not null)
        {
            foreach (var (key, value) in CheckedEntries(mapping)) Dictionary.Set(key, value);
            return;
        }
        RejectCustomMapping(source, "keys");
        IPyIterator iterator = PyIter.Create(source);
        while (iterator.MoveNext())
        {
            object?[] pair = PyOps.ToSequence(iterator.Current());
            if (pair.Length != 2) throw new PyValueError("dictionary update sequence element must have length 2");
            Dictionary.Set(pair[0], pair[1]);
        }
    }
    /// <summary>未知映射不能悄悄按键序列处理；本阶段只开放明确的原生存储接口。</summary>
    internal static void RejectCustomMapping(object? source, string member)
    {
        try { _ = PyObject.GetAttr(source, member); }
        catch (PyAttributeError) { return; }
        throw new PyNotImplementedError("custom mapping protocols are not supported by collections yet");
    }
    /// <summary>复用 ValueTuple 枚举，检查结构版本而不为每个条目分配 Python tuple。</summary>
    internal static IEnumerable<(object? Key, object? Value)> CheckedEntries(PyDict source)
    {
        long version = source.Version;
        foreach (var entry in source.Entries())
        {
            if (source.Version != version) throw new PyRuntimeError("dictionary changed size during iteration");
            yield return entry;
        }
        if (source.Version != version) throw new PyRuntimeError("dictionary changed size during iteration");
    }
    private object PopItem()
    {
        if (Dictionary.Count == 0) throw new PyKeyError("popitem(): dictionary is empty");
        object? key = null, value = null;
        foreach (var entry in Dictionary.Entries()) { key = entry.Key; value = entry.Value; }
        Dictionary.Delete(key);
        return new PyTuple([key, value]);
    }
    private object MapEqual(object? other)
    {
        using var recursion = PyCollectionsComparison.Enter();
        if (other is PyDictInstance) return PyDictProtocols.Compare("__eq__", Dictionary, other);
        PyDict? mapping = other as PyDict ?? (other as IPyNativeMapping)?.Dictionary;
        return mapping is null ? PyNotImplemented.Instance : PyHash.Equal(Dictionary, mapping);
    }
    /// <summary>递归安全地渲染映射内容；子类提供外围类型信息。</summary>
    protected string ReprMapping(Func<string, string> wrap) => GuardRepr(() => wrap(PyRepr.Repr(Dictionary)));
    /// <summary>渲染一次，避免预计算备用文本重复调用用户 repr。</summary>
    protected string GuardRepr(Func<string> render)
    {
        _repr ??= new();
        if (!_repr.Add(this)) return "...";
        try { return render(); }
        finally { _repr.Remove(this); }
    }
}
