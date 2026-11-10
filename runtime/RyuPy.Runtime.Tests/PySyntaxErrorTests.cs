// 新SyntaxError/ParseError真实父链、零/字符串消息域及所有公开模块写入口保护。
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PySyntaxErrorTests
{
    [Theory]
    [InlineData("SyntaxError")]
    [InlineData("ParseError")]
    public void MessageDomainHasRealArgsDefaultsAndTypeIdentity(string name)
    {
        var type = PyType.Get(name);
        foreach (object?[] args in new object?[][] { [], [""], ["message"] })
        {
            var error = Assert.IsAssignableFrom<PySyntaxError>(PyOps.CallValue(type, args, [], []));
            Assert.Equal(args, error.Args); Assert.Equal(args.Length == 0 ? "None" : args[0], PyRepr.Str(error));
            Assert.Equal(args.Length == 0 ? name + "()" : name + "(" + PyRepr.Repr(args[0]) + ")", PyRepr.Repr(error));
            Assert.Same(type, PyObject.TypeOf(error));
            Assert.Equal(args.Length == 0 ? PyNone.Instance : args[0], PyObject.GetAttr(error, "msg"));
            foreach (string field in new[] { "filename", "lineno", "offset", "text", "end_lineno", "end_offset" })
                Assert.Same(PyNone.Instance, PyObject.GetAttr(error, field));
            foreach (string field in new[] { "code", "position" }) Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(error, field));
            Assert.Equal(name == "ParseError" ? "xml.etree.ElementTree" : "builtins", PyObject.GetAttr(type, "__module__"));
            Assert.Equal(name == "ParseError" ? "<class 'xml.etree.ElementTree.ParseError'>" : "<class 'SyntaxError'>", PyRepr.Repr(type));
            Assert.Equal((name == "ParseError" ? "xml.etree.ElementTree." : "") + name
                + (args.Length == 1 && (string)args[0]! == "" ? "" : ": " + PyRepr.Str(error)), error.Render());
            foreach (string parent in new[] { name, "SyntaxError", "Exception", "BaseException" })
            {
                Assert.True(PyErrors.MatchDynamicException(error, PyType.Get(parent)));
                Assert.Equal(true, PyOps.CallBuiltin("isinstance", [error, PyType.Get(parent)], [], []));
                Assert.Equal(true, PyOps.CallBuiltin("issubclass", [type, PyType.Get(parent)], [], []));
            }
            Assert.False(PyErrors.MatchDynamicException(error, PyType.Get("ValueError")));
        }
        Assert.Empty(PyExceptionTypes.Create(name, "").Args); // raise类/内部空消息不能变成('',)。
    }

    [Fact]
    public void ClrCatchAndDynamicSelectorsUseActualNewFamilies()
    {
        var error = new PyXmlParseError("bad");
        try { throw error; } catch (PySyntaxError caught) { Assert.Same(error, caught); }
        Assert.True(typeof(PyException).IsAssignableFrom(typeof(PySyntaxError)));
        Assert.True(typeof(PySyntaxError).IsAssignableFrom(typeof(PyXmlParseError)));
        Assert.False(PyErrors.MatchDynamicException(new PySyntaxError("bad"), PyType.Get("ParseError")));
        Assert.Same(PyType.Get("ParseError"), PyOps.TypeObject("xml.etree.ElementTree.ParseError"));
    }

    [Fact]
    public void UnsupportedConstructionAndMutationNeverInvokeUserFormatting()
    {
        foreach (string name in new[] { "SyntaxError", "ParseError" })
        {
            var type = PyType.Get(name);
            foreach (object value in new object[] { PyNone.Instance, 7L, new PyList() })
                Assert.Equal(name + " message other than exact str is not supported", Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(type, [value], [], [])).PyMessage);
            Assert.Equal(name + " positional location details is not supported", Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(type, ["x", new PyTuple([])], [], [])).PyMessage);
            Assert.Equal(name + "() takes no keyword arguments", Assert.Throws<PyTypeError>(() => PyOps.CallValue(type, [], ["msg"], ["x"])).PyMessage);
            var error = PyOps.CallValue(type, ["x"], [], []);
            foreach (string field in new[] { "msg", "args", "filename" })
            {
                Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(error, field, "replacement"));
                Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(error, field));
            }
            Assert.Throws<PyNotImplementedError>(() => PyRuntimeBases.Validate(type));
            Assert.Throws<PyNotImplementedError>(() => PyOps.MakeClass("Child", [type], null));
        }
    }

    [Fact]
    public void ParseErrorRebindingIsRejectedBeforeEveryPublicModuleWrite()
    {
        PyNativeModules.Register(); var module = PyModules.Import(PyXmlElementTree.ModuleName);
        var original = module.GetAttr("ParseError"); Assert.Same(PyType.Get("ParseError"), original);
        foreach (Action<object?> set in new Action<object?>[] {
            value => module.SetAttr("ParseError", value), value => PyObject.SetAttr(module, "ParseError", value),
            value => PyOps.SetAttr(module, "ParseError", value), value => PyModules.SetDynamic(module, "ParseError", value) })
        {
            set(original); Assert.Same(original, module.GetAttr("ParseError"));
            Assert.Equal("rebinding xml.etree.ElementTree.ParseError is not supported", Assert.Throws<PyNotImplementedError>(() => set(PyType.Get("ValueError"))).PyMessage);
            Assert.Same(original, module.GetAttr("ParseError"));
            Assert.False(module.DynamicAttributes.ContainsKey("ParseError"));
        }
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(module, "ParseError"));
        Assert.Same(original, module.GetAttr("ParseError"));
        foreach (string name in new[] { "Comment", "ProcessingInstruction", "PI", "Element" })
        {
            var old = module.GetAttr(name);
            try { module.SetAttr(name, 7L); Assert.Equal(7L, module.GetAttr(name)); }
            finally { module.SetAttr(name, old); }
        }
    }
}
