// urllib.request.Request：独立的可写请求数据和 Python 风格头字段名规范化。
using System;
using System.Collections.Generic;
using System.Linq;

namespace RyuPy.Runtime;

/// <summary>HTTP(S) Request 的日常构造、头字段与方法选择。</summary>
internal sealed class PyHttpRequest : PyNativeObject
{
    private static readonly PyFunction Constructor = PyNativeFunctions.Create("Request",
        ["url", "data", "headers", "origin_req_host", "unverifiable", "method"], [null, null, null, null, false, null], 1,
        args => new PyHttpRequest(args));
    private string url;
    private object? data;
    private readonly string? method;
    private readonly PyDict headers = new();
    internal readonly PyDict Unredirected = new();
    internal string Url => url;
    internal byte[]? Data => data is PyBytes binary ? binary.Bytes : null;
    internal string Method => method ?? (PyUrlParse.None(data) ? "GET" : "POST");
    /// <inheritdoc/>
    public override string TypeName => "Request";
    internal static object? Create(object?[] args, string[] names, object?[] values) => PyOps.CallValue(Constructor, args, names, values);
    internal PyHttpRequest(string url) : this([url, null, null, null, false, null]) { }
    private PyHttpRequest(object?[] args)
    {
        url = PyUrlParse.Text(args[0]); ValidateUrl(url); SetData(args[1]);
        if (!PyUrlParse.None(args[3]) || PyOps.Truthy(args[4])) throw new PyNotImplementedError("origin_req_host and unverifiable are not implemented");
        method = PyUrlParse.None(args[5]) ? null : PyUrlParse.Text(args[5]);
        if (method is not null) ValidateToken(method);
        if (!PyUrlParse.None(args[2]))
        {
            if (args[2] is not PyDict mapping) throw new PyTypeError("headers must be a dict");
            foreach (var entry in mapping.Entries()) AddHeader(entry.Key, entry.Value, false);
        }
    }
    private static void ValidateUrl(string value)
    {
        if (value.Any(character => character < 32 || character == 127 || character == ' ')) throw new PyInvalidURL("URL can't contain control characters");
        if (PyUrlParse.Split(value).Scheme.Length == 0) throw new PyValueError($"unknown url type: {PyRepr.Repr(value)}");
    }
    internal static void ValidateToken(string token)
    {
        if (token.Length == 0 || token.Any(character => !char.IsAsciiLetterOrDigit(character) && !"!#$%&'*+-.^_`|~".Contains(character)))
            throw new PyValueError("invalid HTTP token");
    }
    private void AddHeader(object? key, object? value, bool unredirected)
    {
        string name = PyUrlParse.Text(key), text = PyUrlParse.Text(value); ValidateToken(name);
        if (text.Contains('\r') || text.Contains('\n') || text.Contains('\0')) throw new PyValueError("Invalid header value");
        (unredirected ? Unredirected : headers).Set(char.ToUpperInvariant(name[0]) + name[1..].ToLowerInvariant(), text);
    }
    internal void SetData(object? value)
    {
        if (!PyUrlParse.None(value) && value is not PyBytes) throw new PyTypeError("POST data should be bytes");
        if (!ReferenceEquals(data, value)) { headers.DeleteIfPresent("Content-length"); Unredirected.DeleteIfPresent("Content-length"); }
        data = value;
    }
    private Dictionary<string, string> AllHeaders()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var source in new[] { Unredirected, headers })
            foreach (var entry in source.Entries()) result[PyUrlParse.Text(entry.Key)] = PyUrlParse.Text(entry.Value);
        return result;
    }
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "full_url" => url, "data" => data, "type" => PyUrlParse.Split(url).Scheme,
        "host" => PyUrlParse.Split(url).Netloc,
        "selector" => Selector(),
        "headers" => headers, "unredirected_hdrs" => Unredirected,
        "get_method" => PyNativeFunctions.Create(name, [], [], 0, _ => Method),
        "get_full_url" => PyNativeFunctions.Create(name, [], [], 0, _ => url),
        "add_header" or "add_unredirected_header" => PyNativeFunctions.Create(name, ["key", "val"], [null, null], 2,
            args => { AddHeader(args[0], args[1], name == "add_unredirected_header"); return null; }),
        "has_header" => PyNativeFunctions.Create(name, ["header_name"], [null], 1,
            args => AllHeaders().ContainsKey(PyUrlParse.Text(args[0]))),
        "get_header" => PyNativeFunctions.Create(name, ["header_name", "default"], [null, null], 1,
            args => AllHeaders().TryGetValue(PyUrlParse.Text(args[0]), out string? found) ? found : args[1]),
        "remove_header" => PyNativeFunctions.Create(name, ["header_name"], [null], 1,
            args => { string key = PyUrlParse.Text(args[0]); headers.DeleteIfPresent(key); Unredirected.DeleteIfPresent(key); return null; }),
        "header_items" => PyNativeFunctions.Create(name, [], [], 0,
            _ => new PyList(AllHeaders().Select(pair => (object?)new PyTuple([pair.Key, pair.Value])))),
        _ => throw Missing(name),
    };
    internal Dictionary<string, string> TransportHeaders()
    {
        var result = AllHeaders();
        // headers 是可写的真实 dict；直接修改也必须经过传输边界校验。
        foreach (var entry in result)
        {
            ValidateToken(entry.Key);
            if (entry.Value.Any(character => character is '\r' or '\n' or '\0')) throw new PyValueError("Invalid header value");
        }
        return result;
    }
    private string Selector()
    { var split = PyUrlParse.Split(url); return split.Path + (split.Query.Length > 0 ? "?" + split.Query : ""); }
    /// <inheritdoc/>
    public override void SetAttr(string name, object? value)
    {
        if (name == "data") SetData(value);
        else if (name == "full_url") { string next = PyUrlParse.Text(value); ValidateUrl(next); url = next; }
        else throw Missing(name);
    }
}
