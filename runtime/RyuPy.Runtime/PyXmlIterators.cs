// XML children 的活索引游标与惰性前序遍历；不预复制树、不把共享 DAG 去重。
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>首次耗尽后保持结束；耗尽前能观察 append 的真实子节点迭代器。</summary>
internal sealed class PyXmlChildrenIterator : PyNativeObject, IPyIterator
{
    private readonly PyXmlElement _element;
    private int _cursor;
    private object? _current;
    internal PyXmlChildrenIterator(PyXmlElement element) => _element = element;
    public override string TypeName => "iterator";
    public override string PythonRepr() => throw PyXmlElementTree.Unsupported("Element iterator representation");
    public bool MoveNext()
    {
        if (_cursor < 0) return false;
        if (_cursor >= _element.Count) { _cursor = -1; _current = null; return false; }
        _current = _element.ChildAt(_cursor++); return true;
    }
    public object? Current() => _current;
    public override object? GetAttr(string name) => name switch
    {
        "__iter__" => PyNativeFunctions.Create("iterator.__iter__", [], [], 0, _ => this),
        "__next__" => PyNativeFunctions.Create("iterator.__next__", [], [], 0,
            _ => MoveNext() ? Current() : throw new PyStopIteration()),
        "__repr__" or "__str__" or "__format__" => throw PyXmlElementTree.Unsupported("Element iterator representation"),
        _ => throw Missing(name),
    };
}

/// <summary>显式栈的前序 Element.iter，不递归 CLR 调用栈；只在当前路径检查环。</summary>
internal sealed class PyXmlPreorderIterator : PyNativeObject, IPyIterator
{
    private readonly List<(PyXmlElement Node, int Next)> _stack = [];
    private readonly HashSet<PyXmlElement> _active = [];
    private readonly string? _filter;
    private PyXmlElement? _root;
    private object? _current;
    private bool _finished;
    internal PyXmlPreorderIterator(PyXmlElement root, string? filter) { _root = root; _filter = filter; }
    public override string TypeName => "_element_iterator";
    public override string PythonRepr() => throw PyXmlElementTree.Unsupported("Element iterator representation");
    public bool MoveNext()
    {
        if (_finished) return false;
        while (true)
        {
            PyXmlElement node;
            if (_root is { } root) { node = root; _root = null; }
            else
            {
                while (_stack.Count > 0 && _stack[^1].Next >= _stack[^1].Node.Count)
                {
                    _active.Remove(_stack[^1].Node); _stack.RemoveAt(_stack.Count - 1);
                }
                if (_stack.Count == 0) { _finished = true; _current = null; return false; }
                var top = _stack[^1];
                node = top.Node.ChildAt(top.Next);
                _stack[^1] = (top.Node, top.Next + 1);
            }
            // 环可以构造；在遍历真正遇到时明确拒绝，不在 append 偷改对象图。
            if (_active.Contains(node)) throw PyXmlElementTree.Unsupported("Element.iter cyclic graphs");
            if (_stack.Count >= 10000) throw PyXmlElementTree.Unsupported("Element.iter depth above 10000");
            _active.Add(node); _stack.Add((node, 0));
            if (_filter is not null)
            {
                if (PyObject.MayHaveMethods(node.Tag) && node.Tag is not PyXmlQName)
                    throw PyXmlElementTree.Unsupported("Element.iter user tag comparison");
                if (!PyHash.Equal(node.Tag, _filter)) continue;
            }
            _current = node; return true;
        }
    }
    public object? Current() => _current;
    public override object? GetAttr(string name) => name switch
    {
        "__iter__" => PyNativeFunctions.Create("_element_iterator.__iter__", [], [], 0, _ => this),
        "__next__" => PyNativeFunctions.Create("_element_iterator.__next__", [], [], 0,
            _ => MoveNext() ? Current() : throw new PyStopIteration()),
        "__repr__" or "__str__" or "__format__" => throw PyXmlElementTree.Unsupported("Element iterator representation"),
        _ => throw Missing(name),
    };
}
