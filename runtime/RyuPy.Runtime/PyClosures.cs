// 共享 cell 与闭包环境的读写；空 cell 按访问作用域抛出对应的 Python 名称异常。
using System;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>
/// 闭包 cell（M3.0）。
/// </summary>
/// <remarks>
/// 被嵌套函数捕获的局部变量装进 <c>object[1]</c>：所有共享它的闭包持有同一个数组，
/// 读写都落在唯一的槽位上——这正是 Python「闭包捕获变量本身而不是值快照」的语义。
/// 「未赋值」用 <see cref="PyUnbound"/> 哨兵表示，读取时抛 <c>UnboundLocalError</c>。
/// 闭包函数的 env 是 <c>object[]</c>：元素就是外层作用域的这些 cell，
/// 嵌套函数读取时直接按下标取出再读槽位。
/// </remarks>
public static partial class PyOps
{
    /// <summary>创建一个 cell：长度 1 的数组，槽位放「未赋值」哨兵。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object CellNew() => new object?[] { PyUnbound.Instance };

    /// <summary>读取 cell（本作用域的 cell 局部变量）。</summary>
    /// <param name="cell"><see cref="CellNew"/> 创建的 <c>object[1]</c>。</param>
    /// <param name="name">变量名，用于错误消息。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object CellGet(object cell, string name)
    {
        var slot = (object?[])cell;
        object? value = slot[0];
        if (value is null or PyUnbound)
        {
            throw new PyUnboundLocalError(
                $"cannot access local variable '{name}' where it is not associated with a value"
            );
        }
        return value;
    }

    /// <summary>写入 cell（本作用域的 cell 局部变量）。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CellSet(object cell, object? value)
    {
        ((object?[])cell)[0] = value;
    }

    /// <summary>
    /// 读取外层 cell（嵌套函数的自由变量）：env 槽位里是外层 cell，
    /// 未赋值时的错误消息与 CPython 一致（"free variable ... in enclosing scope"）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object CellGetFree(object env, long index, string name)
    {
        var cells = (object?[])env;
        var slot = (object?[])cells[index]!;
        object? value = slot[0];
        if (value is null or PyUnbound)
        {
            throw new PyNameError(
                $"cannot access free variable '{name}' where it is not associated with a value in enclosing scope"
            );
        }
        return value;
    }
}
