// 外部 provider 的冷只读注册事实；不触发导入、模块初始化或用户公开属性协议。
namespace RyuPy.Runtime;

public static partial class PyModules
{
    /// <summary>读取已有明确包标记；旧无标记注册与未知名字返回 false，不猜公开 __package__。</summary>
    /// <param name="name">已冻结的绝对登记名。</param>
    /// <param name="isPackage">查询成功时的真实包标记。</param>
    public static bool TryGetRegisteredPackage(string name, out bool isPackage)
    {
        isPackage = false;
        if (!Registry.TryGetValue(name, out Registration? registration)) return false;
        if (registration.ResourceLocation is { } source) { isPackage = source.IsPackage; return true; }
        if (registration.InstanceCode is { } code) { isPackage = code.IsPackage; return true; }
        return false;
    }
}
