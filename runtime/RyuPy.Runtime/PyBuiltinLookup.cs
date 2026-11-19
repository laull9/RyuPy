// 静态默认builtins域的冷读取：只读真实共享字典，不缓存、不替换实际绑定或改变实例owner。
namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>读取默认builtins当前值；实例模块仍使用其捕获context的LoadName。</summary>
    public static object? LoadDefaultBuiltin(string name)
    {
        if (PyModuleBuiltins.Default.TryGet(name, out object? value)) return value;
        // 缺失的已支持名称是NameError；不能复用会把全部已知builtin拒绝为NI的旧槽检查。
        if (PyModuleBuiltins.IsUnsupported(name))
            throw PyTraceProduct.NativeFresh(new PyNotImplementedError($"builtin '{name}' is not implemented"));
        throw PyTraceProduct.NativeFresh(new PyNameError($"name '{name}' is not defined"));
    }
}
