// provider反向回调的冷来源边界：新增可信CIL帧才可覆盖当前Python现场，普通协议保原frame。
// 只在跨域调用栈保存两个整数；普通CIL Invoke、PushFrame和无provider程序无需读取此状态。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    [ThreadStatic] private static bool carrierCallbackActive;
    [ThreadStatic] private static long carrierCallbackDepth;
    private static bool HasNewCompiledCarrierFrame => carrierCallbackActive
        && PyTraceback.CaptureDepth() > carrierCallbackDepth;
    // CLR展开后的诊断帧仍供异常Attach读取，但错误导出阶段不能把它们当活动业务调用者。
    private static void SuppressRetiredCarrierFrames()
    { carrierCallbackActive = true; carrierCallbackDepth = PyTraceback.CaptureDepth(); }

    internal readonly struct ManagedCarrierCallbackScope : IDisposable
    {
        private readonly bool previousActive;
        private readonly long previousDepth;
        internal ManagedCarrierCallbackScope(long depth)
        {
            previousActive = carrierCallbackActive; previousDepth = carrierCallbackDepth;
            carrierCallbackActive = true; carrierCallbackDepth = depth;
        }
        public void Dispose()
        { carrierCallbackActive = previousActive; carrierCallbackDepth = previousDepth; }
    }
}
