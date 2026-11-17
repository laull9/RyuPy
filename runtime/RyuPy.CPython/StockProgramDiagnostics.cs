// 封闭编译程序的异常/退出报告拥有独立短scope；公共caller不能铸造Runtime诊断请求。
// 入口外getter仍属于外部owned；scope仅确定退休本次Report新建的流/格式化metadata。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost : IPyDiagnosticScopeProvider
{
    /// <inheritdoc />
    public IDisposable? EnterDiagnosticScope(PyDiagnosticScopeRequest request)
    {
        if (request is null || !stockProgramAuthorized || Environment.CurrentManagedThreadId != ownerThread
            || state is not (1 or 5) || !request.TryConsume(this)) return null;
        Lease lease = Enter(drainReferences: false);
        try
        {
            var scope = new ProgramOwnedScope(this, stockProgramCallback);
            stockProgramCallback = scope;
            try { Drain(); } catch { scope.Dispose(); throw; }
            return new DiagnosticLease(lease, scope);
        }
        catch { lease.Dispose(); throw; }
    }
    private sealed class DiagnosticLease(Lease lease, ProgramOwnedScope scope) : IDisposable
    {
        private int disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            try { scope.Dispose(); } finally { lease.Dispose(); }
        }
    }
}
