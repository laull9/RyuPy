// decode的四入口、真实编码、绑定拒绝及无隐式转换合同。
using Xunit;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyBinaryDecodeTests
{
    private static object? Call(int route, object self, object?[] args, object?[] names, object?[] values)
    {
        object owner = PyObject.TypeOf(self);
        return route switch
        {
            0 => PyOps.CallMethod(self, "decode", args, names, values),
            1 => PyOps.CallValue(PyObject.GetAttr(self, "decode"), args, names, values),
            2 => PyOps.CallMethod(owner, "decode", [self, .. args], names, values),
            _ => PyOps.CallValue(PyObject.GetAttr(owner, "decode"), [self, .. args], names, values),
        };
    }

    [Fact]
    public void AllRoutesBindKeywordsAndRejectInvalidArguments()
    {
        foreach (object self in new object[] { new PyBytes([0xc3, 0xa9]), new PyByteArray([0xc3, 0xa9]) })
            for (int route = 0; route < 4; route++)
            {
                Assert.Equal("é", Call(route, self, [], ["encoding", "errors"], ["utf-8", "strict"]));
                Assert.Equal("é", Call(route, self, ["utf_8", "strict"], [], []));
                Assert.Throws<PyTypeError>(() => Call(route, self, ["utf8"], ["encoding"], ["ascii"]));
                Assert.Throws<PyTypeError>(() => Call(route, self, [], ["encoding"], [PyNone.Instance]));
                Assert.Throws<PyTypeError>(() => Call(route, self, [], ["errors"], [1L]));
                Assert.Throws<PyTypeError>(() => Call(route, self, [], ["unknown"], ["utf8"]));
                Assert.Throws<PyTypeError>(() => Call(route, self, ["utf8", "strict", 1L], [], []));
                Assert.Throws<PyValueError>(() => Call(route, self, [], ["encoding"], ["utf8\0"]));
                Assert.Equal("é", Call(route, self, [], ["errors"], ["backslashreplace"]));
                Assert.Throws<PyNotImplementedError>(() => Call(route, self, ["ascii"], ["errors"], ["backslashreplace"]));
                Assert.Equal("é", Call(route, self, [], ["errors"], ["unknown-handler"]));
                Assert.Throws<PyLookupError>(() => Call(route, self, ["ascii"], ["errors"], ["unknown-handler"]));
                Assert.Throws<PyLookupError>(() => Call(route, self, [""], [], []));
                Assert.Throws<PyUnicodeDecodeError>(() => Call(route, self, ["ascii"], [], []));
            }
    }

    [Fact]
    public void Utf16HasRealBomEndianAndStrictSurrogateSemantics()
    {
        Assert.Equal("A😀", PyEncoding.Decode([0xff, 0xfe, 65, 0, 0x3d, 0xd8, 0, 0xde], "utf-16"));
        Assert.Equal("A😀", PyEncoding.Decode([0xfe, 0xff, 0, 65, 0xd8, 0x3d, 0xde, 0], "utf-16"));
        Assert.Equal("\ufeffA", PyEncoding.Decode([0xff, 0xfe, 65, 0], "utf-16-le"));
        Assert.Equal("A", PyEncoding.Decode([0, 65], "utf-16-be"));
        Assert.Equal("", PyEncoding.Decode([], "utf-16"));
        foreach (byte[] invalid in new byte[][] { [0xff], [0xff, 0xfe, 65], [0xff, 0xfe, 0, 0xd8], [0xfe, 0xff, 0xdc, 0] })
            Assert.Throws<PyUnicodeDecodeError>(() => PyEncoding.Decode(invalid, "utf-16"));
        Assert.Throws<PyUnicodeDecodeError>(() => PyEncoding.Decode([0xff], "utf-8"));
    }

    [Fact]
    public void ReplaceAndIgnoreUsePythonErrorUnitsOnAllRoutes()
    {
        foreach (object self in new object[] { new PyBytes([0xed, 0xa0, 0x80]), new PyByteArray([0xed, 0xa0, 0x80]) })
            for (int route = 0; route < 4; route++)
            {
                Assert.Equal("\ufffd\ufffd\ufffd", Call(route, self, ["utf-8", "replace"], [], []));
                Assert.Equal("", Call(route, self, ["ascii"], ["errors"], ["ignore"]));
                Assert.Equal("\ufffd\ufffd\ufffd", Call(route, self, ["ascii"], ["errors"], ["replace"]));
            }
        Assert.Equal("\ufffd", PyEncoding.Decode([0, 0xd8, 65], "utf-16-le", "replace"));
        Assert.Equal("\ufffd", PyEncoding.Decode([0xd8, 0, 65], "utf-16-be", "replace"));
        Assert.Equal("\ufffdA", PyEncoding.Decode([0, 0xd8, 65, 0], "utf-16-le", "replace"));
        Assert.Equal("", PyEncoding.Decode([0, 0xd8, 65], "utf-16-le", "ignore"));
        Assert.Equal("\ufffd", PyEncoding.Decode([0xe2, 0x82], "utf-8", "replace"));
        Assert.Equal("ÿ", PyEncoding.Decode([255], "latin-1", "unknown-handler"));
    }

    [Fact]
    public void EncodingAndErrorsDoNotCoerceUserObjects()
    {
        int calls = 0;
        var type = new PyClass("EncodingTrap");
        type.SetAttr("__str__", PyNativeFunctions.Create("__str__", ["self"], [null], 1,
            _ => { calls++; return "utf-8"; }));
        var trap = new PyInstance(type);
        for (int route = 0; route < 4; route++)
            foreach (string name in new[] { "encoding", "errors" })
                Assert.Throws<PyTypeError>(() => Call(route, new PyBytes([65]), [], [name], [trap]));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void RootChecksSelfAndSavedMethodKeepsMutableReceiver()
    {
        object root = PyObject.GetAttr(PyType.Get("bytes"), "decode")!;
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(root, [], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(root, [new PyByteArray([])], [], []));
        var array = new PyByteArray([65]);
        object saved = PyObject.GetAttr(array, "decode")!;
        array.Append(66L);
        Assert.Equal("AB", PyOps.CallValue(saved, [], ["encoding"], ["ascii"]));
    }
}
