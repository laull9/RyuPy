// Python 网络异常的真实 CLR 层次及属性；传输失败在边界转换，禁止泄漏 CLR 异常。
using System;

namespace RyuPy.Runtime;

/// <summary>网络等待超时。</summary>
internal sealed class PyTimeoutError : PyOSError
{
    /// <summary>构造可被 TimeoutError/OSError 捕获的异常。</summary>
    public PyTimeoutError(string message) : base("TimeoutError", message) { }
}
/// <summary>urllib 的 URL/连接错误。</summary>
internal class PyURLError : PyOSError
{
    /// <summary>Python 原因对象。</summary>
    public object? Reason { get; }
    /// <summary>保留原始原因参数。</summary>
    public PyURLError(object? reason) : this("URLError", reason) { }
    /// <summary>HTTPError 复用的有限异常构造。</summary>
    protected PyURLError(string name, object? reason) : base(name, "", [reason]) => Reason = reason;
    /// <inheritdoc/>
    public override string PyMessage => $"<urlopen error {PyRepr.Str(Reason)}>";
    internal virtual object? Attribute(string name) => name == "reason" ? Reason
        : throw new PyAttributeError($"'{PyTypeName}' object has no attribute '{name}'");
}
/// <summary>HTTP 失败状态仍可读取响应体，close 与 with 交给同一个响应实例。</summary>
internal sealed class PyHTTPError : PyURLError
{
    private readonly string url;
    private readonly long status;
    private readonly object? headers;
    private readonly PyHttpResponse? response;
    /// <summary>从真实 HTTP 响应构造。</summary>
    internal PyHTTPError(PyHttpResponse response) : this(response.Url, response.Status, response.Reason, response.Headers, response) { }
    internal PyHTTPError(string url, long status, object? reason, object? headers, PyHttpResponse? response)
        : base("HTTPError", reason)
    {
        this.url = url; this.status = status; this.headers = headers; this.response = response;
        InitializePositionalArguments([]);
    }
    /// <inheritdoc/>
    public override string PyMessage => $"HTTP Error {status}: {PyRepr.Str(Reason)}";
    internal override object? Attribute(string name) => name switch
    {
        "code" or "status" => status, "url" or "filename" => url, "reason" or "msg" => Reason,
        "headers" or "hdrs" => headers, "fp" => (object?)response ?? PyNone.Instance,
        "__enter__" => PyNativeFunctions.Create(name, [], [], 0, _ => this),
        _ when response is not null => response.GetAttr(name),
        "close" => PyNativeFunctions.Create(name, [], [], 0, _ => null),
        "__exit__" => PyNativeFunctions.Create(name, ["type", "value", "traceback"], [null, null, null], 3, _ => false),
        _ => base.Attribute(name),
    };
}
/// <summary>SSL 层失败属于 OSError。</summary>
internal class PySSLError : PyOSError
{
    /// <summary>构造 SSL 错误。</summary>
    public PySSLError(string message) : this("SSLError", message) { }
    /// <summary>证书子类指定真实类型。</summary>
    protected PySSLError(string name, string message) : base(name, message) { }
}
/// <summary>证书链或主机名验证失败。</summary>
internal sealed class PySSLCertVerificationError : PySSLError
{
    /// <summary>提供稳定 Python 原因而非平台 OpenSSL/Schannel 文本。</summary>
    public PySSLCertVerificationError(string message) : base("SSLCertVerificationError", message) { }
}
/// <summary>HTTP 协议层异常。</summary>
internal class PyHTTPException : PyException
{
    /// <summary>构造 HTTPException。</summary>
    public PyHTTPException(string message) : this("HTTPException", message) { }
    /// <summary>供有限协议子类使用。</summary>
    protected PyHTTPException(string name, string message) : base(name, message) { }
}
/// <summary>请求 URL 含非法字符。</summary>
internal sealed class PyInvalidURL : PyHTTPException
{
    /// <summary>构造 InvalidURL。</summary>
    public PyInvalidURL(string message) : base("InvalidURL", message) { }
}

/// <summary>已声明长度的响应提前结束，保留本次 read 得到的字节及缺少长度。</summary>
internal sealed class PyIncompleteRead : PyHTTPException
{
    internal PyBytes Partial { get; }
    internal long? Expected { get; }
    internal PyIncompleteRead(byte[] partial, long? expected) : base("IncompleteRead", "")
    {
        Partial = new PyBytes(partial); Expected = expected;
        InitializePositionalArguments([Partial]);
    }
    /// <inheritdoc/>
    public override string PyMessage => $"IncompleteRead({Partial.Bytes.Length} bytes read"
        + (Expected is { } count ? $", {count} more expected)" : ")");
}
