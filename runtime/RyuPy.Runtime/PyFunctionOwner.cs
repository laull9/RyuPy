// 显式模块实例的函数归属：仅新路径绑定委托target，旧函数布局和调用入口保持不变。
// owner与闭包cell独立；定义点捕获真实字典引用，调用时不查注册名、线程环境或弱表。
namespace RyuPy.Runtime;

/// <summary>显式实例函数thunk；context不属于Python参数，self仍是真实函数身份。</summary>
public delegate object? PyOwnedCall(object context, PyFunction self, object?[] args,
    string[] kwNames, object?[] kwValues);

// 只有显式实例factory分配此载体；共享代码委托不反向持有实例。
internal sealed class PyFunctionOwner(PyModuleExecutionContext context, PyOwnedCall body)
{
    internal readonly PyModuleExecutionContext Context = context;
    private readonly PyOwnedCall _body = body;

    internal object? Invoke(PyFunction self, object?[] args, string[] kwNames, object?[] kwValues) =>
        _body(Context, self, args, kwNames, kwValues);

    // 只在反射属性冷路识别委托target；普通Invoke和名称读写均不增加检测。
    internal static PyModuleExecutionContext GetContext(PyFunction function) =>
        function.Body.Target is PyFunctionOwner owner ? owner.Context
            : throw new PyNotImplementedError("function global environment is not available for static functions");
}

public static partial class PyModuleInstances
{
    /// <summary>真实def时捕获当前globals中的builtins，不能继承调用者过期的捕获。</summary>
    public static object CaptureDefinitionContext(object callerContext)
    {
        var caller = Context(callerContext);
        // CP缺键继承当前frame且不插键；这与模块Execute进入时补默认字典不同。
        if (!caller.State.Globals.TryGet("__builtins__", out object? value))
            return caller;
        // 本切仅真实PyDict；CP还允许module/其它对象并可能晚错，此处明确较早NI。
        if (value is not PyDict builtins)
            throw new PyNotImplementedError("instance function __builtins__ must be a dictionary");
        // context不可变且无Python可观察身份；同引用无需再分配，重绑才创建新捕获。
        return ReferenceEquals(caller.Builtins, builtins) ? caller
            : new PyModuleExecutionContext(caller.State, builtins);
    }

    /// <summary>消费已经捕获的context，不在默认值或装饰器副作用之后二次选择builtins。</summary>
    public static PyFunction CreateOwnedFunction(object capturedContext, string name, PySignature signature,
        PyOwnedCall body, object?[] defaults, bool usesSuper, object?[]? cells)
    {
        var owner = new PyFunctionOwner(Context(capturedContext), body);
        return new PyFunction(name, signature, owner.Invoke, defaults, usesSuper, cells);
    }

    /// <summary>定义元数据只读取真实globals，不把builtin或可变函数__module__当来源。</summary>
    public static object? DefinitionModule(object context)
    {
        if (Context(context).State.Globals.TryGet("__name__", out object? value)) return value;
        return PyNone.Instance;
    }
}
