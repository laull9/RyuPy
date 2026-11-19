// urllib.parse 的字符串合同：解析保留原始 URL，传输层才使用 System.Uri。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>URL 字段、相对引用与查询入口；不支持的 bytes 结果族明确拒绝。</summary>
internal static class PyUrlParse
{
    internal static string Text(object? value) => value is string text ? text
        : throw new PyTypeError("URL argument must be str");
    internal static bool None(object? value) => value is null or PyNone;
    internal static PyFunction Function(string name, string[] names, object?[] defaults, int required,
        Func<object?[], object?> body) => PyNativeFunctions.Create(name, names, defaults, required, body);

    /// <summary>只登记已完成语义实现的 URL API。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new(StringComparer.Ordinal)
    {
        ["urlsplit"] = Function("urlsplit", ["url", "scheme", "allow_fragments"], [null, "", true], 1,
            args => Split(Text(args[0]), Text(args[1]), PyOps.Truthy(args[2]), false)),
        ["urlparse"] = Function("urlparse", ["url", "scheme", "allow_fragments"], [null, "", true], 1,
            args => Split(Text(args[0]), Text(args[1]), PyOps.Truthy(args[2]), true)),
        ["urljoin"] = Function("urljoin", ["base", "url", "allow_fragments"], [null, null, true], 2,
            args => Join(Text(args[0]), Text(args[1]), PyOps.Truthy(args[2]))),
        ["quote"] = Function("quote", ["string", "safe", "encoding", "errors"], [null, "/", null, null], 1,
            args => PyUrlCodec.Quote(args[0], args[1], args[2], args[3], false)),
        ["quote_plus"] = Function("quote_plus", ["string", "safe", "encoding", "errors"], [null, "", null, null], 1,
            args => PyUrlCodec.Quote(args[0], args[1], args[2], args[3], true)),
        ["unquote"] = Function("unquote", ["string", "encoding", "errors"], [null, "utf-8", "replace"], 1,
            args => PyUrlCodec.Unquote(args[0], args[1], args[2], false)),
        ["unquote_plus"] = Function("unquote_plus", ["string", "encoding", "errors"], [null, "utf-8", "replace"], 1,
            args => PyUrlCodec.Unquote(args[0], args[1], args[2], true)),
        ["urlencode"] = Function("urlencode", ["query", "doseq", "safe", "encoding", "errors", "quote_via"],
            [null, false, "", null, null, null], 1, PyUrlCodec.UrlEncode),
        ["parse_qs"] = Function("parse_qs", ["qs", "keep_blank_values", "strict_parsing", "encoding", "errors", "max_num_fields", "separator"],
            [null, false, false, "utf-8", "replace", null, "&"], 1, PyUrlCodec.ParseQuery),
        ["SplitResult"] = new PyNativeType("SplitResult", "urllib.parse", null,
            value => value is PyUrlResult { Parsed: false }, "tuple"),
        ["ParseResult"] = new PyNativeType("ParseResult", "urllib.parse", null,
            value => value is PyUrlResult { Parsed: true }, "tuple"),
    };

    internal static PyUrlResult Split(string url, string scheme = "", bool fragments = true, bool parse = false)
    {
        url = url.TrimStart(Enumerable.Range(0, 33).Select(value => (char)value).ToArray())
            .Replace("\t", "").Replace("\r", "").Replace("\n", "");
        string netloc = "", query = "", fragment = "", parameters = "";
        int colon = url.IndexOf(':');
        if (colon > 0 && char.IsAsciiLetter(url[0])
            && url[..colon].All(value => char.IsAsciiLetterOrDigit(value) || value is '+' or '-' or '.'))
        { scheme = url[..colon].ToLowerInvariant(); url = url[(colon + 1)..]; }
        if (url.StartsWith("//", StringComparison.Ordinal))
        {
            int end = url.IndexOfAny(['/', '?', '#'], 2);
            if (end < 0) end = url.Length;
            netloc = url[2..end]; url = url[end..];
            ValidateAuthority(netloc);
        }
        if (fragments) Cut(ref url, '#', out fragment);
        Cut(ref url, '?', out query);
        if (parse && UsesParams(scheme))
        {
            int semicolon = url.IndexOf(';', url.LastIndexOf('/') + 1);
            if (semicolon >= 0) { parameters = url[(semicolon + 1)..]; url = url[..semicolon]; }
        }
        return new PyUrlResult(scheme, netloc, url, parameters, query, fragment, parse);
    }

    private static void Cut(ref string text, char separator, out string suffix)
    {
        int position = text.IndexOf(separator); suffix = "";
        if (position >= 0) { suffix = text[(position + 1)..]; text = text[..position]; }
    }

    private static void ValidateAuthority(string authority)
    {
        string host = authority[(authority.LastIndexOf('@') + 1)..];
        int left = host.IndexOf('['), right = host.IndexOf(']');
        if ((left >= 0) != (right >= 0) || right >= 0 && right < left)
            throw new PyValueError("Invalid IPv6 URL");
        if (left >= 0)
        {
            if (left != 0 || right + 1 < host.Length && host[right + 1] != ':')
                throw new PyValueError("Invalid IPv6 URL");
            string address = host[(left + 1)..right];
            int dot = address.IndexOf('.');
            if (address.StartsWith('v') && (dot <= 1 || dot == address.Length - 1 || !address[1..dot].All(Uri.IsHexDigit)))
                throw new PyValueError("IPvFuture address is invalid");
            if (!address.StartsWith('v') && (!IPAddress.TryParse(address, out var parsed)
                || parsed.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6))
                throw new PyValueError("Invalid IPv6 URL");
        }
        // CPython 拒绝 NFKC 后变成 URL 分隔符的 authority，避免视觉混淆。
        string normalized = authority.Replace("@", "").Replace(":", "").Replace("?", "").Replace("#", "").Normalize(NormalizationForm.FormKC);
        if (normalized.IndexOfAny(['/', '?', '#', '@', ':']) >= 0)
            throw new PyValueError($"netloc '{authority}' contains invalid characters under NFKC normalization");
    }

    private static bool UsesParams(string scheme) => scheme is "" or "ftp" or "hdl" or "prospero" or "http" or "imap" or "https" or "shttp" or "rtsp" or "rtsps" or "rtspu" or "sip" or "sips" or "mms" or "sftp" or "tel";
    internal static bool UsesNetloc(string scheme) => scheme is "" or "ftp" or "http" or "https" or "file" or "ws" or "wss" or "nntp" or "gopher" or "telnet" or "imap" or "wais" or "shttp" or "snews" or "prospero" or "rtsp" or "rtsps" or "rtspu" or "rsync" or "svn" or "svn+ssh" or "sftp" or "nfs" or "git" or "git+ssh";

    internal static string Join(string basis, string relative, bool fragments = true)
    {
        if (basis.Length == 0) return relative;
        if (relative.Length == 0) return basis;
        var parent = Split(basis, fragments: fragments, parse: true);
        var child = Split(relative, parent.Scheme, fragments, true);
        if (child.Scheme != parent.Scheme || child.Scheme is not ("" or "ftp" or "http" or "gopher" or "nntp"
            or "imap" or "wais" or "file" or "https" or "shttp" or "mms" or "prospero" or "rtsp"
            or "rtsps" or "rtspu" or "sftp" or "svn" or "svn+ssh" or "ws" or "wss")) return relative;
        if (child.Netloc.Length != 0) return child.Url();
        if (child.Path.Length == 0 && child.Parameters.Length == 0)
            return new PyUrlResult(child.Scheme, parent.Netloc, parent.Path, parent.Parameters,
                child.Query.Length == 0 ? parent.Query : child.Query, child.Fragment, true).Url();
        string path = child.Path;
        if (!path.StartsWith('/'))
        {
            int slash = parent.Path.LastIndexOf('/');
            path = (slash >= 0 ? parent.Path[..(slash + 1)] : parent.Netloc.Length != 0 ? "/" : "") + path;
            // 合并相对路径时 CPython 折叠内部空段，保留首尾空段。
            var pieces = path.Split('/').ToList();
            for (int index = pieces.Count - 2; index > 0; index--) if (pieces[index] == "") pieces.RemoveAt(index);
            path = string.Join('/', pieces);
        }
        var resolved = new List<string>();
        string[] segments = path.Split('/');
        foreach (string segment in segments)
        {
            if (segment == "..") { if (resolved.Count > 0) resolved.RemoveAt(resolved.Count - 1); }
            else if (segment != ".") resolved.Add(segment);
        }
        if (segments[^1] is "." or "..") resolved.Add("");
        path = string.Join('/', resolved);
        if (path.Length == 0) path = "/";
        return new PyUrlResult(child.Scheme, parent.Netloc, path, child.Parameters, child.Query, child.Fragment, true).Url();
    }
}

/// <summary>真实 tuple 载体的 URL 结果，支持字段访问、索引、解包与 geturl。</summary>
internal sealed class PyUrlResult : PyTuple
{
    internal string Scheme { get; }
    internal string Netloc { get; }
    internal string Path { get; }
    internal string Parameters { get; }
    internal string Query { get; }
    internal string Fragment { get; }
    internal bool Parsed { get; }
    internal PyUrlResult(string scheme, string netloc, string path, string parameters, string query, string fragment, bool parsed)
        : base(parsed ? [scheme, netloc, path, parameters, query, fragment] : [scheme, netloc, path, query, fragment])
    { Scheme = scheme; Netloc = netloc; Path = path; Parameters = parameters; Query = query; Fragment = fragment; Parsed = parsed; }

    internal string Url()
    {
        string path = Path + (Parameters.Length > 0 ? ";" + Parameters : "");
        if (Netloc.Length > 0 || Scheme.Length > 0 && PyUrlParse.UsesNetloc(Scheme) && !path.StartsWith("//", StringComparison.Ordinal))
            path = "//" + Netloc + (path.Length > 0 && !path.StartsWith('/') ? "/" : "") + path;
        return (Scheme.Length > 0 ? Scheme + ":" : "") + path
            + (Query.Length > 0 ? "?" + Query : "") + (Fragment.Length > 0 ? "#" + Fragment : "");
    }

    internal object? Attribute(string name) => name switch
    {
        "scheme" => Scheme, "netloc" => Netloc, "path" => Path,
        "params" when Parsed => Parameters, "query" => Query, "fragment" => Fragment,
        "username" => UserInfo(false), "password" => UserInfo(true), "hostname" => Hostname(), "port" => Port(),
        "geturl" => PyNativeFunctions.Create("geturl", [], [], 0, _ => Url()),
        "__class__" => PyNativeType.LookupByName(Parsed ? "ParseResult" : "SplitResult"),
        "count" or "index" or "__len__" => PyOps.PrepareMethod(this, name),
        _ => throw new PyAttributeError($"'{(Parsed ? "ParseResult" : "SplitResult")}' object has no attribute '{name}'"),
    };

    internal string Repr()
    {
        string[] names = Parsed ? ["scheme", "netloc", "path", "params", "query", "fragment"] : ["scheme", "netloc", "path", "query", "fragment"];
        return (Parsed ? "ParseResult" : "SplitResult") + "(" + string.Join(", ", names.Select((name, index) => name + "=" + PyRepr.Repr(Items[index]))) + ")";
    }

    private object? UserInfo(bool password)
    {
        int at = Netloc.LastIndexOf('@'); if (at < 0) return PyNone.Instance;
        string info = Netloc[..at]; int colon = info.IndexOf(':');
        return password ? colon >= 0 ? info[(colon + 1)..] : PyNone.Instance : colon < 0 ? info : info[..colon];
    }
    private string Host() => Netloc[(Netloc.LastIndexOf('@') + 1)..];
    private object Hostname()
    {
        string host = Host(); host = host.StartsWith('[') ? host[1..host.IndexOf(']')] : host.Split(':')[0];
        if (host.Length == 0) return PyNone.Instance;
        int zone = host.IndexOf('%');
        return zone < 0 ? host.ToLowerInvariant() : host[..zone].ToLowerInvariant() + host[zone..];
    }
    private object Port()
    {
        string host = Host(); int colon = host.StartsWith('[') ? host.IndexOf(':', host.IndexOf(']')) : host.IndexOf(':');
        if (colon < 0 || colon == host.Length - 1) return PyNone.Instance;
        string port = host[(colon + 1)..];
        if (!port.All(char.IsAsciiDigit) || !long.TryParse(port, out long number))
            throw new PyValueError($"Port could not be cast to integer value as '{port}'");
        if (number > 65535) throw new PyValueError("Port out of range 0-65535");
        return number;
    }
}
