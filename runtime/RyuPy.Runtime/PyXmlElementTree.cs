// ElementTree 首切注册与构造：真实模块/类型/原值，不解析、序列化或借用单父 XML DOM。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>有限 ElementTree 模块；类型单例不随属性字典重建而覆盖短名注册身份。</summary>
public static class PyXmlElementTree
{
    internal const string ModuleName = "xml.etree.ElementTree";
    /// <summary>真实 Element 类型，节点类型不跟随模块公开名字重绑定。</summary>
    public static PyNativeType ElementType { get; } = new("Element", ModuleName,
        (args, names, values) => CreateElement(args, names, values, false), value => value is PyXmlElement);
    /// <summary>真实树包装类型，保存根引用而不判断根的真值。</summary>
    public static PyNativeType TreeType { get; } = new("ElementTree", ModuleName, CreateTree,
        value => value is PyXmlTree);
    /// <summary>QName 类型由独立有限对象实现。</summary>
    public static PyNativeType QNameType { get; } = new("QName", ModuleName, PyXmlQName.Create,
        value => value is PyXmlQName);

    /// <summary>创建工厂的定义全局字典；旧函数永远读取这份字典，不按模块名重新导入。</summary>
    public static Dictionary<string, object?> CreateAttributes()
    {
        var globals = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Element"] = ElementType, ["ElementTree"] = TreeType, ["QName"] = QNameType,
            ["SubElement"] = Raw("SubElement", (a, n, v) => CreateElement(a, n, v, true)),
            ["HTML_EMPTY"] = new PySet(new object?[] { "area", "base", "basefont", "br", "col", "embed",
                "frame", "hr", "img", "input", "isindex", "link", "meta", "param", "source", "track", "wbr" }),
        };
        globals["Comment"] = PyNativeFunctions.Create("Comment", ["text"], [PyNone.Instance], 0,
            a => Factory(globals, "Comment", a[0]));
        globals["ProcessingInstruction"] = PyNativeFunctions.Create("ProcessingInstruction", ["target", "text"],
            [null, PyNone.Instance], 1, a =>
            {
                // 官方 Python 工厂先建节点并写 target，再消费 text 的真值/拼接协议。
                var node = Factory(globals, "ProcessingInstruction", a[0]);
                if (PyOps.Truthy(a[1]))
                {
                    if (a[1] is not string || a[0] is not string)
                        throw Unsupported("ProcessingInstruction non-string concatenation");
                    node.Text = (string)a[0]! + " " + (string)a[1]!;
                }
                return node;
            });
        globals["PI"] = globals["ProcessingInstruction"];
        return globals;
    }

    /// <summary>QName 的 Python 全局重绑会改变其方法的 isinstance；有限类型不能接受后静默忽略。</summary>
    public static void BeforeSet(string name, object? value)
    {
        if (name == "QName" && !ReferenceEquals(value, QNameType))
            throw Unsupported("rebinding xml.etree.ElementTree.QName");
    }

    private static PyXmlElement Factory(Dictionary<string, object?> globals, string name, object? text)
    {
        // 自定义 Element 工厂需要单独验证回调/返回值合同，不偷偷退回 canonical 类型。
        if (!globals.TryGetValue("Element", out object? element) || !ReferenceEquals(element, ElementType))
            throw Unsupported(name + " with a rebound Element factory");
        if (!globals.TryGetValue(name, out object? tag))
            throw Own(new PyNameError("name '" + name + "' is not defined"));
        return new PyXmlElement(tag, new PyDict()) { Text = text };
    }

    private static object CreateElement(object?[] args, string[] names, object?[] values, bool sub)
    {
        string name = sub ? "SubElement" : "Element";
        int required = sub ? 2 : 1;
        if (args.Length < required) throw Own(new PyTypeError($"{name}() takes at least {required} argument{(required == 1 ? "" : "s")} ({args.Length} given)"));
        if (args.Length > required + 1) throw Own(new PyTypeError($"{name}() takes at most {required + 1} arguments ({args.Length} given)"));
        PyXmlElement? parent = sub ? RequireElement(args[0], "SubElement() argument 1") : null;
        object? source = null;
        int consumed = -1;
        if (args.Length > required) source = args[required];
        else
            for (int i = 0; i < names.Length; i++)
                if (names[i] == "attrib") { source = values[i]; consumed = i; break; }
        PyDict attributes;
        if (args.Length > required || consumed >= 0)
        {
            string prefix = consumed >= 0 ? "attrib must be dict, not " : $"{name}() argument {required + 1} must be dict, not ";
            attributes = RequireAttributes(source, prefix, positional: consumed < 0);
        }
        else attributes = new PyDict();
        // 先完成形状验证再复制，未知用户 key 不能在拒绝之前触发 hash/eq。
        GuardKeys(attributes);
        var copy = new PyDict();
        foreach (var (key, value) in attributes.Entries()) copy.Set(key, value);
        for (int i = 0; i < names.Length; i++) if (i != consumed) copy.Set(names[i], values[i]);
        var result = new PyXmlElement(args[required - 1], copy);
        parent?.Append(result);
        return result;
    }

    private static object? CreateTree(object?[] args, string[] names, object?[] values) =>
        InvokeInitializer("ElementTree", TreeConstructor, 1, args, names, values);
    private static readonly PyFunction TreeConstructor = PyNativeFunctions.Create("ElementTree.__init__",
        ["self", "element", "file"], [null, PyNone.Instance, PyNone.Instance], 1, a =>
        {
            // file 的解析仅在真值分支消费；默认 None/空字符串不触发 parser。
            if (PyOps.Truthy(a[2])) throw Unsupported("ElementTree file parsing");
            return new PyXmlTree(a[1]);
        });

    internal static PyXmlElement RequireElement(object? value, string prefix) => value is PyXmlElement element
        ? element : throw Own(new PyTypeError(prefix + " must be xml.etree.ElementTree.Element, not " + (value is null or PyNone ? "None" : PyRepr.TypeName(value))));
    internal static PyDict RequireAttributes(object? value, string prefix = "attrib must be dict, not ", bool positional = false)
    {
        if (value is PyOrderedDict) throw PyOrderedDict.Unsupported("Element attrib storage");
        if (value is PyDictInstance) throw Unsupported("Element attrib dict subclasses");
        return value is PyDict dictionary ? dictionary : throw Own(new PyTypeError(prefix
            + (positional && value is null or PyNone ? "None" : PyRepr.TypeName(value))));
    }
    internal static void GuardKeys(PyDict dictionary)
    {
        foreach (object? key in dictionary.Keys)
            if (key is not string) throw Unsupported("Element attrib keys other than exact str");
    }
    internal static string Key(object? value) => value is string text ? text
        : throw Unsupported("Element attribute keys other than exact str");
    // 此处两个 Python initializer 都恰有最多三参（含 self）；保通用绑定器的错误
    // 优先级，仅改正已经确认的过量位置实参范围文案，不改整个 runtime binder。
    internal static object? InvokeInitializer(string name, PyFunction constructor, int minimum,
        object?[] args, string[] names, object?[] values)
    {
        try { return PyOps.CallValue(constructor, WithSelf(PyNone.Instance, args), names, values); }
        catch (PyTypeError error) when (args.Length > 2 && error.PyMessage ==
            $"{name}.__init__() takes 3 positional arguments but {args.Length + 1} were given")
        {
            throw Own(new PyTypeError($"{name}.__init__() takes from {minimum} to 3 positional arguments but {args.Length + 1} were given"));
        }
    }

    // Python 定义的类方法仍把 self 算进绑定错误；不能拿已去 self 的 native 签名冒充。
    internal static object?[] WithSelf(object? self, object?[] args)
    {
        var actual = new object?[args.Length + 1]; actual[0] = self;
        Array.Copy(args, 0, actual, 1, args.Length); return actual;
    }
    internal static PyFunction BoundPython(string name, object self, PyFunction function) =>
        Raw(name, (a, n, v) => PyOps.CallValue(function, WithSelf(self, a), n, v));
    // C slot wrapper 的位置数量和关键字错误不经过 Python 函数绑定器。
    internal static PyFunction Wrapper(string name, int count, Func<object?[], object?> body) => Raw(name, (a, n, _) =>
    {
        if (n.Length != 0) throw Own(new PyTypeError($"wrapper {name}() takes no keyword arguments"));
        if (a.Length != count) throw Own(new PyTypeError($"expected {count} argument{(count == 1 ? "" : "s")}, got {a.Length}"));
        return body(a);
    });
    internal static PyFunction Raw(string name, Func<object?[], string[], object?[], object?> body) =>
        new(name, PySignature.Create(name, [], [], []), (_, a, n, v) => body(a, n, v) ?? PyNone.Instance, [], false);
    internal static PyNotImplementedError Unsupported(string area) => Own(new PyNotImplementedError(area + " is not supported"));
    internal static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}

/// <summary>ElementTree 根引用包装，不用空壳冒充 parser/TreeBuilder 状态机。</summary>
public sealed class PyXmlTree : PyNativeObject
{
    private readonly object? _root;
    private static readonly PyFunction GetRoot = PyNativeFunctions.Create("ElementTree.getroot", ["self"], [null], 1,
        a => ((PyXmlTree)a[0]!)._root);
    internal PyXmlTree(object? root) => _root = root;
    /// <inheritdoc />
    public override string TypeName => "ElementTree";
    /// <inheritdoc />
    public override string PythonRepr() => throw PyXmlElementTree.Unsupported("ElementTree representation");
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "__class__" => PyXmlElementTree.TreeType,
        "getroot" => PyXmlElementTree.BoundPython("ElementTree.getroot", this, GetRoot),
        "__repr__" or "__str__" or "__format__" or "_setroot" or "parse" or "write" or "find" or "findall" or "findtext" or "iter" or "iterfind"
            => throw PyXmlElementTree.Unsupported("ElementTree." + name),
        _ => throw Missing(name),
    };
}
