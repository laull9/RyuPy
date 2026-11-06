// TextWrapper 的空白预处理与真实词块列表；配置在消费点读取，允许普通 duck self。
using System;
using System.Numerics;

namespace RyuPy.Runtime;

internal static partial class PyTextWrapper
{
    internal static object? MungeWhitespace(object? self, object? text, PyClass defaultOwner)
    {
        if (PyOps.Truthy(PyObject.GetAttr(self, "expand_tabs")))
        {
            // Python 先取得 text.expandtabs，再计算 tabsize 实参；错误顺序不能倒置。
            string source = TextForMethod(text, "expandtabs");
            long size = AlgorithmInteger(PyObject.GetAttr(self, "tabsize"), "tabsize");
            if (size < int.MinValue || size > int.MaxValue)
                throw Unsupported("textwrap.TextWrapper tabsize outside the C int range is not supported");
            text = PyTextWrapEngine.Normalize(source, true, false, size);
        }
        if (PyOps.Truthy(PyObject.GetAttr(self, "replace_whitespace")))
        {
            string source = TextForMethod(text, "translate");
            RequireDefaultClassData(self, "unicode_whitespace_trans", defaultOwner);
            text = PyTextWrapEngine.Normalize(source, false, true, 8);
        }
        // 两步都关闭时，CP 原样返回输入，文本域错误由后续 _split 决定。
        return text;
    }

    internal static object? Split(object? self, object? text, PyClass defaultOwner)
    {
        // CP 在分词处判断 is True；数字 1 不是 True，长词断行处则另用真值判断。
        bool hyphens = PyObject.GetAttr(self, "break_on_hyphens") is true;
        RequireDefaultClassData(self, hyphens ? "wordsep_re" : "wordsep_simple_re", defaultOwner);
        if (text is not string source)
        {
            if (text is PyInstance or PyNativeObject)
                throw Unsupported("textwrap.TextWrapper custom text protocols are not supported");
            if (text is PyBytes or PyByteArray)
                throw new PyTypeError("cannot use a string pattern on a bytes-like object");
            throw new PyTypeError($"expected string or bytes-like object, got '{PyRepr.TypeName(text)}'");
        }
        return new PyList(PyTextWrapEngine.SplitText(source, hyphens));
    }

    private static string TextForMethod(object? value, string method)
    {
        if (value is string text) return text;
        if (value is PyInstance or PyNativeObject or PyBytes or PyByteArray)
            throw Unsupported("textwrap.TextWrapper custom and binary text protocols are not supported");
        throw new PyAttributeError($"'{PyRepr.TypeName(value)}' object has no attribute '{method}'");
    }

    private static long AlgorithmInteger(object? value, string name)
    {
        if (!PyInt.IsInt(value))
            throw Unsupported("textwrap.TextWrapper non-integer " + name + " is not supported");
        BigInteger number = PyInt.ToBigInteger(value!);
        if (number < long.MinValue || number > long.MaxValue)
            throw Unsupported("textwrap.TextWrapper integer exceeds the supported resource range");
        return (long)number;
    }

    private static string AlgorithmText(object? value) => value as string ??
        throw Unsupported("textwrap.TextWrapper chunk and indent values must be actual strings");

    private static PyList AlgorithmList(object? value) => value as PyList ??
        throw Unsupported("textwrap.TextWrapper chunk containers must be actual lists");

    private static void RequireDefaultClassData(object? self, string name, PyClass defaultOwner)
    {
        if (self is not PyInstance receiver || !receiver.Class.IsSubclassOf(defaultOwner))
        {
            // 无关 duck 不继承默认类数据：在真正消费点查找，缺属性与 __getattr__ 副作用照常发生。
            _ = PyObject.GetAttr(self, name);
            throw Unsupported("textwrap.TextWrapper custom " + name + " is not supported");
        }
        // 默认类暂不导出 CP 的 regex/translate 表。不触发额外 __getattr__，也不忽略用户覆写。
        bool own = receiver.Class.TryGetInstanceSlot(name, out int slot) && receiver.TryGetSlot(slot, out _);
        if (own || receiver.Class.TryLookup(name, out _, out _))
            throw Unsupported("textwrap.TextWrapper custom " + name + " is not supported");
    }
}
