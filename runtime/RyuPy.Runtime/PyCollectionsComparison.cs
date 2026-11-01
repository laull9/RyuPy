using System;

namespace RyuPy.Runtime;

/// <summary>
/// 新原生容器的相等性不经过编译函数的 traceback 递归守卫；用线程局部深度防止
/// 直接循环容器导致不可捕获的 CLR StackOverflow。保守上限256，不改变普通容器旧路径。
/// </summary>
internal static class PyCollectionsComparison
{
    [ThreadStatic] private static int _depth;
    internal static Scope Enter()
    {
        if (_depth >= 256) throw new PyRecursionError("maximum recursion depth exceeded in collections comparison");
        _depth++;
        return new Scope();
    }
    // using 的 finally 路径无论正常返回还是用户比较抛错都归还深度；结构体不产生堆分配。
    internal readonly struct Scope : IDisposable
    {
        public void Dispose() => _depth--;
    }
}
