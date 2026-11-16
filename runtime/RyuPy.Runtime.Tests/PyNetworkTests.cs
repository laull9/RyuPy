// 网络模块的静态入口、Python 绑定与安全拒绝测试；真实 HTTP/TLS/AOT 差分另由 loopback harness 验收。
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyNetworkTests
{
    public PyNetworkTests() { PySystem.Initialize("network-tests.py", []); PyNativeModules.Register(); }
    private static object? Call(string module, string name, object?[] args, string[]? names = null, object?[]? values = null) =>
        PyOps.CallValue(PyModules.Import(module).GetAttr(name), args, names ?? [], values ?? []);

    [Fact]
    public void ResultMethodsPreserveTupleAndSavedMethodBinding()
    {
        object result = Call("urllib.parse", "urlsplit", ["HTTPS://User:pw@EXAMPLE.com:443/a?x=1#f"])!;
        Assert.IsAssignableFrom<PyTuple>(result);
        Assert.Equal("example.com", PyObject.GetAttr(result, "hostname"));
        Assert.Equal(443L, PyObject.GetAttr(result, "port"));
        Assert.Equal("https://User:pw@EXAMPLE.com:443/a?x=1#f", PyOps.CallMethod(result, "geturl", [], [], []));
        Assert.Equal(0L, PyOps.CallMethod(result, "index", ["https"], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallMethod(result, "geturl", [], ["extra"], [1L]));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(result, "params"));
    }

    [Theory]
    [InlineData("http://[v.foo]")]
    [InlineData("http://[v1.]")]
    [InlineData("http://prefix[::1]suffix")]
    [InlineData("http://[::1]suffix")]
    [InlineData("http://／host")]
    public void AuthorityValidationUsesPythonErrors(string url) =>
        Assert.Throws<PyValueError>(() => Call("urllib.parse", "urlsplit", [url]));

    [Fact]
    public void CodecErrorsAndUnsupportedResultFamiliesAreExplicit()
    {
        Assert.Equal("%3F", Call("urllib.parse", "quote", ["?", new PyBytes([255])]));
        Assert.Equal("中", Call("urllib.parse", "unquote", ["%E4%B8%AD"]));
        Assert.Throws<PyUnicodeDecodeError>(() => Call("urllib.parse", "unquote", ["%FF"], ["errors"], ["strict"]));
        Assert.Throws<PyTypeError>(() => Call("urllib.parse", "urlsplit", [new PyBytes([97])]));
        Assert.Throws<PyNotImplementedError>(() => Call("urllib.parse", "quote", ["x"], ["errors"], ["surrogatepass"]));
        Assert.Throws<PyNotImplementedError>(() => Call("urllib.parse", "urlencode", [new PyDict()], ["quote_via"], [1L]));
    }

    [Fact]
    public void RequestMutationsRetainRealHeaderDictionary()
    {
        object request = Call("urllib.request", "Request", ["http://localhost/a"])!;
        var headers = Assert.IsType<PyDict>(PyObject.GetAttr(request, "headers"));
        headers.Set("X-test", "mutated");
        Assert.Equal("mutated", PyOps.CallMethod(request, "get_header", ["X-test"], [], []));
        PyObject.SetAttr(request, "data", new PyBytes([97]));
        Assert.Equal("POST", PyOps.CallMethod(request, "get_method", [], [], []));
        Assert.Throws<PyTypeError>(() => PyObject.SetAttr(request, "data", "text"));
        Assert.Throws<PyValueError>(() => PyOps.CallMethod(request, "add_header", ["X-test", "a\r\nb"], [], []));
    }

    [Fact]
    public void UnsupportedTransportOptionsFailBeforeNetworkAccess()
    {
        Assert.Throws<PyNotImplementedError>(() => Call("urllib.request", "urlopen", ["http://localhost/", null, 0L]));
        Assert.Throws<PyNotImplementedError>(() => Call("urllib.request", "urlopen", ["http://localhost/"], ["cafile"], ["unused.pem"]));
        Assert.Throws<PyTypeError>(() => Call("urllib.request", "urlopen", ["http://localhost/"], ["context"], [true]));
        Assert.Throws<PyURLError>(() => Call("urllib.request", "urlopen", ["file:///tmp/not-read"]));
        Assert.Throws<PyNotImplementedError>(() => Call("urllib.request", "urlopen", ["http://user:pw@localhost/"]));
        object request = Call("urllib.request", "Request", ["http://localhost/"])!;
        Assert.IsType<PyDict>(PyObject.GetAttr(request, "headers")).Set("Content-Length", "9");
        Assert.Throws<PyNotImplementedError>(() => Call("urllib.request", "urlopen", [request]));
    }

    [Fact]
    public void DefaultSslContextCannotDisableVerification()
    {
        object context = Call("ssl", "create_default_context", [])!;
        Assert.Equal(true, PyObject.GetAttr(context, "check_hostname"));
        Assert.Equal(2L, PyObject.GetAttr(context, "verify_mode"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(context, "check_hostname", false));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(context, "verify_mode", 0L));
        Assert.Throws<PyNotImplementedError>(() => Call("ssl", "create_default_context", [], ["capath"], ["/tmp"]));
        Assert.Throws<PyTypeError>(() => PyOps.CallMethod(context, "load_verify_locations", [], [], []));
        Assert.Throws<PySSLError>(() => Call("ssl", "create_default_context", [], ["cadata"], ["not PEM"]));
    }

    [Fact]
    public void HttpErrorIsReadableExceptionWithRealCloseMethod()
    {
        object error = Call("urllib.error", "HTTPError", ["http://localhost/", 404L, "missing", null, null])!;
        Assert.IsAssignableFrom<PyURLError>(error);
        Assert.Equal("HTTP Error 404: missing", Assert.IsType<PyHTTPError>(error).PyMessage);
        Assert.Equal(0, Assert.IsType<PyTuple>(PyObject.GetAttr(error, "args")).Length);
        Assert.Same(error, PyOps.CallMethod(error, "__enter__", [], [], []));
        PyOps.CallMethod(error, "close", [], [], []);
        Assert.Equal(false, PyOps.CallMethod(error, "__exit__", [null, null, null], [], []));
    }
}
