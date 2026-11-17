// 静态CIL业务体中的frame相关builtin按真实对象身份分发：globals返回原F的D，显式环境能力保留。
// 活动环境只存在本线程调用栈；规范对象由F的原生GC owner保活，用户同名替身不取得特殊权限。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    [ThreadStatic] private static StockThinFunctionCallLease? currentStockBody;
    private readonly struct StockBodyScope : IDisposable
    {
        private readonly StockThinFunctionCallLease? previous;
        internal StockBodyScope(StockThinFunctionCallLease body)
        { previous = currentStockBody; currentStockBody = body; }
        public void Dispose() => currentStockBody = previous;
    }
    private StockThinFunctionCallLease? ActiveStockBody =>
        (stockProgramExecuting || state == 5) ? currentStockBody : null;

    // Runtime连续buffer只在已证实source位置导出字节快照；真实CP buffer仍交原binder读取一次。
    internal object?[] PrepareStockFrameArguments(PythonObject actual, object?[] args)
    {
        if (args.Length == 0 || args[0] is not (PyByteArray or PyMemoryView)
            || ActiveStockBody is not { } body) return args;
        int kind = body.FrameTarget(actual.Pointer, out nint bound);
        if (kind is not (3 or 4 or 5) || bound != 0) return args;
        object?[] converted = [.. args];
        converted[0] = args[0] is PyByteArray array ? array.ToBytes()
            : new PyBytes(((PyMemoryView)args[0]!).CopyBuffer());
        return converted;
    }
    internal object? PrepareStockFrameKeyword(PythonObject actual, string name, object? value)
    {
        if (name != "source" || value is not (PyByteArray or PyMemoryView) || ActiveStockBody is not { } body)
            return value;
        if (body.FrameTarget(actual.Pointer, out nint bound) != 5 || bound != 0) return value;
        return value is PyByteArray array ? array.ToBytes() : new PyBytes(((PyMemoryView)value).CopyBuffer());
    }
    internal bool TryInvokeStockFrameBuiltin(PythonObject actual, nint args, nint kwargs, out object? result)
    {
        result = null;
        if (ActiveStockBody is not { } body) return false;
        int action = body.FrameCall(actual.Pointer, args, kwargs);
        if (action == 0) return false;
        if (action == 1) { result = body.GlobalsForBuiltin(); return true; }
        string name = action switch { 2 => "locals", 3 => "vars", 4 => "eval", 5 => "exec", _ => "compile" };
        throw new PyNotImplementedError(action switch
        {
            4 or 5 => $"stock CIL {name}() without explicit globals requires a materialized business frame",
            7 => "stock CIL compile() with frame-dependent parameter protocols requires a materialized business frame",
            6 => "stock CIL compile() inheriting caller flags requires a materialized business frame; use dont_inherit=True",
            _ => $"stock CIL {name}() requires a materialized business frame",
        });
    }
}
