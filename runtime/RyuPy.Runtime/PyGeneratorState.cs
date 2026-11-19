// 字段驻留生成器的私有控制载体；Python对象仍为PyGenerator，PC不装箱，用户局部由生成类型保存。
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

internal sealed class PyGeneratorState(object payload, long pc)
{
    internal object? Payload = payload;
    internal long PC = pc;
    internal object? Sent;
    internal object? Injected;

    internal void Clear()
    {
        Payload = null;
        Sent = null;
        Injected = null;
        PC = -1;
    }
}

public static partial class PyOps
{
    /// <summary>编译恢复入口取得自己的字段载体，不执行Python协议。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object GeneratorStateData(object state) => ((PyGeneratorState)state).Payload!;

    /// <summary>读取原生PC；状态仅由生成代码与PyGenerator持有。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GeneratorStateGetPC(object state) => ((PyGeneratorState)state).PC;

    /// <summary>发布下一个恢复位置或完成标记。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GeneratorStateSetPC(object state, long pc) => ((PyGeneratorState)state).PC = pc;
}
