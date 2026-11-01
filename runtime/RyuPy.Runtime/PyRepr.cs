using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>
/// 动态值的 <c>str()</c> / <c>repr()</c> 与类型名。
/// </summary>
/// <remarks>
/// M2.1 的动态值表示是「装箱的 CLR 值」：Python <c>int</c> 是 <see cref="long"/> 或
/// <see cref="BigInteger"/>，<c>float</c> 是 <see cref="double"/>，<c>bool</c> 是
/// <see cref="bool"/>，<c>str</c> 是 <see cref="string"/>，<c>None</c> 是
/// <see cref="PyNone"/> 单例。类型判定因此是 CLR 类型判定，取反例时要先判断
/// <c>bool</c> 再判断数值——<c>bool</c> 在 CLR 里不继承 <c>long</c>，但 Python 里
/// <c>True == 1</c> 成立，这一差别只在比较与算术里显式处理。
/// </remarks>
public static class PyRepr
{
    /// <summary>Python 类型名，用于错误消息（<c>'int' and 'str'</c>）。</summary>
    public static string TypeName(object? value) => value switch
    {
        PyNativeObject native => native.TypeName,
        null => "NoneType",
        PyNone => "NoneType",
        PyUnbound => "NoneType",
        bool => "bool",
        long or int or BigInteger => "int",
        double or float => "float",
        string => "str",
        PyTuple => "tuple",
        PyList => "list",
        PyDict => "dict",
        PySet => "set",
        PyFrozenSet => "frozenset",
        PyBytes => "bytes",
        PyByteArray => "bytearray",
        PySlice => "slice",
        PyDictView view => view.Kind switch
        {
            PyDictViewKind.Keys => "dict_keys",
            PyDictViewKind.Values => "dict_values",
            _ => "dict_items",
        },
        PyType or PyNativeType => "type",
        PyClass => "type",
        PyModule => "module",
        PyInstance instance => instance.Class.Name,
        PyBoundMethod => "method",
        PyFunction => "function",
        PyBuiltinFunction or PyBuiltinMethod => "builtin_function_or_method",
        PyGenerator => "generator",
        PyNotImplemented => "NotImplementedType",
        PyProperty => "property",
        PyStaticMethod or PyClassMethod => "builtin_function_or_method",
        PySuper => "super",
        PyBaseException exception => exception.PyTypeName,
        _ => value.GetType().Name,
    };

    /// <summary><c>str(value)</c>。</summary>
    public static string Str(object? value) => value switch
    {
        PyNativeObject native => native.PythonStr(),
        null => "None",
        PyNone => "None",
        PyUnbound => "<unbound>",
        bool b => b ? "True" : "False",
        long l => l.ToString(CultureInfo.InvariantCulture),
        BigInteger big => big.ToString(CultureInfo.InvariantCulture),
        double d => PyFormat.FloatRepr(d),
        string s => s,
        // str(异常) 是消息本身，不含类型名（与 CPython 一致）
        PyBaseException exception => exception.PyMessage,
        PyTuple tuple => ReprTuple(tuple),
        PyList list => ReprList(list),
        PyDict dict => ReprDict(dict),
        PySet set => ReprSet(set.Items),
        PyFrozenSet frozen => "frozenset(" + ReprSet(frozen.Items) + ")",
        PyBytes bytes => ReprBytes(bytes.Bytes),
        PyByteArray array => "bytearray(" + ReprBytes([.. array.Bytes]) + ")",
        PySlice slice => slice.ToString(),
        PyDictView view => ReprView(view),
        PyType type => type.ToString(),
        PyNativeType type => type.ToString(),
        PyClass cls => cls.ToString(),
        PyInstance instance => StrInstance(instance),
        PyModule module => module.ToString(),
        PyBoundMethod method => method.ToString(),
        PyProperty property => property.ToString(),
        PyStaticMethod staticMethod => staticMethod.ToString(),
        PyClassMethod classMethod => classMethod.ToString(),
        _ => value.ToString() ?? string.Empty,
    };

    /// <summary><c>str(value)</c> 的编译期入口：返回 CIL 字符串，便于静态槽位直接使用。</summary>
    /// <param name="value">任意 Python 值。</param>
    public static string StrValue(object? value) => Str(value);

    /// <summary>
    /// <c>repr(value)</c>。字符串用单引号、转义反斜杠与控制字符；非 ASCII 字符保持原样
    /// （与 CPython 一致，<c>repr</c> 不把可打印 Unicode 转成 <c>\uXXXX</c>）。
    /// </summary>
    public static string Repr(object? value) => value switch
    {
        PyNativeObject native => native.PythonRepr(),
        string s => Quote(s),
        PyBaseException exception => ReprException(exception),
        PyTuple tuple => ReprTuple(tuple),
        PyList list => ReprList(list),
        PyDict dict => ReprDict(dict),
        PySet set => ReprSet(set.Items),
        PyFrozenSet frozen => "frozenset(" + ReprSet(frozen.Items) + ")",
        PyBytes bytes => ReprBytes(bytes.Bytes),
        PyByteArray array => "bytearray(" + ReprBytes([.. array.Bytes]) + ")",
        PyDictView view => ReprView(view),
        PyClass cls => cls.ToString(),
        PyInstance instance => ReprInstance(instance),
        PyModule module => $"<module '{module.Name}' from '{module.File}'>",
        PyBoundMethod method => method.ToString(),
        PyProperty property => property.ToString(),
        PyStaticMethod staticMethod => staticMethod.ToString(),
        PyClassMethod classMethod => classMethod.ToString(),
        _ => Str(value),
    };

    /// <summary>绑定方法 / 函数的名字（用于 <c>&lt;bound method Box.add of ...&gt;</c>）。</summary>
    public static string MethodName(object? target) => target switch
    {
        PyFunction function => function.Name,
        PyBoundMethod method => MethodName(method.Target),
        _ => TypeName(target),
    };

    /// <summary>实例的 <c>str()</c>：优先 `__str__`，其次 `__repr__`，最后默认形式。</summary>
    private static string StrInstance(PyInstance instance)
    {
        if (PyObject.TryMethod(instance, "__str__", [], out object? custom))
        {
            return RequireString(custom, "__str__", instance.Class.Name);
        }
        if (PyObject.TryMethod(instance, "__repr__", [], out object? repr))
        {
            return RequireString(repr, "__repr__", instance.Class.Name);
        }
        return instance.DefaultRepr();
    }

    /// <summary>实例的 <c>repr()</c>：只认 `__repr__`。</summary>
    private static string ReprInstance(PyInstance instance)
    {
        if (PyObject.TryMethod(instance, "__repr__", [], out object? repr))
        {
            return RequireString(repr, "__repr__", instance.Class.Name);
        }
        return instance.DefaultRepr();
    }

    private static string RequireString(object? value, string method, string className) =>
        value as string
        ?? throw new PyTypeError(
            $"__{method.Trim('_')}__ returned non-string (type {TypeName(value)})"
        );

    /// <summary>
    /// 容器的嵌套 <c>repr</c> 深度上限。
    /// </summary>
    /// <remarks>
    /// Python 允许自引用容器（<c>a = []; a.append(a)</c>），CPython 用递归标记输出
    /// <c>[[...]]</c>。RyuPy 目前用固定深度上限兜底，保证不会无界递归；
    /// M2.3 会把这里换成「正在格式化的对象集合」，输出与 CPython 一致的省略形式。
    /// </remarks>
    private const int MaxContainerDepth = 8;

    [ThreadStatic]
    private static int _containerDepth;

    /// <summary>元组的 <c>repr</c>：单元素要加逗号。</summary>
    private static string ReprTuple(PyTuple tuple)
    {
        if (_containerDepth >= MaxContainerDepth)
        {
            return "(...)";
        }
        _containerDepth++;
        try
        {
            if (tuple.Items.Length == 0)
            {
                return "()";
            }
            var builder = new StringBuilder("(");
            for (int i = 0; i < tuple.Items.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(Repr(tuple.Items[i]));
            }
            if (tuple.Items.Length == 1)
            {
                builder.Append(',');
            }
            builder.Append(')');
            return builder.ToString();
        }
        finally
        {
            _containerDepth--;
        }
    }

    /// <summary>异常对象的 <c>repr</c>：<c>ValueError('x')</c>。</summary>
    private static string ReprException(PyBaseException exception)
    {
        if (exception.Args.Length == 0)
        {
            return exception.PyTypeName + "()";
        }
        var parts = new string[exception.Args.Length];
        for (int i = 0; i < exception.Args.Length; i++)
        {
            parts[i] = Repr(exception.Args[i]);
        }
        return exception.PyTypeName + "(" + string.Join(", ", parts) + ")";
    }

    // ───────────────────── 容器 repr ─────────────────────
    //
    // 容器可以自引用（`a = []; a.append(a)`），必须记录「正在格式化」的对象，
    // 否则会无限递归。CPython 输出 `[[...]]` / `{'k': {...}}` 这种省略形式，
    // 这里沿用同样的规则。

    [ThreadStatic]
    private static HashSet<object>? _inProgress;

    /// <summary>把容器 repr 包在递归保护里。</summary>
    private static string WithCycleGuard(object container, string recursionLabel, Func<string> body)
    {
        _inProgress ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
        if (!_inProgress.Add(container))
        {
            return recursionLabel;
        }
        try
        {
            return body();
        }
        finally
        {
            _inProgress.Remove(container);
        }
    }

    /// <summary>列表的 <c>repr</c>。</summary>
    private static string ReprList(PyList list) =>
        WithCycleGuard(list, "[...]", () =>
        {
            var builder = new StringBuilder("[");
            for (int i = 0; i < list.Items.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(Repr(list.Items[i]));
            }
            builder.Append(']');
            return builder.ToString();
        });

    /// <summary>字典的 <c>repr</c>：保持插入顺序。</summary>
    private static string ReprDict(PyDict dict) =>
        WithCycleGuard(dict, "{...}", () =>
        {
            var builder = new StringBuilder("{");
            bool first = true;
            foreach (var (key, value) in dict.Entries())
            {
                if (!first)
                {
                    builder.Append(", ");
                }
                first = false;
                builder.Append(Repr(key)).Append(": ").Append(Repr(value));
            }
            builder.Append('}');
            return builder.ToString();
        });

    /// <summary>集合的 <c>repr</c>；空集合必须写成 <c>set()</c>。</summary>
    private static string ReprSet(IEnumerable<object> items)
    {
        var parts = new List<string>();
        foreach (object item in items)
        {
            parts.Add(Repr(item));
        }
        if (parts.Count == 0)
        {
            return "set()";
        }
        parts.Sort(StringComparer.Ordinal);
        return "{" + string.Join(", ", parts) + "}";
    }

    /// <summary>视图的 <c>repr</c>。</summary>
    private static string ReprView(PyDictView view)
    {
        string name = view.Kind switch
        {
            PyDictViewKind.Keys => "dict_keys",
            PyDictViewKind.Values => "dict_values",
            _ => "dict_items",
        };
        return name + "(" + ReprList(new PyList(PyOps.ToSequence(view.ToArray()))) + ")";
    }

    /// <summary>
    /// <c>bytes</c> 的 <c>repr</c>：可打印 ASCII 原样输出，其余用 <c>\xNN</c>。
    /// </summary>
    private static string ReprBytes(byte[] bytes)
    {
        var builder = new StringBuilder(bytes.Length + 3);
        builder.Append("b'");
        foreach (byte b in bytes)
        {
            switch (b)
            {
                case (byte)'\\':
                    builder.Append("\\\\");
                    break;
                case (byte)'\'':
                    builder.Append("\\'");
                    break;
                case (byte)'\n':
                    builder.Append("\\n");
                    break;
                case (byte)'\r':
                    builder.Append("\\r");
                    break;
                case (byte)'\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (b >= 0x20 && b < 0x7f)
                    {
                        builder.Append((char)b);
                    }
                    else
                    {
                        builder.Append("\\x").Append(b.ToString("x2"));
                    }
                    break;
            }
        }
        builder.Append('\'');
        return builder.ToString();
    }

    /// <summary>字符串的 Python <c>repr</c>：优先单引号，内部单引号且无单引号时切换双引号。</summary>
    public static string Quote(string text)
    {
        bool hasSingle = text.Contains('\'');
        bool hasDouble = text.Contains('"');
        char quote = hasSingle && !hasDouble ? '"' : '\'';
        var builder = new StringBuilder(text.Length + 2);
        builder.Append(quote);
        foreach (char ch in text)
        {
            switch (ch)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (ch == quote)
                    {
                        builder.Append('\\');
                    }
                    builder.Append(ch);
                    break;
            }
        }
        builder.Append(quote);
        return builder.ToString();
    }
}

/// <summary>
/// Python <c>None</c> 的单例。用独立类型而不是 <c>null</c>：<c>null</c> 在 CIL 里表示
/// 「未初始化引用」，会让 <c>None</c> 与「变量尚未赋值」混淆，也无法参与 <c>repr</c>
/// 与容器元素。
/// </summary>
public sealed class PyNone
{
    private PyNone() { }

    /// <summary>唯一的 <c>None</c> 实例。</summary>
    public static readonly PyNone Instance = new();

    /// <summary>与 <c>str(None)</c> 一致。</summary>
    public override string ToString() => "None";
}
