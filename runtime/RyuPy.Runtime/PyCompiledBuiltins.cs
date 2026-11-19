// 编译入口的可选 builtins 冷能力；仅适配权威环境及规范 callable 身份，不承担完整导入协议。
namespace RyuPy.Runtime;

/// <summary>外来编译入口使用的原生 builtins；普通 Runtime 模块继续使用原环境。</summary>
public interface IPyCompiledBuiltinsProvider
{
    /// <summary>显式 profile 就绪时返回同一权威字典；未启用时返回 false。</summary>
    bool TryGetCompiledBuiltins(out IPyModuleNamespace? builtins);
    /// <summary>只接受所属解释器的精确 dict 或真实模块，返回其原字典，不复制内容。</summary>
    bool TryGetCompiledBuiltinsNamespace(object value, out IPyModuleNamespace? builtins);
    /// <summary>比较初始化时保留的规范 builtin，用户替换的同名值不能通过守卫。</summary>
    bool IsCanonicalCompiledBuiltin(object? value, string name);
}
