// str.expandtabs的唯一绑定/转换/两遍填充权威；冷类型槽协议局限于本API，不扩全局调用ABI。
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

internal static class PyTextExpandTabs
{
    // .NET10真实UTF16容量，不是Python字符数或业务额度；越界是已登记的宿主偏差。
    internal const int MaxLength = 0x3FFFFFDF;

    internal static string Invoke(object receiver, object?[] args, string[] names, object?[] values)
    {
        string text = PyStringLayout.RequireRaw(receiver);
        int count = args.Length + names.Length;
        if (count > 1)
            throw Own(new PyTypeError($"expandtabs() takes at most 1 {(args.Length == 0 ? "keyword argument" : "argument")} ({count} given)"));
        if (names.Length == 1 && names[0] != "tabsize")
            throw Own(new PyTypeError($"'{names[0]}' is an invalid keyword argument for expandtabs()"));
        // 回调可能重入PIC同站点并覆盖实参数组；正文只消费这里冻结的文本与参数。
        object? sizeObject = args.Length == 1 ? args[0] : names.Length == 1 ? values[0] : 8;
        int size = Index(sizeObject);
        // 空串/无tab也必须完成转换及其警告；相同长度不代表相同内容。
        if (text.IndexOf('\t') < 0) return text;
        int length = Measure(text, size);
        return string.Create(length, (text, size), static (destination, state) =>
            Fill(destination, state.text, state.size));
    }

    private static int Index(object? value) => value switch
    {
        int number => number,
        long number => Narrow(number),
        bool flag => flag ? 1 : 0,
        BigInteger number => Narrow(number),
        _ => IndexSlow(value),
    };

    private static int Narrow(long number)
    {
        if (number < int.MinValue || number > int.MaxValue) throw Overflow();
        return (int)number;
    }

    private static int Narrow(BigInteger number)
    {
        if (number < int.MinValue || number > int.MaxValue) throw Overflow();
        return (int)number;
    }

    private static PyOverflowError Overflow() =>
        Own(new PyOverflowError("Python int too large to convert to C int"));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int IndexSlow(object? value)
    {
        object? result;
        PyClass? cls = PythonClass(value);
        if (cls is not null)
        {
            cls.GuardInheritedGenericProtocol("__index__");
            if (!cls.TryLookup("__index__", out PyClass owner, out object? raw)) throw MissingIndex(value);
            result = InvokeIndexSlot(value!, cls, owner, raw);
        }
        else if (value is PyNativeObject && PyObject.TryMethod(value, "__index__", [], out result))
        {
            // 保留已有native特殊槽域，不把普通实例字段当作类型槽。
        }
        else throw MissingIndex(value);
        // 用户getter/index异常不catch；bool结果的警告可抛错或重入，之后不重读任何实参。
        if (result is bool flag)
        {
            PyWarnings.IndexReturnedStrictSubclass("bool");
            return flag ? 1 : 0;
        }
        return result switch
        {
            int number => number,
            long number => Narrow(number),
            BigInteger number => Narrow(number),
            _ => throw Own(new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(result)})")),
        };
    }

    private static PyTypeError MissingIndex(object? value) =>
        Own(new PyTypeError($"'{PyRepr.TypeName(value)}' object cannot be interpreted as an integer"));

    private static PyClass? PythonClass(object? value) => value switch
    {
        PyInstance instance => instance.Class,
        PyBaseException exception => exception.PythonClass,
        _ => null,
    };

    // __index__按实际类型链取槽，不接受实例遮蔽；上层普通描述符绑定与下层raw Get严格分开。
    private static object? InvokeIndexSlot(object self, PyClass actualClass, PyClass owner, object? raw)
    {
        if (raw is PyFunction) return PyObject.InvokeBound(raw, owner, self, [], [], []);
        if (raw is PyStaticMethod staticMethod)
        {
            CheckCallable(staticMethod.Value);
            return PyObject.InvokeBound(raw, owner, self, [], [], []);
        }
        if (raw is PyClassMethod classMethod)
        {
            CheckCallable(classMethod.Value);
            return PyObject.InvokeBound(raw, owner, self, [], [], []);
        }
        object? callable;
        if (raw is PyProperty property)
        {
            object? getter = property.Getter;
            if (getter is null or PyNone)
                throw Own(new PyAttributeError($"property '__index__' of '{actualClass.Name}' object has no getter"));
            // 普通getter函数保已有super上下文；其它getter是raw callable，显式接收self。
            callable = getter is PyFunction
                ? PyObject.InvokeBound(getter, owner, self, [], [], []) : CallRaw(getter, [self]);
        }
        else if (PythonClass(raw) is { } descriptorClass)
        {
            descriptorClass.GuardInheritedGenericProtocol("__get__");
            if (descriptorClass.TryLookup("__get__", out PyClass getOwner, out object? get))
                // 普通函数仍收到三个参数，并保已有super的owner/self上下文；
                // 其它raw槽绝不再次descriptor绑定，static包装也必须接收完整三参。
                callable = get is PyFunction
                    ? PyObject.InvokeBound(get, getOwner, raw, [self, actualClass], [], [])
                    : CallRaw(get, [raw, self, actualClass]);
            else callable = PyObject.Bind(raw, owner, self);
        }
        else callable = PyObject.Bind(raw, owner, self);
        return CallRaw(callable, []);
    }

    // CallValue旧入口会解包classmethod；本API的raw调用明确拒绝它和property，绝不调用getter。
    private static object? CallRaw(object? target, object?[] args)
    {
        target = CheckCallable(target);
        return PyOps.CallValue(target, args, [], []);
    }

    private static object? CheckCallable(object? target)
    {
        while (target is PyStaticMethod method) target = method.Value;
        bool bad = target switch
        {
            PyClassMethod or PyProperty => true,
            PyInstance instance => !instance.Class.TryLookup("__call__", out _, out _),
            IPyNativeCallable or PyFunction or PyBuiltinFunction or PyNativeType or PyClass
                or PyBoundMethod or PyBuiltinMethod or PyType => false,
            _ => true,
        };
        // 旧TypeName将两个wrapper统称内建方法；此处必须保CP的精确classmethod标签。
        if (bad) throw Own(new PyTypeError($"'{(target is PyClassMethod ? "classmethod" : PyRepr.TypeName(target))}' object is not callable"));
        return target;
    }

    // 纯容量比较可测limit±1而不分配；每步计长即检查，不等巨大结果构造后再拒绝。
    internal static int CheckCapacity(long length)
    {
        if (length > MaxLength)
            throw Own(new PyNotImplementedError("str.expandtabs: result exceeds managed string capacity"));
        return (int)length;
    }

    internal static int Measure(string text, int size)
    {
        long length = 0, column = 0;
        for (int offset = 0; offset < text.Length;)
        {
            char current = text[offset];
            if (current == '\t')
            {
                long spaces = size > 0 ? size - column % size : 0;
                length += spaces;
                column += spaces;
                offset++;
            }
            else
            {
                int width = Width(text, offset);
                length += width;
                column = current is '\r' or '\n' ? 0 : column + 1;
                offset += width;
            }
            CheckCapacity(length);
        }
        return (int)length;
    }

    // 有效代理对是一列两个UTF16单位；孤立代理逐单位原拷贝，只有CR/LF重置逻辑列。
    private static int Width(string text, int offset) => char.IsHighSurrogate(text[offset])
        && offset + 1 < text.Length && char.IsLowSurrogate(text[offset + 1]) ? 2 : 1;

    private static void Fill(Span<char> destination, string text, int size)
    {
        long column = 0;
        int written = 0;
        for (int offset = 0; offset < text.Length;)
        {
            char current = text[offset];
            if (current == '\t')
            {
                int spaces = size > 0 ? (int)(size - column % size) : 0;
                destination.Slice(written, spaces).Fill(' ');
                written += spaces;
                column += spaces;
                offset++;
            }
            else
            {
                int width = Width(text, offset);
                text.AsSpan(offset, width).CopyTo(destination.Slice(written, width));
                written += width;
                column = current is '\r' or '\n' ? 0 : column + 1;
                offset += width;
            }
        }
    }

    // 仅本API自造错误准备现场；任何用户正文和warning输出异常保持原身份与来源。
    private static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}
