// 静态CIL业务体中的frame相关builtin按真实对象身份分发：globals返回原F的D，显式环境能力保留。
// 活动环境只存在本线程调用栈；规范对象由入口owned保活，用户同名替身不取得特殊权限。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private readonly Dictionary<string, PythonObject> stockFrameBuiltins = new(StringComparer.Ordinal);
    [ThreadStatic] private static StockThinFunctionCallLease? currentStockBody;
    private readonly struct StockBodyScope : IDisposable
    {
        private readonly StockThinFunctionCallLease? previous;
        internal StockBodyScope(StockThinFunctionCallLease body)
        { previous = currentStockBody; currentStockBody = body; }
        public void Dispose() => currentStockBody = previous;
    }
    private void CaptureStockFrameBuiltins(PythonObject globals)
    {
        try
        {
            stockFrameBuiltins.Add("globals", globals);
            foreach (string name in new[] { "locals", "vars", "eval", "exec", "compile" })
                stockFrameBuiltins.Add(name, new PythonObject(this, Check(Native.rp_builtin(name))));
        }
        catch
        {
            foreach (PythonObject owner in stockFrameBuiltins.Values) owner.Dispose();
            stockFrameBuiltins.Clear(); throw;
        }
    }
    internal bool TryInvokeStockFrameBuiltin(PythonObject actual, object?[] args,
        string[] names, object?[] values, out object? result)
    {
        result = null;
        if (currentStockBody is not { } body || actual.IsDisposed) return false;
        string? name = null;
        foreach (var pair in stockFrameBuiltins)
            if (!pair.Value.IsDisposed && pair.Value.Pointer == actual.Pointer) { name = pair.Key; break; }
        if (name is null) return false;
        if (name == "globals" && args.Length == 0 && names.Length == 0)
        { result = body.GlobalsForBuiltin(); return true; }
        if (name is "locals" or "vars" && args.Length == 0 && names.Length == 0)
            throw new PyNotImplementedError($"stock CIL {name}() requires a materialized business frame");
        // 错误形状交真实builtin完成其原参数/类型诊断；仅有效源码的隐式环境形状需要业务frame。
        if (name is "eval" or "exec" && args.Length is >= 1 and <= 3
            && names.All(keyword => keyword is "globals" or "locals")
            && !(args.Length >= 2 && names.Contains("globals"))
            && !(args.Length >= 3 && names.Contains("locals"))
            && IsFrameSource(args[0]))
        {
            int globalsKeyword = Array.IndexOf(names, "globals");
            object? globals = args.Length >= 2 ? args[1] : globalsKeyword >= 0 ? values[globalsKeyword] : null;
            if (globals is null or PyNone)
                throw new PyNotImplementedError($"stock CIL {name}() without explicit globals requires a materialized business frame");
        }
        if (name == "compile" && args.Length is >= 3 and <= 7 && IsFrameSource(args[0]))
        {
            int keyword = Array.IndexOf(names, "dont_inherit");
            object? inherit = args.Length >= 5 ? args[4] : keyword >= 0 ? values[keyword] : false;
            // 显式非零值及需真实__index__协议的参数仍由CP binder处理；默认/精确false缺少caller flags来源。
            if (inherit is false or 0 or 0L && !(args.Length >= 5 && keyword >= 0))
                throw new PyNotImplementedError("stock CIL compile() inheriting caller flags requires a materialized business frame; use dont_inherit=True");
        }
        return false;
    }
    private static bool IsFrameSource(object? source) => source is string or PyBytes or PyByteArray
        || source is PythonObject foreign && !foreign.IsDisposed && Native.rp_frame_source(foreign.Pointer) != 0;
}
