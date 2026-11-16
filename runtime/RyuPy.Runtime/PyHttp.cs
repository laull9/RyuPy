// urllib.request 的直接 HTTP(S) 传输：BCL 处理协议/TLS，重定向与异常由 Python 合同控制。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;

namespace RyuPy.Runtime;

/// <summary>无反射、可裁剪、NativeAOT 可达的网络模块入口。</summary>
internal static class PyHttp
{
    /// <summary>urllib.request 的已实现入口。</summary>
    public static Dictionary<string, object?> RequestAttributes() => new(StringComparer.Ordinal)
    {
        ["Request"] = new PyNativeType("Request", "urllib.request", PyHttpRequest.Create, value => value is PyHttpRequest),
        ["urlopen"] = PyNativeFunctions.Create("urlopen", ["url", "data", "timeout", "cafile", "capath", "cadefault", "context"],
            [null, null, null, null, null, false, null], 1, Open,
            ["pos", "pos", "pos", "kwonly", "kwonly", "kwonly", "kwonly"]),
    };
    /// <summary>HTTP 客户端只公开真实响应类型与有限异常。</summary>
    public static Dictionary<string, object?> ClientAttributes() => new(StringComparer.Ordinal)
    {
        ["HTTPResponse"] = new PyNativeType("HTTPResponse", "http.client", null, value => value is PyHttpResponse),
        ["HTTPMessage"] = new PyNativeType("HTTPMessage", "http.client", null, value => value is PyHttpHeaders),
        ["HTTPException"] = PyType.Get("HTTPException"), ["InvalidURL"] = PyType.Get("InvalidURL"),
        ["IncompleteRead"] = PyType.Get("IncompleteRead"),
        ["OK"] = 200L, ["NOT_FOUND"] = 404L,
    };
    /// <summary>urllib.error 的真实异常身份。</summary>
    public static Dictionary<string, object?> ErrorAttributes() => new(StringComparer.Ordinal)
    { ["URLError"] = PyType.Get("URLError"), ["HTTPError"] = PyType.Get("HTTPError") };

    internal static CancellationTokenSource Cancellation(double? timeout)
    {
        var cancellation = new CancellationTokenSource();
        if (timeout is { } seconds) cancellation.CancelAfter(TimeSpan.FromSeconds(seconds));
        return cancellation;
    }
    private static double? Timeout(object? value)
    {
        if (PyUrlParse.None(value)) return null;
        double seconds = value switch { double number => number, long number => number, int number => number,
            bool flag => flag ? 1 : 0, _ => throw new PyTypeError("timeout must be a number") };
        if (double.IsNaN(seconds) || seconds < 0) throw new PyValueError("Timeout value out of range");
        if (!double.IsFinite(seconds) || seconds > uint.MaxValue / 1000.0) throw new PyOverflowError("timeout is too large");
        if (seconds == 0) throw new PyNotImplementedError("nonblocking HTTP timeout=0 is not implemented");
        return seconds;
    }
    private static object Open(object?[] args)
    {
        if (!PyUrlParse.None(args[3]) || !PyUrlParse.None(args[4]) || PyOps.Truthy(args[5]))
            throw new PyNotImplementedError("urlopen cafile/capath/cadefault are not implemented; pass an SSLContext");
        PySslContext? context = PyUrlParse.None(args[6]) ? null : args[6] as PySslContext
            ?? throw new PyTypeError("context must be an SSLContext");
        var request = args[0] as PyHttpRequest ?? new PyHttpRequest(PyUrlParse.Text(args[0]));
        if (!PyUrlParse.None(args[1])) request.SetData(args[1]);
        double? timeout = Timeout(args[2]);
        string url = request.Url, method = request.Method;
        byte[]? data = request.Data;
        var headers = request.TransportHeaders();
        var handler = new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false, UseProxy = false,
            AutomaticDecompression = DecompressionMethods.None };
        var client = new PyHttpClient(handler, context);
        bool transferred = false;
        var visited = new Dictionary<string, int>(StringComparer.Ordinal);
        try
        {
            for (int redirects = 0; ; redirects++)
            {
                Uri uri = TransportUri(url);
                using var message = new HttpRequestMessage(new HttpMethod(method), uri)
                { Version = HttpVersion.Version11, VersionPolicy = HttpVersionPolicy.RequestVersionExact };
                if (data is not null)
                {
                    message.Content = new ByteArrayContent(data);
                    message.Content.Headers.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
                }
                message.Headers.TryAddWithoutValidation("User-Agent", "Python-urllib/3.12");
                foreach (var header in headers)
                {
                    // Content-Length 由实际 bytes 计算，拒绝伪造长度和分块头，防止请求边界不一致。
                    if (header.Key.Equals("Content-length", StringComparison.OrdinalIgnoreCase)
                        || header.Key.Equals("Transfer-encoding", StringComparison.OrdinalIgnoreCase))
                        throw new PyNotImplementedError("manual Content-Length/Transfer-Encoding is not supported");
                    message.Headers.Remove(header.Key);
                    if (!message.Headers.TryAddWithoutValidation(header.Key, header.Value))
                    {
                        message.Content ??= new ByteArrayContent([]);
                        message.Content.Headers.Remove(header.Key);
                        if (!message.Content.Headers.TryAddWithoutValidation(header.Key, header.Value)) throw new PyValueError("invalid HTTP header");
                    }
                }
                using var cancellation = Cancellation(timeout);
                HttpResponseMessage response = client.Send(message, HttpCompletionOption.ResponseHeadersRead, cancellation.Token);
                int status = (int)response.StatusCode;
                string? location = response.Headers.Location?.OriginalString;
                if (status is 301 or 302 or 303 or 307 or 308 && location is not null)
                {
                    string next = PyUrlParse.Join(url, location.Replace(" ", "%20"));
                    int repeats = visited.GetValueOrDefault(next);
                    if (redirects >= 10 || repeats >= 4 || method is not ("GET" or "HEAD") && !(method == "POST" && status is 301 or 302 or 303))
                    {
                        var failure = new PyHttpResponse(response, client, url, timeout); transferred = true; throw new PyHTTPError(failure);
                    }
                    try
                    {
                        Uri destination = TransportUri(next);
                        if (uri.Scheme == "https" && destination.Scheme != "https") throw new PyURLError("HTTPS downgrade redirect refused");
                        if (!uri.GetLeftPart(UriPartial.Authority).Equals(destination.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
                            foreach (string key in headers.Keys.ToArray())
                                if (key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) || key.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
                                    || key.Equals("Proxy-authorization", StringComparison.OrdinalIgnoreCase) || key.Equals("Host", StringComparison.OrdinalIgnoreCase)) headers.Remove(key);
                        foreach (var entry in request.Unredirected.Entries()) headers.Remove(PyUrlParse.Text(entry.Key));
                        if (method != "HEAD" && status is 301 or 302 or 303)
                        { method = "GET"; data = null; foreach (string key in headers.Keys.ToArray()) if (key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase)) headers.Remove(key); }
                        visited[next] = repeats + 1; url = next;
                    }
                    finally { response.Dispose(); }
                    continue;
                }
                var result = new PyHttpResponse(response, client, url, timeout); transferred = true;
                if (status >= 400 || status is >= 300 and < 400 && status != 304) throw new PyHTTPError(result);
                if (status == 304) throw new PyHTTPError(result);
                return result;
            }
        }
        catch (OperationCanceledException)
        {
            if (client.Connecting || client.ConnectTimedOut) throw new PyURLError(new PyTimeoutError("timed out"));
            throw new PyTimeoutError("timed out");
        }
        catch (HttpRequestException error)
        {
            if (error.HttpRequestError == HttpRequestError.SecureConnectionError || error.InnerException is AuthenticationException)
                throw new PyURLError(client.CertificateRejected
                    ? new PySSLCertVerificationError("certificate verify failed") : new PySSLError("TLS handshake failed"));
            throw new PyURLError(new PyOSError(error.HttpRequestError == HttpRequestError.NameResolutionError
                ? "name resolution failed" : "connection failed"));
        }
        catch (System.IO.IOException) { throw new PyURLError(new PyOSError("connection failed")); }
        catch (UriFormatException) { throw new PyValueError("invalid URL"); }
        catch (ArgumentException) { throw new PyValueError("invalid HTTP request"); }
        finally
        {
            if (!transferred) client.Dispose();
        }
    }
    private static Uri TransportUri(string url)
    {
        string scheme = PyUrlParse.Split(url).Scheme;
        if (scheme is not ("http" or "https")) throw new PyURLError("unknown url type: " + scheme);
        if (url.Any(character => character > 127)) throw new PyUnicodeEncodeError("HTTP URL must be ASCII; quote non-ASCII paths");
        if (url.Any(character => character <= 32 || character == 127)) throw new PyInvalidURL("URL can't contain control characters");
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Host.Length == 0) throw new PyURLError("no host given");
        if (uri.UserInfo.Length > 0) throw new PyNotImplementedError("URL credentials are not implemented; use an explicit Authorization header");
        // 禁止 System.Uri 折叠请求路径的点段或改变百分号转义；fragment 从来不进入请求目标。
        int fragment = url.IndexOf('#');
        return new Uri(fragment < 0 ? url : url[..fragment], new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = true });
    }
}
