// getopt 的有限真实解析器：独立于 optparse，保尾切片身份、声明顺序及长参数真值回调。
using System;
using System.Collections.Generic;
using System.Numerics;
namespace RyuPy.Runtime;

/// <summary>CPython 3.12 的普通 getopt；自定义输入协议明确拒绝，不伪装 GNU 扫描。</summary>
public static class PyGetopt
{
    private static readonly PySignature Signature = PySignature.Create("getopt",
        ["args", "shortopts", "longopts"], ["pos", "pos", "pos"], ["0", "0", "1"]);
    private static readonly PyFunction Function = new("getopt", Signature, Invoke, [null, null, new PyList()], false);

    /// <summary>两个异常导出共用真实类型；没有空模块或未实现的 GNU 成功占位。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new(StringComparer.Ordinal)
    {
        ["getopt"] = Function, ["GetoptError"] = PyType.Get("GetoptError"), ["error"] = PyType.Get("GetoptError"),
    };

    private static object Invoke(PyFunction self, object?[] args, string[] names, object?[] values)
    {
        object?[] bound;
        // 只接管自身冷绑定错误；解析与用户 __bool__ 不包在广泛 catch 内。
        try { bound = PyBinder.Bind(Signature, args, names, values, self.Defaults); }
        catch (PyBaseException error)
        {
            // 通用绑定器已决定错误优先级；仅校准此固定签名的两条冷错误文本。
            // 不检查/转换实参值，不改其它函数，不包住解析或用户回调。
            if (error is PyTypeError && args.Length == 0
                && error.PyMessage == "getopt() missing 2 required positional arguments: 'args', 'shortopts'")
                throw Own(new PyTypeError("getopt() missing 2 required positional arguments: 'args' and 'shortopts'"));
            if (error is PyTypeError && args.Length > 3
                && error.PyMessage == $"getopt() takes 3 positional arguments but {args.Length} were given")
                throw Own(new PyTypeError($"getopt() takes from 2 to 3 positional arguments but {args.Length} were given"));
            PyErrors.PrepareRaise(error);
            throw;
        }
        return Parse(bound[0], bound[1], bound[2]);
    }

    private static PyTuple Parse(object? source, object? shortopts, object? longopts)
    {
        PyList options = new();
        object?[] longs = LongOptions(longopts); // 即使 args 为空也先转换 longopts。
        if (source is PyNamedTupleInstance) throw Unsupported("namedtuple argument sequence");
        if (source is not (PyList or PyTuple)) throw Unsupported("custom argument sequence");
        object remaining = source;
        while (PyOps.Truthy(remaining))
        {
            string token = Token(PyOps.GetItem(remaining, 0L));
            if (!token.StartsWith("-", StringComparison.Ordinal) || token == "-") break;
            if (token == "--") { remaining = Tail(remaining); break; }
            // 必须先隔离尾切片，再判选项/执行参数真值。不能持原 list 游标。
            string body = token[(token.StartsWith("--", StringComparison.Ordinal) ? 2 : 1)..];
            remaining = Tail(remaining);
            if (token.StartsWith("--", StringComparison.Ordinal)) Long(options, body, longs, ref remaining);
            else Short(options, body, shortopts, ref remaining);
        }
        return new PyTuple([options, remaining]);
    }

    private static object?[] LongOptions(object? value) => value switch
    {
        PyNamedTupleInstance => throw Unsupported("namedtuple longopts sequence"),
        string text => [text],
        PyList list => list.Items.ToArray(),
        PyTuple tuple => (object?[])tuple.Items.Clone(),
        null or PyNone or bool or int or long or BigInteger or double =>
            throw Own(new PyTypeError($"'{PyRepr.TypeName(value)}' object is not iterable")),
        _ => throw Unsupported("custom longopts iterable"),
    };

    private static string Token(object? value)
    {
        if (value is string text) return text;
        if (value is PyInstance or PyNativeObject) throw Unsupported("custom option string protocol");
        // 普通无 startswith 属性对象应保 CP 的 AttributeError，而不是一律变 unsupported。
        if (value is PyBytes or PyByteArray) throw Unsupported("bytes option string protocol");
        throw Own(new PyAttributeError($"'{PyRepr.TypeName(value)}' object has no attribute 'startswith'"));
    }

    private static string ShortOptions(object? value)
    {
        if (value is string text) return text;
        if (value is null or PyNone or bool or int or long or BigInteger or double)
            throw Own(new PyTypeError($"object of type '{PyRepr.TypeName(value)}' has no len()"));
        throw Unsupported("custom shortopts protocol");
    }

    private static object Tail(object args) => PyOps.GetItem(args, new PySlice(1L, null, null))!;

    private static void Short(PyList options, string cluster, object? declaration, ref object args)
    {
        // 原生 str 内无用户回调；以码点推进，只有必需参数的余串才切片一次。
        int offset = 0;
        while (offset < cluster.Length)
        {
            string option = PyText.ReadAt(cluster, ref offset);
            string shortopts = ShortOptions(declaration);
            bool found = false, requires = false;
            int index = 0;
            while (index < shortopts.Length)
            {
                string declared = PyText.ReadAt(shortopts, ref index);
                if (option != declared || declared == ":") continue;
                found = true;
                requires = index < shortopts.Length && shortopts[index] == ':';
                break; // 重复声明中第一个决定参数要求。
            }
            if (!found) throw Error($"option -{option} not recognized", option);
            object? value = "";
            if (requires)
            {
                if (offset < cluster.Length) value = cluster[offset..];
                else
                {
                    if (!PyOps.Truthy(args)) throw Error($"option -{option} requires argument", option);
                    value = PyOps.GetItem(args, 0L);
                    args = Tail(args);
                }
                offset = cluster.Length;
            }
            options.Items.Add(new PyTuple(["-" + option, value])); // 短参数绝不测值的真值。
        }
    }

    private static void Long(PyList options, string body, object?[] declarations, ref object args)
    {
        int equals = body.IndexOf('=');
        bool supplied = equals >= 0;
        string option = supplied ? body[..equals] : body;
        object? value = supplied ? body[(equals + 1)..] : null;
        var possibilities = new List<string>();
        // 完整扫描先于精确命中；后面的坏元素/重复候选不能被字典快路径吞掉。
        foreach (object? declaration in declarations)
        {
            string candidate = Token(declaration);
            if (candidate.StartsWith(option, StringComparison.Ordinal)) possibilities.Add(candidate);
        }
        if (possibilities.Count == 0) throw Error($"option --{option} not recognized", option);
        bool requires;
        if (possibilities.Contains(option)) requires = false;
        else if (possibilities.Contains(option + "=")) requires = true;
        else
        {
            if (possibilities.Count > 1) throw Error($"option --{option} not a unique prefix", option);
            option = possibilities[0];
            requires = option.EndsWith('=');
            if (requires) option = option[..^1];
        }
        if (requires && !supplied)
        {
            if (!PyOps.Truthy(args)) throw Error($"option --{option} requires argument", option);
            value = PyOps.GetItem(args, 0L);
            args = Tail(args);
        }
        else if (!requires && supplied)
            throw Error($"option --{option} must not have an argument", option);
        // 此处允许用户 __bool__/__len__；原异常与上下文不改写、不重捕获。
        object? result = PyOps.Truthy(value) ? value : "";
        options.Items.Add(new PyTuple(["--" + option, result]));
    }

    private static PyBaseException Error(string message, string option) => Own(new PyGetoptError(message, option));
    private static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
    private static PyNotImplementedError Unsupported(string feature) => Own(new PyNotImplementedError("getopt " + feature + " is not supported"));
}
