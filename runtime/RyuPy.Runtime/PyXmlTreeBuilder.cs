// 默认C加速TreeBuilder的有界事件状态机：节点身份/字典别名/文本尾文本/错误恢复均保留。
// 不借XML DOM，不支持任意factory、用户真值或任意文本/映射协议。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal sealed class PyXmlTreeBuilder : PyNativeObject
{
    internal static readonly PyNativeType Type = new("TreeBuilder", PyXmlElementTree.ModuleName, Create,
        value => value is PyXmlTreeBuilder);
    private static PyFunction? defaultComment, defaultPi;
    private readonly PyFunction commentFactory, piFactory;
    private readonly bool copyAttributes, insertComments, insertPis;
    private readonly List<PyXmlElement> stack = [];
    private PyXmlElement? root, current, last, tailTarget;
    private string? pending;
    private List<string>? fragments;

    // 只在实际native登记时捕获；另一次CreateAttributes绝不能覆盖已登记工厂的globals。
    internal static void RegisterFactories(Dictionary<string, object?> globals)
    {
        defaultComment = (PyFunction)globals["Comment"]!;
        defaultPi = (PyFunction)globals["ProcessingInstruction"]!;
    }
    private PyXmlTreeBuilder(bool copy, bool comments, bool pis, PyFunction comment, PyFunction pi)
    {
        copyAttributes = copy; insertComments = comments; insertPis = pis;
        commentFactory = comment; piFactory = pi;
    }
    private static object Create(object?[] args, string[] names, object?[] values)
    {
        // C签名绑定先完成，再消费有限factory/flag协议，不把域外用户对象静默强转。
        if (args.Length > 1) throw PyXmlTreeBuilderErrors.Type($"TreeBuilder() takes at most 1 positional argument ({args.Length} given)");
        if (args.Length + names.Length > 5) throw PyXmlTreeBuilderErrors.Type($"TreeBuilder() takes at most 5 arguments ({args.Length + names.Length} given)");
        string[] parameters = ["element_factory", "comment_factory", "pi_factory", "insert_comments", "insert_pis"];
        object?[] bound = [PyNone.Instance, PyNone.Instance, PyNone.Instance, false, false];
        var assigned = new bool[5];
        if (args.Length == 1) { bound[0] = args[0]; assigned[0] = true; }
        for (int i = 0; i < names.Length; i++)
        {
            int index = Array.IndexOf(parameters, names[i]);
            if (index < 0) continue;
            if (assigned[index]) throw PyXmlTreeBuilderErrors.Type($"argument for TreeBuilder() given by name ('{names[i]}') and position ({index + 1})");
            assigned[index] = true; bound[index] = values[i];
        }
        foreach (string name in names)
            if (Array.IndexOf(parameters, name) < 0) throw PyXmlTreeBuilderErrors.Type($"'{name}' is an invalid keyword argument for TreeBuilder()");
        bool copy = ReferenceEquals(bound[0], PyXmlElementTree.ElementType);
        if (bound[0] is not (null or PyNone) && !copy)
            throw PyXmlTreeBuilderErrors.Unsupported("TreeBuilder custom element_factory");
        // 无真实模块登记的宿主调用也不得自行构造另一份默认globals。
        if (defaultComment is null || defaultPi is null)
            throw PyXmlTreeBuilderErrors.Unsupported("TreeBuilder before native module registration");
        if (bound[1] is not (null or PyNone) && !ReferenceEquals(bound[1], defaultComment))
            throw PyXmlTreeBuilderErrors.Unsupported("TreeBuilder custom comment_factory");
        if (bound[2] is not (null or PyNone) && !ReferenceEquals(bound[2], defaultPi))
            throw PyXmlTreeBuilderErrors.Unsupported("TreeBuilder custom pi_factory");
        if (bound[3] is not bool comments) throw PyXmlTreeBuilderErrors.Unsupported("TreeBuilder insert_comments other than bool");
        if (bound[4] is not bool pis) throw PyXmlTreeBuilderErrors.Unsupported("TreeBuilder insert_pis other than bool");
        return new PyXmlTreeBuilder(copy, comments, pis, defaultComment, defaultPi);
    }

    public override string TypeName => "TreeBuilder";
    public override string PythonRepr() => throw PyXmlTreeBuilderErrors.Unsupported("TreeBuilder representation");
    public override object? GetAttr(string name) => name switch
    {
        "__class__" => Type,
        "start" or "end" or "data" or "comment" or "pi" or "close" =>
            PyXmlElementTree.Raw(name, (a, n, _) => Method(name, a, n)),
        "__repr__" or "__str__" or "__format__" or "__getstate__" or "__setstate__" or "__reduce__" or "__reduce_ex__"
            => throw PyXmlTreeBuilderErrors.Unsupported("TreeBuilder." + name),
        _ => throw MissingAttribute(name),
    };
    public override void SetAttr(string name, object? value) => throw WriteAttribute(name);
    public override void DelAttr(string name) => throw WriteAttribute(name);
    private static PyAttributeError WriteAttribute(string name) => name is "start" or "end" or "data" or "comment" or "pi" or "close"
        ? PyXmlTreeBuilderErrors.Attribute($"'xml.etree.ElementTree.TreeBuilder' object attribute '{name}' is read-only") : MissingAttribute(name);
    private static PyAttributeError MissingAttribute(string name) => PyXmlTreeBuilderErrors.Attribute(
        $"'xml.etree.ElementTree.TreeBuilder' object has no attribute '{name}'");

    private object? Method(string name, object?[] args, string[] names)
    {
        int minimum = name == "close" ? 0 : name == "start" ? 2 : 1;
        int maximum = name == "pi" ? 2 : minimum;
        if (names.Length != 0) throw PyXmlTreeBuilderErrors.Type($"TreeBuilder.{name}() takes no keyword arguments");
        if (args.Length < minimum || args.Length > maximum)
        {
            string message = name == "close" ? $"TreeBuilder.close() takes no arguments ({args.Length} given)"
                : name is "data" or "end" or "comment" ? $"TreeBuilder.{name}() takes exactly one argument ({args.Length} given)"
                : minimum == maximum ? $"{name} expected {minimum} arguments, got {args.Length}"
                : $"pi expected {(args.Length < minimum ? "at least 1" : "at most 2")} arguments, got {args.Length}";
            throw PyXmlTreeBuilderErrors.Type(message);
        }
        return name switch
        {
            "start" => Start(args[0], args[1]),
            "end" => End(), // C实现不比较传入tag，也不调用用户相等协议。
            "data" => Data(args[0]),
            "comment" => InsertSpecial(commentFactory, args, insertComments),
            "pi" => InsertSpecial(piFactory, args, insertPis),
            _ => root, // close不flush、不验证栈、不销毁builder。
        };
    }

    private PyXmlElement Start(object? tag, object? attributes)
    {
        Flush();
        if (attributes is not PyDict dictionary || attributes is PyOrderedDict or PyDictInstance)
            throw PyXmlTreeBuilderErrors.Unsupported("TreeBuilder attrib other than exact dict");
        foreach (object? key in dictionary.Keys)
            if (key is not string) throw PyXmlTreeBuilderErrors.Unsupported("TreeBuilder attrib keys other than exact str");
        // 默认空dict在C节点中尚未物化；非空字典保别名。显式canonical Element仍浅复制。
        PyDict storage = dictionary;
        if (copyAttributes || dictionary.Count == 0)
        {
            storage = new PyDict();
            foreach (var (key, value) in dictionary.Entries()) storage.Set(key, value);
        }
        var node = new PyXmlElement(tag, storage);
        // 第二根错误前已flush，且清除上一个tail目标；不能把失败伪装成原子操作。
        tailTarget = null;
        if (current is not null) current.Append(node);
        else if (root is null) root = node;
        else throw PyXmlTreeBuilderErrors.Parse("multiple elements on top level");
        stack.Add(node); current = node; last = node;
        return node;
    }
    private PyXmlElement End()
    {
        Flush();
        if (stack.Count == 0) throw PyXmlTreeBuilderErrors.Index("pop from empty stack");
        var node = stack[^1]; stack.RemoveAt(stack.Count - 1);
        current = stack.Count == 0 ? null : stack[^1]; last = node; tailTarget = node;
        return node;
    }
    private object? Data(object? value)
    {
        if (last is null) return null;
        if (value is not string text) throw PyXmlTreeBuilderErrors.Unsupported("TreeBuilder data other than exact str");
        if (pending is null) pending = text;
        else (fragments ??= [pending]).Add(text);
        return null;
    }
    private void Flush()
    {
        if (pending is null || last is null) return;
        string text = fragments is null ? pending : string.Concat(fragments);
        PyXmlElement node = tailTarget ?? last;
        object? prior = tailTarget is null ? node.Text : node.Tail;
        if (prior is not (null or PyNone or string))
            throw PyXmlTreeBuilderErrors.Unsupported("TreeBuilder text/tail append other than exact str");
        if (prior is string prefix) text = prefix + text;
        if (tailTarget is null) node.Text = text; else node.Tail = text;
        pending = null; fragments = null;
    }
    private PyXmlElement InsertSpecial(PyFunction factory, object?[] args, bool insert)
    {
        Flush();
        // 保存的原函数读取它自己的定义globals；用户重绑异常绝不catch后重新Fresh。
        var node = (PyXmlElement)PyOps.CallValue(factory, args, [], [])!;
        if (insert && current is not null)
        {
            current.Append(node); tailTarget = node;
        }
        return node;
    }
}
