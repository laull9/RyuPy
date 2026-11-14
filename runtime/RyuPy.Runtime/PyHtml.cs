// html.escape 的有限真实协议：逐次读取当前对象的 replace，三步之后才判断 quote。
// 精确字符串只复用 Ordinal Replace；高阶未实现域明确守门，不改变全局属性/真值协议。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>html 的有限原生入口：escape 保逐步替换、迟真值与对象身份。</summary>
public static class PyHtml
{
    private static readonly PySignature Signature = PySignature.Create("escape",
        ["s", "quote"], ["pos", "pos"], ["0", "1"]);
    private static readonly object?[] Defaults = [null, true];
    private static readonly EscapeFunction Function = new();

    /// <summary>只导出已闭合的 escape；不以空 unescape/entities/parser 冒充完整 html。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new(StringComparer.Ordinal)
    {
        ["escape"] = Function,
    };

    private static object? Escape(object?[] args, string[] names, object?[] values)
    {
        object?[] bound;
        try
        {
            if (args.Length > 2)
            {
                // CP 先按顺序检查关键字，再拒绝超量位置参数；截断只用于冷错误诊断。
                PyBinder.Bind(Signature, args[..2], names, values, Defaults);
                throw new PyTypeError($"escape() takes from 1 to 2 positional arguments but {args.Length} were given");
            }
            bound = PyBinder.Bind(Signature, args, names, values, Defaults);
        }
        catch (PyTypeError error) { throw Own(error); }

        // 不预先验证 s/quote，不强转返回值；用户正文不在绑定 catch 中。
        object? current = Replace(bound[0], "&", "&amp;");
        current = Replace(current, "<", "&lt;");
        current = Replace(current, ">", "&gt;");
        GuardTruthSlot(bound[1]);
        if (PyOps.Truthy(bound[1]))
        {
            current = Replace(current, "\"", "&quot;");
            current = Replace(current, "'", "&#x27;");
        }
        return current;
    }

    private static object? Replace(object? current, string old, string replacement)
    {
        // 每一步重新判断，不把 str 子类剥成 payload；无匹配由 CLR 保原字符串身份。
        if (current is string text) return text.Replace(old, replacement, StringComparison.Ordinal);
        GuardReplacementLookup(current);
        object? method = PyObject.GetAttr(current, "replace");
        return PyOps.CallValue(method, [old, replacement], [], []);
    }

    private static void GuardReplacementLookup(object? current)
    {
        if (current is not PyInstance instance) return;
        bool hasInstance = instance.TryOwnAttribute("replace", out _);
        if (instance.Class.TryLookup("replace", out _, out object? raw))
        {
            // 普通 property 是真实数据描述符，仍由 GetAttr 执行 getter；不误拦它。
            if (raw is PyProperty) return;
            if (Descriptor(raw, out PyClass descriptor))
            {
                bool data = descriptor.TryLookup("__set__", out _, out _)
                    || descriptor.TryLookup("__delete__", out _, out _);
                // 只拒绝旧路径可能错误成功的域：跳过 getter 调 raw，或数据描述符被实例值绕过。
                // 非数据描述符被实际实例值遮蔽时，Python 本来也不绑定它。
                if ((data && hasInstance) || (!hasInstance && descriptor.TryLookup("__call__", out _, out _)))
                    throw Unsupported("custom replace descriptor");
            }
            return;
        }
        // str 子类先有真实根 replace；只有无类值/实例值/根方法才会用 __getattr__。
        if (hasInstance || instance is PyStringInstance) return;
        if (instance.Class.TryLookup("__getattr__", out _, out object? fallback)
            && CallableDescriptor(fallback)) throw Unsupported("custom __getattr__ descriptor");
    }

    private static void GuardTruthSlot(object? quote)
    {
        if (quote is not PyInstance instance || quote is PyStringInstance) return;
        // 真值特殊槽在类上读取；有 __bool__ 就不检查未使用的 __len__，且检查不调用用户代码。
        if (instance.Class.TryLookup("__bool__", out _, out object? boolean))
        {
            if (CallableDescriptor(boolean)) throw Unsupported("custom __bool__ descriptor");
        }
        else if (instance.Class.TryLookup("__len__", out _, out object? length)
            && CallableDescriptor(length)) throw Unsupported("custom __len__ descriptor");
        // property 真值槽的旧错误失败保留 pending，不为本库扩充通用特殊槽框架。
    }

    private static bool Descriptor(object? raw, out PyClass descriptor)
    {
        if (raw is PyInstance value && value.Class.TryLookup("__get__", out _, out _))
        {
            descriptor = value.Class; return true;
        }
        descriptor = null!; return false;
    }

    private static bool CallableDescriptor(object? raw) => Descriptor(raw, out PyClass descriptor)
        && descriptor.TryLookup("__call__", out _, out _);
    private static PyBaseException Own(PyBaseException error) => (PyBaseException)PyErrors.PrepareRaise(error);
    private static PyBaseException Unsupported(string operation) => Own(new PyNotImplementedError($"html.escape: {operation} is not supported"));

    // 使用既有 native callable/type/descriptor 接口，不增全局分支或伪 Python 帧。
    private sealed class EscapeFunction : PyNativeObject, IPyNativeCallable, IPyNativeDescriptor
    {
        private readonly PyTuple publicDefaults = new([true]);
        public override string TypeName => "function";
        public object? Invoke(object?[] args, string[] names, object?[] values) => Escape(args, names, values);
        public override object? GetAttr(string name) => name switch
        {
            "__name__" or "__qualname__" => "escape",
            "__module__" => "html",
            "__defaults__" => publicDefaults,
            "__class__" => PyType.Get("function"),
            "__doc__" or "__dict__" or "__code__" or "__closure__" or "__annotations__" or "__kwdefaults__"
                or "__globals__" or "__get__" or "__reduce__" or "__reduce_ex__"
                => throw Unsupported("function reflection"),
            _ => throw Own(Missing(name)),
        };
        public object? Bind(PyClass owner, object? self) => self is null or PyNone ? this
            : throw Unsupported("binding escape as an instance method");
        public override void SetAttr(string name, object? value) => throw Unsupported("changing function metadata");
        public override void DelAttr(string name) => throw Unsupported("changing function metadata");
        public override string PythonRepr() => "<function escape>";
    }
}
