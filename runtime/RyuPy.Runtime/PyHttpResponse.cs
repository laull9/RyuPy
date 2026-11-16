// HTTPResponse：按流读取字节，每次底层等待重置超时，显式 close/with 释放客户端和响应。
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace RyuPy.Runtime;

/// <summary>HttpClient 响应的 Python 资源所有者。</summary>
internal sealed class PyHttpResponse : PyNativeObject
{
    private readonly HttpResponseMessage response;
    private readonly HttpClient client;
    private readonly Stream stream;
    private readonly double? timeout;
    private bool closed;
    private bool eof;
    private long receivedTotal;
    internal string Url { get; }
    internal long Status => (long)response.StatusCode;
    internal string Reason => response.ReasonPhrase ?? "";
    internal PyHttpHeaders Headers { get; }
    /// <inheritdoc/>
    public override string TypeName => "HTTPResponse";
    internal PyHttpResponse(HttpResponseMessage response, HttpClient client, string url, double? timeout)
    {
        this.response = response; this.client = client; Url = url; this.timeout = timeout;
        try { stream = response.Content.ReadAsStream(); }
        catch { response.Dispose(); throw; }
        var headers = new List<(string, string)>();
        foreach (var header in response.Headers) foreach (string value in header.Value) headers.Add((header.Key, value));
        foreach (var header in response.Content.Headers) foreach (string value in header.Value) headers.Add((header.Key, value));
        Headers = new PyHttpHeaders(headers);
    }
    private object Read(object? size)
    {
        long count = PyUrlParse.None(size) ? -1 : PyIO.Index(size);
        if (closed || count == 0) return new PyBytes([]);
        if (eof)
        {
            if (count < 0 && response.RequestMessage?.Method != HttpMethod.Head
                && response.Content.Headers.ContentLength is { } remaining && receivedTotal < remaining)
                throw new PyIncompleteRead([], remaining - receivedTotal);
            return new PyBytes([]);
        }
        if (count > int.MaxValue) throw new PyOverflowError("read size is too large");
        using var output = new MemoryStream();
        var buffer = new byte[(int)(count < 0 ? 8192 : Math.Min(count, 8192))];
        try
        {
            while (count < 0 || output.Length < count)
            {
                using var cancellation = PyHttp.Cancellation(timeout);
                int length = count < 0 ? buffer.Length : (int)Math.Min(buffer.Length, count - output.Length);
                int received = stream.ReadAsync(buffer.AsMemory(0, length), cancellation.Token).GetAwaiter().GetResult();
                if (received == 0) { eof = true; break; }
                output.Write(buffer, 0, received); receivedTotal += received;
            }
            if (count < 0 && response.RequestMessage?.Method != HttpMethod.Head
                && response.Content.Headers.ContentLength is { } total && receivedTotal < total)
                throw new PyIncompleteRead(output.ToArray(), total - receivedTotal);
            return new PyBytes(output.ToArray());
        }
        catch (OperationCanceledException) { Close(); throw new PyTimeoutError("timed out"); }
        catch (HttpIOException error) when (error.HttpRequestError == HttpRequestError.ResponseEnded)
        {
            eof = true;
            // CPython 的限长 read 允许短读，read() 才按声明长度报告 IncompleteRead。
            if (count >= 0) return new PyBytes(output.ToArray());
            long? expected = response.Content.Headers.ContentLength is { } total ? total - receivedTotal : null;
            Close(); throw new PyIncompleteRead(output.ToArray(), expected);
        }
        catch (IOException) { Close(); throw new PyOSError("HTTP response read failed"); }
        catch (HttpRequestException) { Close(); throw new PyHTTPException("HTTP response read failed"); }
    }
    internal void Close()
    {
        if (closed) return;
        closed = true;
        try { stream.Dispose(); } finally { response.Dispose(); client.Dispose(); }
    }
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "status" or "code" => Status, "reason" or "msg" => Reason, "url" => Url,
        "headers" => Headers, "closed" => closed,
        "version" => response.Version.Major == 1 && response.Version.Minor == 0 ? 10L : 11L,
        "read" => PyNativeFunctions.Create(name, ["amt"], [null], 0, args => Read(args[0])),
        "close" => PyNativeFunctions.Create(name, [], [], 0, _ => { Close(); return null; }),
        "getcode" => PyNativeFunctions.Create(name, [], [], 0, _ => Status),
        "geturl" => PyNativeFunctions.Create(name, [], [], 0, _ => Url),
        "info" => PyNativeFunctions.Create(name, [], [], 0, _ => Headers),
        "getheader" => PyNativeFunctions.Create(name, ["name", "default"], [null, null], 1, args => Headers.Joined(PyUrlParse.Text(args[0]), args[1])),
        "getheaders" => Headers.GetAttr("items"),
        "__enter__" => PyNativeFunctions.Create(name, [], [], 0, _ => closed ? throw new PyValueError("I/O operation on closed file") : this),
        "__exit__" => PyNativeFunctions.Create(name, ["type", "value", "traceback"], [null, null, null], 3, _ => { Close(); return false; }),
        _ => throw Missing(name),
    };
}
