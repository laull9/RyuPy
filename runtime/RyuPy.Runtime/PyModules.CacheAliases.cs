// sys.modules 的受限新增别名冷路；只发布真实模块身份，不扩展替换、删除或导入生命周期。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

public static partial class PyModules
{
    // 只记录本入口的来源，不按可写 __name__/__spec__ 推断；未使用别名的程序不分配集合。
    // 与 Cache 同寿命且不逐出：每个合法新键增加一个强缓存引用和一个名字标记。
    private static HashSet<string>? CacheAliasNames;

    internal static object AddCacheAlias(object? key, object? value)
    {
        // 精确 CLR string 与 sealed PyModule 不触发 hash/eq/repr/属性或用户转换协议。
        if (key is not string name || name.Length == 0 || name.Contains('.'))
            throw CacheAliasUnsupported("only new nonempty undotted exact str keys are supported");
        if (value is not PyModule module)
            throw CacheAliasUnsupported("only real module values are supported");
        // 包含失败初始化后逃逸的 State=1 对象；State=0 的公开未执行实例必须保留原状。
        if (module.State == 1)
            throw CacheAliasUnsupported("modules being initialized cannot be aliased");
        foreach (string existing in Registry.Keys)
            if (ConflictsWithDeclaredBinding(existing, name))
                throw CacheAliasUnsupported("registered module names and their package prefixes cannot be replaced");
        foreach (string existing in Cache.Keys)
            if (ConflictsWithDeclaredBinding(existing, name))
                throw CacheAliasUnsupported("cached module names and their package prefixes cannot be replaced");

        // 所有语义检查完成后才发布；不改对象、状态、元数据、globals 或父包属性，也不执行正文。
        Cache.Add(name, module);
        (CacheAliasNames ??= new(StringComparer.Ordinal)).Add(name);
        return PyNone.Instance;
    }

    // 五处注册写路及声明预检均在任何位置解析/登记前调用，避免宿主随后偷偷覆盖别名图。
    private static void EnsureRegistrationAvoidsCacheAliases(string name)
    {
        if (IsCacheAliasOrDescendant(name))
            throw CacheAliasUnsupported("module registration cannot replace or extend an alias");
    }

    private static bool IsCacheAliasOrDescendant(string name)
    {
        if (CacheAliasNames is null) return false;
        int dot = name.IndexOf('.');
        return CacheAliasNames.Contains(dot < 0 ? name : name[..dot]);
    }

    // 只在未注册导入和公开 finder 冷路使用；普通缓存命中不增加分支或检查公开 spec。
    private static void RejectCacheAliasDescendant(string name)
    {
        if (IsCacheAliasOrDescendant(name))
            throw CacheAliasUnsupported("imports below an alias are not implemented");
    }

    internal static PyNotImplementedError CacheAliasUnsupported(string detail, PyBaseException? context = null) =>
        (PyNotImplementedError)PyErrors.PrepareRaise(new PyNotImplementedError("sys.modules aliases: " + detail)
            { Context = context });
}
