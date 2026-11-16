// ADR-0221 Slice 1 子进程集成测试：覆盖 ProcessNativeBinding 不可逆状态机、
// 严格内核 math + atexit + 三阶段无盲区 complete=true 审计、顺序重建与真实 FiniCore tp_dealloc。
namespace RyuPy.CPython.Tests;

[Collection("StrictKernelSlice1")]
public sealed class StrictKernelSlice1Tests(StrictKernelBuildFixture fixture) : IClassFixture<StrictKernelBuildFixture>
{
    private void RunScenario(string scenario)
    {
        Assert.True(fixture.Available, "严格 CPython 3.12.13 内核源码包或构建环境未就绪");
        int exit = fixture.RunSubprocess(scenario, out string stdout, out string stderr);
        Assert.True(exit == 0, $"子进程场景 {scenario} 失败 (exit={exit}):\nstdout:\n{stdout}\nstderr:\n{stderr}");
    }

    [Fact]
    public void StrictKernel_MinimalMathAndAtexitProducesCompleteZeroFrameAudit() =>
        RunScenario("strict_math_atexit_audit");

    [Fact]
    public void StrictKernel_FiniCoreFenceRunsRealNativeDeallocBeforeInterpreterDestroy() =>
        RunScenario("strict_fini_core_real_dealloc");

    [Fact]
    public void StrictKernel_SameTripleSequentialRebuildSucceedsCleanly() =>
        RunScenario("strict_sequential_rebuild_same_triple");

    [Fact]
    public void ProcessNativeBinding_RejectsCompatToStrictSwitchInSameProcess() =>
        RunScenario("neg_compat_then_strict_rejected");

    [Fact]
    public void ProcessNativeBinding_RejectsStrictToCompatSwitchInSameProcess() =>
        RunScenario("neg_strict_then_compat_rejected");

    [Fact]
    public void ProcessNativeBinding_RejectsStrictPathChangeAfterClose() =>
        RunScenario("neg_strict_path_change_after_closed_rejected");

    [Fact]
    public void ProcessNativeBinding_FakeKernelEntersIrreversibleFailedState() =>
        RunScenario("neg_fake_kernel_triggers_terminal_failed");

    [Fact]
    public void StrictKernel_FiniCoreFenceAllowsReleaseOp0WhileBlockingNewExecution() =>
        RunScenario("neg_fence_blocks_new_leases_allows_op0");

    [Fact]
    public void StrictKernel_MidSessionDisable0ThenReenable1FailsAudit() =>
        RunScenario("neg_strict_toggle_0_to_1_breaks_audit");

    [Fact]
    public void StrictKernel_RuntimeResetEvalStatsFailsAudit() =>
        RunScenario("neg_runtime_reset_eval_stats_breaks_audit");

    [Fact]
    public void StrictKernel_ReplacingCilCallbacksBeforeFreezeKeepsHostActiveAndRunsFenceOnceAtStop() =>
        RunScenario("strict_replace_cil_callbacks_keeps_host_active");

    [Fact]
    public void ProcessNativeBinding_RejectsCompatSequentialRebuildExplicitly() =>
        RunScenario("neg_compat_rebuild_rejected");
}
