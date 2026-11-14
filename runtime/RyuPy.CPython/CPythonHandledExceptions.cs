// 两执行域的handled异常作用域；与待抛raised-error通道独立，嵌套调用严格按LIFO恢复。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private sealed class PythonHandledScope : IDisposable
    {
        private readonly nint previous;
        internal PythonHandledScope(CPythonHost host)
        {
            // GetHandledException返回新引用；SetHandledException借用入参并自行保活。
            previous = Native.rp_get_handled();
            try
            {
                // 已有外部实例由CP字段保持权威，正常handled注入不能用旧缓存覆盖CP刚修改的链。
                nint current = PyErrors.Current is not { } error ? 0
                    : error.ForeignObject is PythonObject origin ? origin.Pointer : host.ExportException(error);
                Native.rp_set_handled(current);
            }
            catch { Native.rp_decref(previous); throw; }
        }
        public void Dispose()
        {
            // shim在切换handled状态时保存raised通道，恢复不能覆盖本次正在传播的异常。
            Native.rp_set_handled(previous); Native.rp_decref(previous);
        }
    }

    private sealed class ManagedHandledScope : IDisposable
    {
        private readonly object? previous;
        internal ManagedHandledScope(CPythonHost host)
        {
            nint current = Native.rp_get_handled();
            // ExceptionFromOwned接收这一新引用；真实CP handler优先于编译调用者的旧上下文。
            PyBaseException? error = current == 0 ? null : host.ExceptionFromOwned(current);
            previous = PyErrors.ExchangeHandledException(error);
        }
        public void Dispose() => PyErrors.LeaveHandler(previous);
    }
}
