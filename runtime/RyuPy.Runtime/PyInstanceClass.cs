// 实例普通class语句的有限入口：先核外frame的真实hook，再以独立class context构造。
// 不执行自定义__build_class__或特殊类布局；公开直接调用仍明确不支持classbody函数协议。
namespace RyuPy.Runtime;

internal static class PyInstanceClassHook
{
    // 稳定真实callable身份独立于可变默认字典；用户覆盖后不能把覆盖值认成canonical。
    internal static readonly PyFunction Function = Create();

    private static PyFunction Create()
    {
        var signature = PySignature.Create("__build_class__", ["args", "kwargs"], ["varpos", "varkw"], ["0", "0"]);
        var function = new PyFunction("__build_class__", signature,
            static (_, _, _, _) => throw new PyNotImplementedError(
                "direct __build_class__ calls require an unsupported class body function protocol"), [], false);
        function.CaptureModule("builtins");
        return function;
    }
}

public static partial class PyModuleInstances
{
    /// <summary>class表达式求基类前消费外frame的builtins hook；不查globals同名覆盖。</summary>
    public static void ClassGuard(object outerContext)
    {
        if (!Context(outerContext).TryBuiltin("__build_class__", out object? hook))
            throw new PyNameError("__build_class__ not found");
        if (!ReferenceEquals(hook, PyInstanceClassHook.Function)
            && !(PyProvider.Current is IPyCompiledBuiltinsProvider provider
                && provider.IsCanonicalCompiledBuiltin(hook, "__build_class__")))
            throw new PyNotImplementedError("custom __build_class__ hooks are not implemented for instance modules");
    }

    /// <summary>已验证canonical hook的编译类体入口；只消费已捕获context，不再选择hook/builtins。</summary>
    public static object CreateClass(object capturedContext, string name, object?[] bases)
    {
        var context = Context(capturedContext);
        if (bases.Length > 1) throw UnsupportedBase();
        if (bases.Length == 1) ValidateOrdinaryBase(bases[0]);
        // stock入口的普通类直接构造真实CP类型；object不是业务委派，类体仍由CIL顺序执行。
        if (context.State.Code is { StockEntry: true } or { StockModule: true }
            && (bases.Length == 0 || ReferenceEquals(bases[0], PyBaseObject.Type)))
            bases = [PyProvider.Current is IPyCompiledClassProvider provider
                ? provider.GetDefaultClassBase()
                : throw new PyNotImplementedError("compiled class provider cannot supply the default object base")];
        var cls = (PyClass)PyOps.MakeClassCore(name, bases, null, captureAmbientMetadata: false);
        // 类体的LOAD_NAME与函数__module__捕获不同：缺global可回退本class frame的builtins。
        try
        {
            if (PyForeignClassConstruction.TryGet(cls, out IPyForeignClassDefinition definition))
            {
                object? module = definition.Contains("__name__") ? definition.Get("__name__") : LoadName(context, "__name__");
                cls.SetAttr("__module__", module ?? PyNone.Instance);
                cls.SetAttr("__qualname__", name);
            }
            else
            {
                cls.Attributes["__module__"] = LoadName(context, "__name__") ?? PyNone.Instance;
                cls.Attributes["__qualname__"] = name;
            }
        }
        catch { PyForeignClassConstruction.Abort(cls); throw; }
        return cls;
    }

    private static void ValidateOrdinaryBase(object? value)
    {
        if (ReferenceEquals(value, PyBaseObject.Type)) return;
        if (value is IPyForeignObject foreign)
        {
            if (!PyForeignClassBridge.IsType(foreign)) throw UnsupportedBase();
            return;
        }
        if (value is not PyClass cls) throw UnsupportedBase();
        // 复用已有冷路对完整继承链、循环、异常/native/Enum/dataclass的验证。
        PyRuntimeBases.Validate(cls);
        for (PyClass? current = cls; current is not null; current = current.BaseClass)
            if (current.HasStringLayout || current.HasListLayout || current.HasDictLayout
                || current.IsTypingGenericFamily || current.IsTypingIoFamily || current.NamedTupleLayout is not null)
                throw UnsupportedBase();
    }

    private static PyNotImplementedError UnsupportedBase() =>
        new("instance classes currently require an ordinary user class or object base");
}
