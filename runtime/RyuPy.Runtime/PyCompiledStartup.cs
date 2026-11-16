// 编译产品的基础启动ABI；不引用inspect工厂，原宿主Initialize/Register继续完整登记。
namespace RyuPy.Runtime;

public static partial class PySystem
{
    /// <summary>driver已冻结无inspect需求时使用；真实argv、sys、流与其它模块保持原初始化。</summary>
    public static void InitializeCompiledBase(string sourceName, string[] args)
    {
        InitializeState(sourceName, args);
        PyNativeModules.RegisterBase();
    }
}

// 此holder只由旧完整登记路径引用；不能让基础入口的类型初始化间接保留inspect可达图。
internal static class PyNativeInspectRegistration
{
    private static bool registered;
    internal static void Ensure()
    {
        if (registered) return;
        PyInspectModule.Register();
        registered = true;
    }
}
