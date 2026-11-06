// TextWrapper的普通类工厂与动态编排；配置保原值，覆盖点走现有属性和绑定协议。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal static partial class PyTextWrapper
{
    private static readonly string[] Fields = ["width", "initial_indent", "subsequent_indent", "expand_tabs",
        "replace_whitespace", "fix_sentence_endings", "break_long_words", "drop_whitespace",
        "break_on_hyphens", "tabsize", "max_lines", "placeholder"];

    internal static PyClass Build(Dictionary<string, object?> module)
    {
        FastPlan? plan = null;
        var root = new PyClass("TextWrapper") { NativeDefinitionModule = "textwrap" };
        root.SetAttr("__init__", Function("TextWrapper.__init__",
            ["self", .. Fields], [null, 70L, "", "", true, true, false, true, true, true, 8L, PyNone.Instance, " [...]"], 1,
            Init, ["pos", "pos", "pos", "pos", "pos", "pos", "pos", "pos", "pos", "pos", "pos", "kwonly", "kwonly"]));
        root.SetAttr("wrap", Method("wrap", ["self", "text"], a => Wrap(a[0], a[1], plan)));
        root.SetAttr("fill", Method("fill", ["self", "text"], a => Fill(a[0], a[1])));
        root.SetAttr("_split_chunks", Method("_split_chunks", ["self", "text"], a => SplitChunks(a[0], a[1])));
        root.SetAttr("_munge_whitespace", Method("_munge_whitespace", ["self", "text"], a => MungeWhitespace(a[0], a[1], root)));
        root.SetAttr("_split", Method("_split", ["self", "text"], a => Split(a[0], a[1], root)));
        root.SetAttr("_fix_sentence_endings", Method("_fix_sentence_endings", ["self", "chunks"],
            _ => throw Unsupported("textwrap sentence fixing is not supported")));
        root.SetAttr("_wrap_chunks", Method("_wrap_chunks", ["self", "chunks"], a => WrapChunks(a[0], a[1])));
        root.SetAttr("_handle_long_word", Method("_handle_long_word",
            ["self", "reversed_chunks", "cur_line", "cur_len", "width"],
            a => HandleLongWord(a[0], a[1], a[2], a[3], a[4])));
        PyOps.FinalizeOrdinaryClass(root);
        // 只分配槽号，不提前写属性；之后普通init不会使root资格因首次字段出现失效。
        var slots = new int[Fields.Length];
        for (int index = 0; index < Fields.Length; index++) slots[index] = root.GetOrAddInstanceSlot(Fields[index]);
        plan = new FastPlan(root, module, slots);
        return root;
    }

    private static object Init(object?[] args)
    {
        // 按签名顺序写入；setter异常保已完成写入，不做类型或数值预检。
        for (int index = 0; index < Fields.Length; index++) PyObject.SetAttr(args[0], Fields[index], args[index + 1]);
        return PyNone.Instance;
    }

    private static object? Wrap(object? self, object? text, FastPlan? plan)
    {
        if (plan is not null && plan.TryWrap(self, text, out object? fast)) return fast;
        object? chunks = Call(self, "_split_chunks", text);
        if (PyOps.Truthy(PyObject.GetAttr(self, "fix_sentence_endings"))) Call(self, "_fix_sentence_endings", chunks);
        return Call(self, "_wrap_chunks", chunks);
    }

    private static object? Fill(object? self, object? text) => Call("\n", "join", Call(self, "wrap", text));

    private static object? SplitChunks(object? self, object? text)
    {
        object? normalized = Call(self, "_munge_whitespace", text);
        return Call(self, "_split", normalized);
    }

    internal static object? Call(object? self, string name, params object?[] args) =>
        PyOps.CallValue(PyObject.GetAttr(self, name), args, [], []);

    internal static PyNotImplementedError Unsupported(string message) =>
        (PyNotImplementedError)PyErrors.PrepareRaise(new PyNotImplementedError(message));

    private static PyFunction Method(string name, string[] names, Func<object?[], object?> body) =>
        Function("TextWrapper." + name, names, new object?[names.Length], names.Length, body);

    // 仅本API校准带默认位置参数的冷错误。先让既有binder检查未知/重复关键字，
    // 不进入用户正文，也不按异常字符串猜测来源。正常调用不分配截断数组。
    // 当前本地签名无必填kwonly；此适配不作为一般Python binder的新实现。
    private static object?[] Bind(PySignature signature, object?[] args, string[] keywords,
        object?[] values, object?[] defaults, int minimum, int maximum)
    {
        if (minimum < maximum && args.Length > maximum)
        {
            var allowed = new object?[maximum]; Array.Copy(args, allowed, maximum);
            _ = PyBinder.Bind(signature, allowed, keywords, values, defaults);
            int keywordOnly = 0;
            foreach (string keyword in keywords)
                for (int index = 0; index < signature.Names.Length; index++)
                    if (signature.Names[index] == keyword && signature.Kinds[index] == PyParamKind.KeywordOnly) keywordOnly++;
            string given = keywordOnly == 0 ? $"{args.Length}" :
                $"{args.Length} positional arguments (and {keywordOnly} keyword-only argument{(keywordOnly == 1 ? "" : "s")})";
            throw new PyTypeError($"{signature.Name}() takes from {minimum} to {maximum} positional arguments but {given} were given");
        }
        return PyBinder.Bind(signature, args, keywords, values, defaults);
    }

    // 绑定错误与算法错误统一准备；用户异常不换身份，也不覆盖已经保存的帧/context。
    internal static PyFunction Function(string name, string[] names, object?[] defaults, int required,
        Func<object?[], object?> body, string[]? kinds = null)
    {
        PySignature signature = PyNativeFunctions.Create(name, names, defaults, required, body, kinds).Signature;
        int maximum = 0, minimum = 0;
        foreach (PyParamKind kind in signature.Kinds)
            if (kind is PyParamKind.PositionalOnly or PyParamKind.PositionalOrKeyword) maximum++;
        for (int index = 0; index < maximum; index++) if (!signature.HasDefault[index]) minimum++;
        string shortName = name[(name.LastIndexOf('.') + 1)..];
        var function = new PyFunction(shortName, signature, (self, args, keywords, values) =>
        {
            try { return body(Bind(signature, args, keywords, values, self.Defaults, minimum, maximum)) ?? PyNone.Instance; }
            catch (PyBaseException error) { throw (PyBaseException)PyErrors.PrepareRaise(error); }
        }, defaults, false);
        PyOps.FunctionModule(function, "textwrap");
        return function;
    }
}
