// Scanner是普通Python类：构造按原迭代顺序解析，scan每个token重新读取action与公开字段。
// 这里只适配有限冷绑定/准入，不修改共享callable、属性协议、迭代器或RuntimeFn ABI。
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace RyuPy.Runtime;

internal static class PyRegexScanner
{
    internal static PyClass Build()
    {
        var root = new PyClass("Scanner") { NativeDefinitionModule = "re" };
        root.SetAttr("__init__", Method("__init__", ["self", "lexicon", "flags"], [null, null, 0L], 2, Init));
        root.SetAttr("scan", Method("scan", ["self", "string"], [null, null], 2, Scan));
        PyOps.FinalizeOrdinaryClass(root);
        // 只预分配槽号；初始match及失败构造的scanner都必须仍然不存在。
        foreach (string name in new[] { "lexicon", "scanner", "match" }) root.GetOrAddInstanceSlot(name);
        return root;
    }

    private static object Init(object?[] args)
    {
        object? self = args[0], lexicon = args[1];
        PyObject.SetAttr(self, "lexicon", lexicon);
        // 已有__iter__优先且异常直传；只提供getitem的序列fallback本切明确待实现。
        if (lexicon is PyInstance instance && !instance.Class.TryLookup("__iter__", out _, out _)
            && instance.Class.TryLookup("__getitem__", out _, out _))
            throw Unsupported("sequence-only lexicon iteration");
        IPyIterator iterator = PyIter.Create(lexicon);
        var fragments = new List<string>();
        int total = 0;
        int translatedLength = PyRegexScannerPattern.InitialSize;
        int? rootFlags = null;
        while (iterator.MoveNext())
        {
            var pair = (PyTuple)PyOps.UnpackSequence(iterator.Current(), 2);
            // 恰二项解包先于资源门和phrase解析，不预读下一项或消费action。
            if (fragments.Count == 1024) throw Unsupported("lexicon item resource limit (1024)");
            var (source, bytes) = Phrase(pair.Items[0]);
            if (source.Length > 16384 || source.Length > 16384 - total)
                throw Unsupported("pattern length resource limit (16384)");
            total += source.Length;
            rootFlags ??= Flags(args[2]);
            string translated;
            // 此段只有本地parser，不捕获上面的迭代、解包或setter用户异常。
            try { translated = new PyRegexParser(source, rootFlags.Value, bytes, scanner: true).Translate(); }
            catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
            // 不能把总译文门推迟到Create：当前fragment加进列表之前计入wrapper/边界，
            // 避免许多短Unicode类别先积累N份大译文，也不读取下一项寻找更晚错误。
            translatedLength = PyRegexScannerPattern.AddSize(translatedLength, translated, fragments.Count + 1);
            fragments.Add(translated);
        }
        int flags = rootFlags ?? Flags(args[2]);
        // 固定CP实证更正：空lexicon的根0在最终编译点失败，不能发布永不匹配假plan。
        // 其它空flags组合尚无完整合同，明确NI；此前lexicon/迭代副作用及旧字段均保留。
        if (fragments.Count == 0)
        {
            if (flags == 0) throw Own(new PyRuntimeError("invalid SRE code"));
            throw Unsupported("empty lexicon flags");
        }
        var plan = PyRegexScannerPattern.Create(fragments, flags);
        PyObject.SetAttr(self, "scanner", plan);
        return PyNone.Instance;
    }

    private static object Scan(object?[] args)
    {
        object? self = args[0];
        object? backend = PyObject.GetAttr(self, "scanner");
        if (backend is not PyRegexScannerPattern plan)
        {
            // 普通Pattern的私有scanner API尚未实现；其它对象先真实读取属性，
            // 让缺属性/property异常在原位置发生，但绝不半调用自造backend。
            if (backend is not PyRegexPattern) _ = PyObject.GetAttr(backend, "scanner");
            throw Unsupported("replacement backend scanner");
        }
        var cursor = plan.Cursor(args[1]);
        var result = new List<object?>();
        int consumed = 0;
        while (cursor.Next(out PyRegexMatch? match, out int wrapper, out int end))
        {
            object? lexicon = PyObject.GetAttr(self, "lexicon");
            object? entry = PyOps.GetItem(lexicon, (long)wrapper - 1);
            object? action = PyOps.GetItem(entry, 1L);
            if (Callable(action))
            {
                // 不预绑定__call__；match setter可以改变action所属类的调用槽。
                PyObject.SetAttr(self, "match", match);
                object token = match!.Group(0);
                RejectCallDescriptor(action);
                action = PyOps.CallValue(action, [self, token], [], []);
            }
            if (action is not null and not PyNone) result.Add(action);
            consumed = end;
        }
        return new PyTuple([new PyList(result), cursor.Remainder(consumed)]);
    }

    private static bool Callable(object? value) => value is PyInstance instance
        ? instance.Class.TryLookup("__call__", out _, out _)
        : value is PyType or IPyNativeCallable or PyFunction or PyBuiltinFunction or PyBuiltinMethod
            or PyNativeType or PyClass or PyBoundMethod or PyStaticMethod;

    // 共享CallValue未实现任意descriptor绑定。本切在真正调用点明确拒绝，
    // 不把property当常量，也不在callable分类中运行getter或真值协议。
    private static void RejectCallDescriptor(object? action)
    {
        // staticmethod自身可调用，但最终目标仍可能是未实现descriptor实例。
        // 只读解包不触发协议；真实调用仍传原action，不能提前绑定被setter改变的槽。
        while (action is PyStaticMethod wrapper) action = wrapper.Value;
        if (action is PyClassMethod)
            throw Own(new PyTypeError("'classmethod' object is not callable"));
        if (action is PyInstance instance && instance.Class.TryLookup("__call__", out _, out object? slot)
            && (slot is PyProperty or IPyNativeDescriptor || slot is PyInstance descriptor
                && descriptor.Class.TryLookup("__get__", out _, out _)))
            throw Unsupported("callable action descriptor");
    }

    private static (string Source, PyBytes? Bytes) Phrase(object? value)
    {
        if (value is string text) return (text, null);
        if (value is PyBytes bytes)
        {
            // 先用已知bytes长度守资源门，不能为超限源先分配整块Latin1字符串。
            if (bytes.Count > 16384) throw Unsupported("pattern length resource limit (16384)");
            return (Encoding.Latin1.GetString(bytes.Bytes), bytes);
        }
        if (DefinitelyNotBuffer(value))
            throw Own(new PyTypeError($"decoding to str: need a bytes-like object, {PyRepr.TypeName(value)} found"));
        throw Unsupported("pattern source buffer or subclass");
    }

    // 已知非buffer值给真实TypeError；未知用户/native buffer不能被猜测为非法输入。
    internal static bool DefinitelyNotBuffer(object? value) => value is null or PyNone or long or int or bool
        or BigInteger or double or float or PyList or PyTuple or PyDict or PySet or PyFrozenSet
        or PyRegexPattern or PyClass or PyType or PyFunction;

    private static int Flags(object? value)
    {
        BigInteger number = value switch
        {
            bool flag => flag ? BigInteger.One : BigInteger.Zero,
            long flag => flag, int flag => flag, BigInteger flag => flag,
            _ => throw Unsupported("flags arithmetic protocol"),
        };
        if (number < 0 || number > int.MaxValue) throw Unsupported("flags integer range");
        int flags = (int)number;
        // L先保留到phrase语法解析后的源域检查；未知位不进入宿主Regex。
        if ((flags & ~(2 | 4 | 8 | 16 | 32 | 64 | 256)) != 0) throw Unsupported("flags");
        return flags;
    }

    internal static PyNotImplementedError Unsupported(string feature) =>
        (PyNotImplementedError)Own(new PyNotImplementedError("re.Scanner: unsupported " + feature));

    internal static PyBaseException Own(PyBaseException error) => (PyBaseException)PyErrors.PrepareRaise(error);

    private static PyFunction Method(string name, string[] names, object?[] defaults, int required,
        Func<object?[], object?> body)
    {
        var signature = PyNativeFunctions.Create("Scanner." + name, names, defaults, required, body).Signature;
        var method = new PyFunction(name, signature, (self, args, keywords, values) =>
        {
            object?[] bound;
            try { bound = Bind(signature, args, keywords, values, self.Defaults); }
            catch (PyTypeError error) { PyErrors.PrepareRaise(error); throw; }
            // 用户异常不在本地binder catch内，不新包一层或覆盖它的cause/context。
            return body(bound);
        }, defaults, false);
        PyOps.FunctionModule(method, "re");
        return method;
    }

    // 仅本类的两个位置或关键字签名校准CP冷错误。仍用共享binder核对关键字；
    // 多位置参数不能抢在重复string/未知关键字之前，缺参文本保Python连接词。
    private static object?[] Bind(PySignature signature, object?[] args, string[] names,
        object?[] values, object?[] defaults)
    {
        int maximum = signature.Names.Length;
        if (args.Length > maximum)
        {
            var allowed = new object?[maximum]; Array.Copy(args, allowed, maximum);
            _ = PyBinder.Bind(signature, allowed, names, values, defaults);
            string range = signature.RequiredCount == maximum ? maximum.ToString()
                : $"from {signature.RequiredCount} to {maximum}";
            throw new PyTypeError($"{signature.Name}() takes {range} positional arguments but {args.Length} were given");
        }
        // 两方法最多两个必填参数；全缺时共享binder目前没有末项and。
        if (args.Length == 0 && signature.RequiredCount == 2)
        {
            bool first = Array.IndexOf(names, signature.Names[0]) >= 0;
            bool second = Array.IndexOf(names, signature.Names[1]) >= 0;
            if (!first && !second)
            {
                // 先让binder检查未知/重复关键字，补默认只用于冷错误判别。
                var optional = PySignature.Create(signature.Name, signature.Names,
                    signature.Names.Length == 3 ? ["pos", "pos", "pos"] : ["pos", "pos"],
                    signature.Names.Length == 3 ? ["1", "1", "1"] : ["1", "1"]);
                _ = PyBinder.Bind(optional, args, names, values, defaults);
                throw new PyTypeError($"{signature.Name}() missing 2 required positional arguments: '{signature.Names[0]}' and '{signature.Names[1]}'");
            }
        }
        return PyBinder.Bind(signature, args, names, values, defaults);
    }
}
