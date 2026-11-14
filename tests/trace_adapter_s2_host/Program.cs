// 真实生成Module.Main的共享静态入口；Native编译不包含PE/Assembly.Location/反射序列化。
using RyuPy.Runtime;

internal static class Program
{
    private static int Main(string[] args)
    {
#if TRACE_NATIVE_ENTRY
        if (args.Length != 2) throw new ArgumentException("Native需要case-name和evidence-directory");
#else
        if (args.Length != 6) throw new ArgumentException("managed需要case-name、evidence-directory和四个冻结PE参数");
#endif
        if (args[0] is not ("explicit_inner" or "explicit_module" or "recursive" or "reused" or "native_local" or "native_outer"))
            throw new ArgumentException("未知冻结case-name");
        Directory.CreateDirectory(args[1]);
        string stage = "managed-inputs";
        Exception? failure = null;
        Exception? disposeFailure = null;
        TraceAdapterScope? scope = null;
        try
        {
#if !TRACE_NATIVE_ENTRY
            ManagedInputChecks.Run(args);
#endif
            stage = "module-main";
            ObservationWriter.Status(args[1], stage, null, null);
            scope = (TraceAdapterScope)PyTraceAdapter.EnterScope();
            int code = RyuPy.Generated.Module.Main([]);
            // 先保真实结果/事件，再运行可能失败的身份与语义断言。
            ObservationWriter.Capture(args[1], scope);
            stage = "execution-checks";
            ExecutionChecks.Check(scope, args[0], code);
        }
        catch (Exception error) { failure = error; }
        finally
        {
            if (scope is not null)
            {
                try { ObservationWriter.Capture(args[1], scope); }
                catch (Exception error) { failure ??= error; }
                try { scope.Dispose(); }
                catch (Exception error) { disposeFailure = error; }
            }
        }
        if (failure is null && disposeFailure is null)
        {
            try
            {
                stage = "manual-boundaries";
                ExecutionChecks.Boundaries(args[1]);
                stage = "passed";
            }
            catch (Exception error) { failure = error; }
        }
        ObservationWriter.Status(args[1], stage, failure?.ToString(), disposeFailure?.ToString());
        if (failure is not null || disposeFailure is not null)
        {
            if (failure is not null) Console.Error.WriteLine(failure.ToString());
            if (disposeFailure is not null) Console.Error.WriteLine("Dispose: " + disposeFailure);
            return 1;
        }
        Console.WriteLine(ObservationWriter.ActualText(args[1]));
        return 0;
    }
}
