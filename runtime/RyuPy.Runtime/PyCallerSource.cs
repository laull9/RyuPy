// 不可变编译单元与同帧冷读取：代码身份、执行环境、Python模块元数据彼此独立。
// 这里只消费生成器已证明的载体；不按文件名、公开函数绑定或模块注册表反推来源。
using System;

namespace RyuPy.Runtime;

internal sealed class CompiledCodeSource(string name, string file, PyModuleGet? getter)
{
    internal readonly string InitialName = name;
    internal readonly string File = file;
    internal readonly PyModuleGet? LegacyGetter = getter;
    internal bool IsInstance => LegacyGetter is null;

    internal static CompiledCodeSource Require(object? value) => value as CompiledCodeSource
        ?? throw new PyRuntimeError("invalid compiled code source");
}

// 冷消费者的一次选帧结果；热压帧仍只写既有平行数组，不分配此记录。
internal readonly record struct PyCallerSource(PyStackFrame Frame, CompiledCodeSource? Code,
    PyDict? Globals, object? Origin, bool OutOfStack = false)
{
    private static readonly object OutsideEnvironment = new();
    internal bool Known => Code is not null || OutOfStack;
    internal object Environment => OutOfStack ? OutsideEnvironment : (object?)Globals ?? Code
        ?? throw new PyRuntimeError("caller has no known environment");

    // 仅受信生成getter的缺槽异常表示missing；其它异常绝不转为编译初名。
    internal bool TryReadName(out object? value)
    {
        if (OutOfStack) { value = "sys"; return true; }
        if (Globals is not null) return Globals.TryGet("__name__", out value);
        if (Code?.LegacyGetter is not { } getter)
            throw new PyRuntimeError("caller has no readable global environment");
        try { value = getter("__name__"); return true; }
        catch (PyAttributeError) { value = null; return false; }
    }

    internal object TypedDictModule()
    {
        if (OutOfStack) return "__main__";
        // Static只提供已验证代码/环境，没有当前可写function.Module；明确保旧消费算法。
        if (!Known || Origin is PySignature)
            return PyModules.ModuleForFile(Frame.File) ?? "__main__";
        object raw = Origin switch
        {
            PyFunction function => function.Module,
            ModuleExecutionSource module => module.EntryModule,
            _ => throw new PyRuntimeError("invalid TypedDict caller source"),
        };
        long depth = PyTraceback.CaptureDepth();
        object? context = PyErrors.EnterHandler(null);
        try
        {
            // 只有真值协议在窄catch内；source/env结构检查已在选帧时完成。
            try { return PyOps.Truthy(raw) ? raw : "__main__"; }
            catch (PyBaseException error) when (PyExceptionMatch.Match(error, PyType.Get("AttributeError"))) { }
        }
        catch (PyBaseException error)
        {
            // 原生辅助可能尚未附帧；必须在清理__bool__/__len__帧之前保存传播现场。
            // 这遵循既有普通runtime冷回调协议，不授予本消费者Trace V1能力。
            PyTraceback.Attach(error);
            throw;
        }
        finally
        {
            PyTraceback.PopTo(depth + 1);
            PyErrors.LeaveHandler(context);
        }
        // __bool__可能先修改globals再抛出；此处必须活读，且不再次求真值。
        if (!TryReadName(out object? fallback)) return "__main__";
        if (fallback is null or PyNone)
            throw new PyNotImplementedError("TypedDict metaclass default module filling is not supported");
        return fallback;
    }
}

public static partial class PyTraceback
{
    /// <summary>生成器冷登记每个实际代码单元；getter为空只表示Instance协议，不执行getter。</summary>
    public static object CreateCodeSource(string moduleName, string diagnosticFile, PyModuleGet? legacyGetter)
    {
        if (moduleName is null || diagnosticFile is null) throw new PyTypeError("code source requires names");
        return new CompiledCodeSource(moduleName, diagnosticFile, legacyGetter);
    }

    /// <summary>每次实际ModuleMain捕获入口值；Legacy保已证明前言初值，Instance取真实目标。</summary>
    public static object CreateModuleExecutionSourceWithCode(object source, object? instanceContext)
    {
        var code = CompiledCodeSource.Require(source);
        if (!code.IsInstance)
        {
            if (instanceContext is not (null or PyNone)) throw new PyRuntimeError("legacy code has instance context");
            return new ModuleExecutionSource(code.InitialName, code, null, code.InitialName);
        }
        if (instanceContext is not PyModuleExecutionContext context)
            throw new PyRuntimeError("instance code has no execution context");
        object entry = context.State.Globals.GetOrDefault("__name__", PyNone.Instance) ?? PyNone.Instance;
        return new ModuleExecutionSource(context.State, code, context.State.Globals, entry);
    }

    // 同一索引读取location与origin；越界独立标识，真实文件名sys不伪装越界。
    internal static PyCallerSource CallerSource(long stacklevel)
    {
        long offset = Math.Max(1, stacklevel);
        if (offset > _depth) return new(new PyStackFrame("sys", "", 1), null, null, null, true);
        int index = _depth - (int)offset;
        var frame = new PyStackFrame(_files[index], _functions[index], _lines[index]);
        object? origin = _origins[index];
        CompiledCodeSource? code;
        PyDict? globals = null;
        switch (origin)
        {
            case null: return new(frame, null, null, null);
            case PyFunction function:
                code = PyFunctionDefinition.Source(function.Signature);
                if (code is null) return new(frame, null, null, origin);
                if (code.IsInstance)
                    globals = PyFunctionOwner.GetContext(function).State.Globals;
                else if (function.Body.Target is PyFunctionOwner)
                    throw new PyRuntimeError("legacy source has instance function owner");
                break;
            case PySignature signature:
                // 裸signature帧是新编译协议；缺source绝不降级为旧Unknown。
                code = PyFunctionDefinition.Source(signature)
                    ?? throw new PyRuntimeError("signature frame has no compiled code source");
                if (code.IsInstance) throw new PyRuntimeError("instance frame requires an actual function");
                break;
            case ModuleExecutionSource module:
                code = module.Source;
                if (code is null) return new(frame, null, null, origin);
                globals = module.Globals;
                if (code.IsInstance != (globals is not null)
                    || globals is not null && (module.Module is not PyModuleInstanceState state
                        || !ReferenceEquals(state.Globals, globals)))
                    throw new PyRuntimeError("module source has inconsistent environment");
                break;
            default: throw new PyRuntimeError("invalid caller origin carrier");
        }
        // 诊断文件一致性是损坏检查，绝不是来源查找键或同名合并依据。
        if (!string.Equals(code.File, frame.File, StringComparison.Ordinal))
            throw new PyRuntimeError("caller frame and compiled code source disagree");
        return new(frame, code, globals, origin);
    }
}
