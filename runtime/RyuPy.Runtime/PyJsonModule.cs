using System;
using System.Collections.Generic;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>JSON公开入口：复用Python签名绑定与流协议，明确拒绝自定义编码器类而不忽略参数。</summary>
public static class PyJsonModule
{
    /// <summary>注册有限原生JSON API；异常导出由原生模块注册表保护为只读。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new()
    {
        ["loads"] = Decoder("loads", false), ["load"] = Decoder("load", true),
        ["dumps"] = Encoder("dumps", false), ["dump"] = Encoder("dump", true),
        ["JSONDecodeError"] = PyType.Get("JSONDecodeError"),
    };

    /// <summary>真实JSONDecodeError构造，避免以普通Exception伪造类型名造成typed catch漏捕。</summary>
    public static object ConstructError(object?[] args, string[] names, object?[] values) =>
        ErrorConstructor.Invoke(args, names, values)!;

    private static readonly PyFunction ErrorConstructor = PyNativeFunctions.Create("JSONDecodeError",
        ["msg", "doc", "pos"], [null, null, null], 3, args =>
        {
            string message = args[0] as string ?? throw new PyTypeError("msg must be str");
            string doc = args[1] as string ?? throw new PyTypeError("doc must be str");
            System.Numerics.BigInteger position = PyInt.ToBigInteger(args[2]!);
            if (position < long.MinValue || position > long.MaxValue) throw new PyOverflowError("position does not fit an index");
            long pos = (long)position;
            if (pos < 0 || pos > PyText.Length(doc))
                throw new PyNotImplementedError("JSONDecodeError positions outside the document are not supported");
            return new PyJSONDecodeError(message, doc, PyText.OffsetOf(doc, pos));
        });

    private static PyFunction Decoder(string name, bool stream)
    {
        string[] names = [stream ? "fp" : "s", "cls", "object_hook", "parse_float", "parse_int", "parse_constant", "object_pairs_hook", "strict"];
        return PyNativeFunctions.Create(name, names,
            [null, PyNone.Instance, PyNone.Instance, PyNone.Instance, PyNone.Instance, PyNone.Instance, PyNone.Instance, true], 1,
            args =>
            {
                RejectClass(args[1]);
                object? source = stream ? PyOps.CallValue(PyObject.GetAttr(args[0], "read"), [], [], []) : args[0];
                string text = DecodeSource(source);
                return new PyJsonParser(text, args[2], args[6], args[4], args[3], args[5], Flag(args[7])).Parse();
            }, ["pos", "kwonly", "kwonly", "kwonly", "kwonly", "kwonly", "kwonly", "kwonly"]);
    }

    private static PyFunction Encoder(string name, bool stream)
    {
        var names = new List<string> { "obj" };
        var defaults = new List<object?> { null };
        var kinds = new List<string> { "pos" };
        if (stream) { names.Add("fp"); defaults.Add(null); kinds.Add("pos"); }
        names.AddRange(["skipkeys", "ensure_ascii", "check_circular", "allow_nan", "cls", "indent", "separators", "default", "sort_keys"]);
        defaults.AddRange([false, true, true, true, PyNone.Instance, PyNone.Instance, PyNone.Instance, PyNone.Instance, false]);
        for (int i = 0; i < 9; i++) kinds.Add("kwonly");
        return PyNativeFunctions.Create(name, names.ToArray(), defaults.ToArray(), stream ? 2 : 1, args =>
        {
            int at = stream ? 2 : 1; RejectClass(args[at + 4]);
            var writer = new PyJsonWriter(Flag(args[at + 1]), Flag(args[at]),
                Flag(args[at + 2]), Flag(args[at + 3]), Flag(args[at + 8]),
                args[at + 5], args[at + 6], args[at + 7]);
            if (!stream) return writer.Write(args[0]);
            writer.WriteTo(args[0], args[1]);
            return PyNone.Instance;
        }, kinds.ToArray());
    }

    private static bool Flag(object? value)
    {
        // 自定义真值协议的调用次数/短路顺序依赖CPython编码快路，首批不静默提前执行它。
        if (value is null or PyNone or bool or int or long or System.Numerics.BigInteger)
            return PyOps.Truthy(value);
        throw new PyNotImplementedError("JSON flags currently require bool, int or None");
    }

    private static void RejectClass(object? cls)
    {
        if (cls is not null and not PyNone)
            throw new PyNotImplementedError("custom JSON encoder/decoder classes are not supported");
    }

    private static string DecodeSource(object? source)
    {
        if (source is string text) return text;
        byte[] bytes = source switch
        {
            PyBytes value => value.Bytes,
            PyByteArray value => value.Bytes.ToArray(),
            _ => throw new PyTypeError($"the JSON object must be str, bytes or bytearray, not {PyRepr.TypeName(source)}"),
        };
        Encoding encoding = new UTF8Encoding(false, true); int skip = 0;
        if (bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 254 && bytes[3] == 255)
        { encoding = new UTF32Encoding(true, false, true); skip = 4; }
        else if (bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 254 && bytes[2] == 0 && bytes[3] == 0)
        { encoding = new UTF32Encoding(false, false, true); skip = 4; }
        else if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191) skip = 3;
        else if (bytes.Length >= 2 && bytes[0] == 254 && bytes[1] == 255)
        { encoding = new UnicodeEncoding(true, false, true); skip = 2; }
        else if (bytes.Length >= 2 && bytes[0] == 255 && bytes[1] == 254)
        { encoding = new UnicodeEncoding(false, false, true); skip = 2; }
        else if (bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 0) encoding = new UTF32Encoding(true, false, true);
        else if (bytes.Length >= 4 && bytes[1] == 0 && bytes[2] == 0) encoding = new UTF32Encoding(false, false, true);
        else if (bytes.Length >= 2 && bytes[0] == 0) encoding = new UnicodeEncoding(true, false, true);
        else if (bytes.Length >= 2 && bytes[1] == 0) encoding = new UnicodeEncoding(false, false, true);
        try { return encoding.GetString(bytes, skip, bytes.Length - skip); }
        catch (DecoderFallbackException) { throw new PyUnicodeDecodeError("invalid encoded JSON document"); }
    }
}
