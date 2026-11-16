// 有限真实 OrderedDict：独立原生身份、raw 插入序与公开双向链各司其职；普通 PyDict 不加字段。
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
namespace RyuPy.Runtime;

internal sealed class PyOrderedDict : PyNativeObject
{
    internal static readonly PyNativeType Type = BuildType();
    internal readonly PyDict Raw = new();
    private readonly Dictionary<object, Node> _nodes = new(PyKeyComparer.Instance);
    internal Node? First, Last;
    internal long OrderVersion { get; private set; }
    internal int Count => Raw.Count;
    [ThreadStatic] private static HashSet<PyOrderedDict>? _repr;
    internal sealed class Node(object key)
    {
        internal readonly object Key = key;
        internal Node? Previous, Next;
    }
    static PyOrderedDict() { }
    private static PyNativeType BuildType()
    {
        var type = new PyNativeType("OrderedDict", "collections", Create, value => value is PyOrderedDict, "dict");
        type.SetAttr("__class__", PyType.Get("type"));
        type.SetAttr("__base__", PyType.Get("dict"));
        type.SetAttr("__bases__", new PyTuple([PyType.Get("dict")]));
        type.SetAttr("__mro__", new PyTuple([type, PyType.Get("dict"), PyBaseObject.Type]));
        foreach (string name in PyOrderedDictMethods.Names)
            type.SetAttr(name, name == "__hash__" ? PyNone.Instance : PyOrderedDictMethods.Unbound(name));
        return type;
    }
    private static object Create(object?[] args, string[] names, object?[] values)
    {
        var result = new PyOrderedDict();
        PyOps.OrderedDictUpdate(result, args, names, values, true);
        return result;
    }
    public override string TypeName => "OrderedDict";
    public override object? GetAttr(string name)
    {
        if (name == "__class__") return Type;
        if (name == "__hash__") return PyNone.Instance;
        if (name is "__dict__" or "__reduce__" or "__reduce_ex__" or "__sizeof__") throw Unsupported(name);
        return PyOrderedDictMethods.Has(name) ? PyOrderedDictMethods.Bind(this, name) : throw Missing(name);
    }
    public override void SetAttr(string name, object? value) => throw Unsupported("instance attribute assignment");
    public override void DelAttr(string name) => throw Unsupported("instance attribute deletion");

    private static bool ScalarKey(object? key) => key is null or PyNone or string or bool or int or long or BigInteger or double;
    internal static void RequireKey(object? key)
    {
        if (ScalarKey(key)) return;
        if (key is PyInstance instance) PyHash.RequireOrdinaryHashable(instance);
        if (!PyHash.IsHashable(key)) throw Error(new PyTypeError($"unhashable type: '{PyRepr.TypeName(key)}'"));
        throw Unsupported("non-scalar keys");
    }
    internal object? Get(object? key)
    {
        RequireKey(key);
        if (Raw.TryGet(key ?? PyNone.Instance, out object? value)) return value;
        throw Error(PyKeyError.ForKey(key));
    }
    internal object? GetOrDefault(object? key, object? fallback)
    { RequireKey(key); return Raw.GetOrDefault(key ?? PyNone.Instance, fallback); }
    internal bool Contains(object? key)
    { RequireKey(key); return Raw.Contains(key ?? PyNone.Instance); }
    internal void Set(object? key, object? value)
    {
        RequireKey(key); object actual = key ?? PyNone.Instance;
        // 全部键不执行用户 hash/eq，因此两个索引之间没有可重入窗口；覆盖保原 key 身份。
        if (_nodes.ContainsKey(actual)) { Raw.Set(actual, value); return; }
        var node = new Node(actual);
        Raw.Set(actual, value); _nodes.Add(actual, node);
        Append(node); OrderVersion++;
    }
    internal void Delete(object? key)
    {
        RequireKey(key); object actual = key ?? PyNone.Instance;
        if (!_nodes.TryGetValue(actual, out Node? node)) throw Error(PyKeyError.ForKey(key));
        Raw.Delete(actual); _nodes.Remove(actual); Unlink(node); OrderVersion++;
    }
    internal object? Pop(object? key, bool hasDefault, object? fallback)
    {
        // OD 与普通 dict 不同：空 pop 仍验证键，不能先返回 default。
        RequireKey(key);
        if (!Raw.TryGet(key ?? PyNone.Instance, out object? value))
        { if (hasDefault) return fallback; throw Error(PyKeyError.ForKey(key)); }
        Delete(key); return value;
    }
    internal object? SetDefault(object? key, object? fallback)
    {
        RequireKey(key);
        if (Raw.TryGet(key ?? PyNone.Instance, out object? value)) return value;
        Set(key, fallback); return fallback;
    }
    internal void Clear()
    {
        if (Count == 0) return;
        Raw.Clear(); _nodes.Clear(); First = Last = null; OrderVersion++;
    }
    internal void Move(object? key, object? last)
    {
        bool tail = PyOps.Truthy(last);
        // truth 可以修改容器；空表先 KeyError，非空才进入 hashable/scalar 检查。
        if (Count == 0)
        {
            // ForKey会格式化key；仅标量及明确空的精确builtin保证不执行用户repr。
            // 不递归探测任意容器，也不因空表绕过本切的未知键边界；truth始终先完成。
            bool safeEmpty = key is PyList { Count: 0 } or PyDict { Count: 0 }
                or PySet { Count: 0 } or PyByteArray { Count: 0 };
            if (!ScalarKey(key) && !safeEmpty) throw Unsupported("non-scalar keys in empty move_to_end");
            throw Error(MissingMoveKey(key));
        }
        RequireKey(key);
        if (!_nodes.TryGetValue(key ?? PyNone.Instance, out Node? node)) throw Error(MissingMoveKey(key));
        if (ReferenceEquals(node, tail ? Last : First)) return;
        Unlink(node);
        if (tail) Append(node);
        else { node.Next = First; node.Previous = null; if (First is not null) First.Previous = node; First = node; Last ??= node; }
        OrderVersion++;
    }
    // CP的move缺None沿专用错误路径产生空args；不改变getitem/pop的(None,)合同。
    private static PyKeyError MissingMoveKey(object? key) => key is null or PyNone
        ? (PyKeyError)PyExceptionConstructors.Create("KeyError", [], []) : PyKeyError.ForKey(key);
    internal PyTuple PopItem(object? last)
    {
        bool tail = PyOps.Truthy(last);
        Node? node = tail ? Last : First;
        if (node is null) throw Error(PyKeyError.ForKey("dictionary is empty"));
        object? value = Raw.Get(node.Key); Delete(node.Key); return new PyTuple([node.Key, value]);
    }
    private void Append(Node node)
    {
        node.Previous = Last; node.Next = null;
        if (Last is not null) Last.Next = node; else First = node;
        Last = node;
    }
    private void Unlink(Node node)
    {
        if (node.Previous is null) First = node.Next; else node.Previous.Next = node.Next;
        if (node.Next is null) Last = node.Previous; else node.Next.Previous = node.Previous;
        node.Previous = node.Next = null;
    }
    internal IEnumerable<(object? Key, object? Value)> Entries()
    {
        var cursor = new PyOrderedDictIterator(this, PyDictViewKind.Keys);
        while (cursor.MoveNext())
        { object? key = cursor.Current(); yield return (key, Raw.Get(key)); }
    }
    internal PyDict PublicCopy()
    {
        var result = new PyDict();
        foreach (var (key, value) in Entries()) result.Set(key, value);
        return result;
    }
    internal PyOrderedDict Copy()
    {
        var result = new PyOrderedDict();
        foreach (var (key, value) in Entries()) result.Set(key, value);
        return result;
    }
    public override string PythonRepr()
    {
        if (Count == 0) return "OrderedDict()";
        _repr ??= new();
        if (!_repr.Add(this)) return "...";
        try
        {
            var text = new StringBuilder("OrderedDict({"); bool first = true;
            foreach (var (key, value) in Entries())
            {
                if (!first) text.Append(", "); first = false;
                text.Append(PyRepr.Repr(key)).Append(": ").Append(PyRepr.Repr(value));
            }
            return text.Append("})").ToString();
        }
        finally { _repr.Remove(this); }
    }
    internal static T Error<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
    internal static PyNotImplementedError Unsupported(string operation) =>
        Error(new PyNotImplementedError($"OrderedDict {operation} is not supported"));
}
