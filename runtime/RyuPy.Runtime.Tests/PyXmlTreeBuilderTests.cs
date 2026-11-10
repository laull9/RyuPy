// TreeBuilder事件状态/别名/注册工厂的直接运行时门；产品差分与旧pending单独保留。
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyXmlTreeBuilderTests
{
    private static object? Call(object? f, object?[] args, string[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(f, args, names ?? [], values ?? []);
    private static object? Method(object obj, string name, params object?[] args) => Call(PyObject.GetAttr(obj, name), args);
    private static PyXmlTreeBuilder Builder(string[]? names = null, object?[]? values = null)
    {
        PyNativeModules.Register();
        return Assert.IsType<PyXmlTreeBuilder>(Call(PyXmlTreeBuilder.Type, [], names, values));
    }
    private static PyXmlElement Start(PyXmlTreeBuilder b, object tag, PyDict? attrs = null) =>
        Assert.IsType<PyXmlElement>(Method(b, "start", tag, attrs ?? new PyDict()));

    [Fact]
    public void TwoBuildersKeepOpaqueNodesTextTailAndStacksIndependent()
    {
        var a = Builder(); var b = Builder(); var tag = new PyList();
        Assert.Same(PyNone.Instance, Method(a, "data", new PyList()));
        var ar = Start(a, tag); var br = Start(b, "b");
        Assert.Same(tag, ar.Tag); ar.Text = "prefix";
        Method(a, "data", "A"); Method(b, "data", "B"); Method(a, "data", "C");
        var child = Start(a, "c"); Assert.Equal("prefixAC", ar.Text);
        Method(a, "data", "inner"); Assert.Same(br, Method(b, "end", new PyList()));
        Assert.Same(child, Method(a, "end", new PyList())); child.Tail = "tail:";
        Method(a, "data", "D"); Assert.Same(ar, Method(a, "end", "ignored"));
        Assert.Equal("inner", child.Text); Assert.Equal("tail:D", child.Tail); Assert.Equal("B", br.Text);
        Assert.Same(child, ar.ChildAt(0)); Assert.Same(ar, Method(a, "close")); Assert.Same(br, Method(b, "close"));
        Assert.Same(PyXmlTreeBuilder.Type, PyObject.TypeOf(a));
    }

    [Fact]
    public void DefaultAttributesAliasOnlyNonemptyAndCanonicalFactoryCopies()
    {
        foreach (bool explicitFactory in new[] { false, true })
        foreach (bool nonempty in new[] { false, true })
        {
            var builder = explicitFactory ? Builder(["element_factory"], [PyXmlElementTree.ElementType]) : Builder();
            var attrs = new PyDict(); var marker = new PyList(); if (nonempty) attrs.Set("a", marker);
            var node = Start(builder, "r", attrs);
            Assert.Equal(!explicitFactory && nonempty, ReferenceEquals(attrs, node.Attributes));
            if (nonempty) Assert.Same(marker, node.Attributes.Get("a"));
            attrs.Set("later", marker);
            Assert.Equal(!explicitFactory && nonempty, node.Attributes.TryGet("later", out _));
            var old = Assert.IsType<PyXmlElement>(Call(PyXmlElementTree.ElementType, ["old", attrs]));
            Assert.NotSame(attrs, old.Attributes);
        }
    }

    [Fact]
    public void CloseDoesNotFlushAndErrorsKeepTheirPrecedingStateChanges()
    {
        var b = Builder(); Assert.Same(PyNone.Instance, Method(b, "close"));
        Assert.Equal("pop from empty stack", Assert.Throws<PyIndexError>(() => Method(b, "end", "x")).PyMessage);
        var root = Start(b, "r"); Method(b, "data", "text");
        Assert.Same(root, Method(b, "close")); Assert.Same(PyNone.Instance, root.Text);
        Assert.Same(root, Method(b, "end", "not-r")); Assert.Equal("text", root.Text);
        Method(b, "data", "tail"); Assert.Same(root, Method(b, "close")); Assert.Same(PyNone.Instance, root.Tail);
        var error = Assert.Throws<PyXmlParseError>(() => Start(b, "second"));
        Assert.Equal("multiple elements on top level", error.PyMessage); Assert.Equal("tail", root.Tail);
        Assert.Same(root, Method(b, "close")); Method(b, "data", "more");
        Assert.Throws<PyIndexError>(() => Method(b, "end", "extra")); Assert.Equal("tail", root.Tail);
        // 冻结oracle未打印失败后的root.text；本门不将其补写为实观合同。
    }

    [Fact]
    public void CommentsAndPiFlushWithOptionalInsertionAndNeverBecomeRoot()
    {
        foreach (bool insert in new[] { false, true })
        {
            var b = Builder(["insert_comments", "insert_pis"], [insert, insert]);
            var before = Assert.IsType<PyXmlElement>(Method(b, "comment", "outside"));
            Assert.Same(PyNone.Instance, Method(b, "close")); var root = Start(b, "r");
            Method(b, "data", "A"); var comment = Assert.IsType<PyXmlElement>(Method(b, "comment", "note"));
            Method(b, "data", "B"); var pi = Assert.IsType<PyXmlElement>(Method(b, "pi", "target", "value"));
            Method(b, "data", "C"); Method(b, "end", "r");
            Assert.Equal(insert ? "A" : "ABC", root.Text); Assert.Equal(insert ? 2 : 0, root.Count);
            Assert.Equal("target value", pi.Text); Assert.Equal("note", comment.Text);
            if (insert) { Assert.Same(comment, root.ChildAt(0)); Assert.Same(pi, root.ChildAt(1)); Assert.Equal("B", comment.Tail); Assert.Equal("C", pi.Tail); }
            var after = Assert.IsType<PyXmlElement>(Method(b, "comment", "after"));
            Assert.Same(PyNone.Instance, before.Tail); Assert.Same(PyNone.Instance, after.Tail);
            Assert.Same(root, Method(b, "close")); Assert.Equal(insert ? 2 : 0, root.Count);
        }
    }

    [Fact]
    public void CapturedFactoriesKeepOriginalGlobalsAcrossAnotherAttributesCreation()
    {
        PyNativeModules.Register(); var module = PyModules.Import(PyXmlElementTree.ModuleName);
        object? oldComment = module.GetAttr("Comment"), oldPi = module.GetAttr("ProcessingInstruction");
        object? oldElement = module.GetAttr("Element");
        try
        {
            var b = Assert.IsType<PyXmlTreeBuilder>(Call(PyXmlTreeBuilder.Type, []));
            var second = PyXmlElementTree.CreateAttributes(); second["Comment"] = "other-global";
            module.SetAttr("Comment", "comment-tag"); module.SetAttr("ProcessingInstruction", "pi-tag");
            var marker = new PyList();
            var comment = Assert.IsType<PyXmlElement>(Method(b, "comment", marker));
            var pi = Assert.IsType<PyXmlElement>(Method(b, "pi", marker, false));
            Assert.Same(marker, comment.Text); Assert.Same(marker, pi.Text);
            Assert.Equal("comment-tag", comment.Tag); Assert.Equal("pi-tag", pi.Tag);
            module.SetAttr("Element", 7L); Assert.IsType<PyXmlElement>(Method(b, "start", "r", new PyDict()));
            Assert.Equal("Comment with a rebound Element factory is not supported", Assert.Throws<PyNotImplementedError>(() => Method(b, "comment", "x")).PyMessage);
        }
        finally { module.SetAttr("Comment", oldComment); module.SetAttr("ProcessingInstruction", oldPi); module.SetAttr("Element", oldElement); }
    }

    [Fact]
    public void BindingAndUnknownProtocolsFailAtTheirExplicitBoundary()
    {
        var b = Builder();
        Assert.Equal("TreeBuilder() takes at most 1 positional argument (2 given)", Assert.Throws<PyTypeError>(() => Call(PyXmlTreeBuilder.Type, [null, null])).PyMessage);
        Assert.Equal("'unknown' is an invalid keyword argument for TreeBuilder()", Assert.Throws<PyTypeError>(() => Call(PyXmlTreeBuilder.Type, [], ["unknown"], [true])).PyMessage);
        Assert.Equal("argument for TreeBuilder() given by name ('element_factory') and position (1)", Assert.Throws<PyTypeError>(() => Call(PyXmlTreeBuilder.Type, [null], ["element_factory"], [null])).PyMessage);
        foreach (string name in new[] { "start", "end", "data", "comment", "pi", "close" })
            Assert.Equal($"TreeBuilder.{name}() takes no keyword arguments", Assert.Throws<PyTypeError>(() => Call(b.GetAttr(name), [], ["x"], [1L])).PyMessage);
        foreach (string name in new[] { "element_factory", "comment_factory", "pi_factory" })
            Assert.Equal($"TreeBuilder custom {name} is not supported", Assert.Throws<PyNotImplementedError>(() => Call(PyXmlTreeBuilder.Type, [], [name], [7L])).PyMessage);
        foreach (string name in new[] { "insert_comments", "insert_pis" })
            Assert.Equal($"TreeBuilder {name} other than bool is not supported", Assert.Throws<PyNotImplementedError>(() => Call(PyXmlTreeBuilder.Type, [], [name], [1L])).PyMessage);
        Assert.Throws<PyNotImplementedError>(() => Method(b, "start", "r", new PyList()));
        var wrongKey = new PyDict(); wrongKey.Set(1L, true);
        Assert.Throws<PyNotImplementedError>(() => Method(b, "start", "r", wrongKey));
        var root = Start(b, "r"); Assert.Throws<PyNotImplementedError>(() => Method(b, "data", 1L));
        root.Text = 1L; Method(b, "data", "x"); Assert.Throws<PyNotImplementedError>(() => Method(b, "end", "r"));
        root.Text = "fixed"; Assert.Same(root, Method(b, "end", "r")); Assert.Equal("fixedx", root.Text);
        Assert.Throws<PyAttributeError>(() => b.GetAttr("missing")); Assert.Throws<PyAttributeError>(() => b.SetAttr("missing", null));
        Assert.Throws<PyAttributeError>(() => b.DelAttr("missing"));
    }
}
