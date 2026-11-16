// 模块裸名删除：静态槽先检查绑定，实例只删真实 globals；删除后读取才允许内建回退。
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>删除有类型模块槽前只检查绑定位，不读取或删除任何内建名称。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckModuleDelete([DoesNotReturnIf(false)] bool bound, string name)
    {
        if (!bound) throw new PyNameError($"name '{name}' is not defined");
    }

    /// <summary>删除动态模块槽前检查未绑定哨兵；None、零值及任意其它绑定均合法。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckModuleDeleteValue(object? value, string name)
    {
        // 不能复用 LoadName 或 CheckGlobalBound，删除缺失的 len / Ellipsis 仍应是 NameError。
        if (value is null or PyUnbound) CheckModuleDelete(false, name);
    }

    /// <summary>已准入的缺绑定回退读取（删除名及map兼容分支）：保留真实值与默认字典。</summary>
    public static object LoadDeletedModuleName(object? value, string name)
    {
        if (value is not null and not PyUnbound) return value;
        // 不缓存或复制内建值，也不把 Default 加入 PyOps 的静态初始化路径。
        if (PyModuleBuiltins.Default.TryGet(name, out object? builtin)) return builtin!;
        // map冷回退已闭合；实际默认键被删除后按支持域报NameError，不谎称未实现。
        if (name == "map") return LoadDefaultBuiltin(name)!;
        // 维持其它既有未实现内建与未知名称的错误分类，不改变普通槽读取的热路径。
        CheckGlobalBound(false, name);
        return value!;
    }
}

public static partial class PyModuleInstances
{
    /// <summary>删除当前执行实例的真实全局键；不查询 builtins、注册表或其它实例。</summary>
    public static void DeleteName(object context, string name)
    {
        if (!Context(context).State.Globals.DeleteIfPresent(name))
            throw new PyNameError($"name '{name}' is not defined");
    }
}
