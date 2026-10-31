using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>
/// 动态运算 / 调用的内联缓存（PIC，M2.9）。
/// </summary>
/// <remarks>
/// # 为什么需要它
///
/// 动态路径上的每个 <c>a + b</c>、<c>obj.method()</c> 都要经过一次完整分派：
/// 类型判断、魔术方法探测、类属性查找、绑定方法分配。对热点循环来说这些开销
/// 与真正的计算无关。PIC 把「上一次在这个调用点看到什么形状」记住，下一次直接走
/// 对应快路径。
///
/// # 边界与失效
///
/// <list type="bullet">
/// <item>每个调用点最多记 <see cref="EntriesPerSite"/> 条；容量固定（<see cref="MaxSites"/>），
/// 超限或形状不再命中就回到 <see cref="PyOps"/> 的通用路径，不报错也不改变语义；</item>
/// <item>方法调用缓存同时校验类修改版本与实例属性版本：替换方法、增删实例属性都会让
/// 条目失效，下一次重新查找；</item>
/// <item>模块属性不做缓存——<c>PyModule</c> 的 getter 本身就是静态槽位读取，
/// 模块重绑定必须立即可见；</item>
/// <item><c>RYUPY_PIC=0</c> 可整体关闭，用于与通用路径做 A/B 对照。</item>
/// </list>
///
/// 缓存只影响**性能**，任何未命中都完整执行通用路径，因此不会掩盖语义差异。
/// </remarks>
public static partial class PyPic
{
    /// <summary>最大调用点数；超出后这些位置直接使用通用路径。</summary>
    private const int MaxSites = 4096;

    /// <summary>每个调用点保留的条目数。</summary>
    /// <remarks>
    /// 4 条足以覆盖常见的小规模多态（同一调用点轮换 3–4 个子类）；更多形态时按轮转替换，
    /// 不会无限增长。必须是 2 的幂（<see cref="NextVictim"/> 用位与取模）。
    /// </remarks>
    private const int EntriesPerSite = 4;

    // 每张缓存表一份「下一个被替换的条目」计数器：命中失败时按站点轮转写入，
    // 使同一站点的多种形态分布在不同条目里（M2.11 前的实现固定写同一格，实为单态缓存）。
    private static readonly byte[] CallVictims = new byte[MaxSites];
    private static readonly byte[] MethodVictims = new byte[MaxSites];
    private static readonly byte[] GetAttrVictims = new byte[MaxSites];
    private static readonly byte[] SetAttrVictims = new byte[MaxSites];

    /// <summary>站点 <paramref name="site"/> 本次应写入的条目下标（含站点基址），并推进轮转计数。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int NextVictim(byte[] victims, int site)
    {
        int offset = victims[site]++ & (EntriesPerSite - 1);
        return site * EntriesPerSite + offset;
    }

    private static readonly bool Enabled = !string.Equals(
        Environment.GetEnvironmentVariable("RYUPY_PIC"),
        "0",
        StringComparison.Ordinal
    );

    // ───────────────────────── 二元运算 ─────────────────────────

    /// <summary>|x| 不超过 2^53 的整数可以无损转成 double。</summary>
    private const long ExactDoubleLimit = 1L << 53;

    /// <summary>带调用点编号的二元运算入口。</summary>
    /// <remarks>
    /// 数值形态直接按 CLR 类型分派，不再查调用点缓存：M2.11 的形状缓存命中与否都执行同一个
    /// 分派函数，只多了一次表查找。现在的顺序是：
    ///
    /// <list type="number">
    /// <item><c>long ⊕ long</c>：加减乘、整除 / 取模、位运算；溢出由 <see cref="PyInt"/> 升级 BigInteger；</item>
    /// <item><c>double</c> 参与的四则运算（含 <c>long</c> 提升）；除数为 0 时交给通用路径抛 ZeroDivisionError；</item>
    /// <item><c>str + str</c>；</item>
    /// <item>其余全部交给 <see cref="PyOps.Binary"/>，并携带原操作码：增量赋值的标记位在那里
    /// 分派到原地路径（list / set / 实例...），错误消息里的 <c>+=</c> 也由它生成。</item>
    /// </list>
    ///
    /// <c>site</c> 参数保留为 ABI，便于以后对非数值形态重新引入按站点缓存。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Binary(int site, int op, object a, object b)
    {
        // 不可变数值的增量赋值与普通运算完全相同，先剥掉标记位走同一条快路径
        int baseOp = op & ~PyOps.InPlaceFlag;
        if (a is long x)
        {
            if (b is long y)
            {
                if (TryLongBinary(baseOp, x, y, out object? result))
                {
                    return result!;
                }
            }
            else if (b is double dy && Enabled)
            {
                if (TryDoubleBinary(baseOp, x, dy, out object? result))
                {
                    return result!;
                }
            }
        }
        else if (a is double dx && Enabled)
        {
            if (b is double dy)
            {
                if (TryDoubleBinary(baseOp, dx, dy, out object? result))
                {
                    return result!;
                }
            }
            else if (b is long y && TryDoubleBinary(baseOp, dx, y, out object? result))
            {
                return result!;
            }
        }
        else if (a is string left && b is string right && baseOp == PyOps.Add && Enabled)
        {
            return string.Concat(left, right);
        }
        return PyOps.Binary(op, a, b);
    }

    /// <summary><c>long ⊕ long</c> 快路径；不处理的运算（<c>/</c>、<c>**</c>、移位）返回 false。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryLongBinary(int op, long x, long y, out object? result)
    {
        switch (op)
        {
            case PyOps.Add:
                result = PyInt.AddI64(x, y);
                return true;
            case PyOps.Sub:
                result = PyInt.SubtractI64(x, y);
                return true;
            case PyOps.Mul:
                result = PyInt.MultiplyI64(x, y);
                return true;
            case PyOps.FloorDiv when y > 0 && x >= 0:
                // 两侧非负时向下取整与截断一致；其它符号组合交给 PyInt 修正
                result = x / y;
                return true;
            case PyOps.Mod when y > 0 && x >= 0:
                result = x % y;
                return true;
            case PyOps.TrueDiv when y != 0
                && x > -ExactDoubleLimit && x < ExactDoubleLimit
                && y > -ExactDoubleLimit && y < ExactDoubleLimit:
                // 两侧都能精确转成 double 时，IEEE 除法就是正确舍入的真除法（与 CPython 相同）
                result = (double)x / y;
                return true;
            case PyOps.BitAnd:
                result = x & y;
                return true;
            case PyOps.BitOr:
                result = x | y;
                return true;
            case PyOps.BitXor:
                result = x ^ y;
                return true;
        }
        result = null;
        return false;
    }

    /// <summary>浮点四则运算快路径；除数为 0 返回 false，由通用路径抛 ZeroDivisionError。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryDoubleBinary(int op, double x, double y, out object? result)
    {
        switch (op)
        {
            case PyOps.Add:
                result = x + y;
                return true;
            case PyOps.Sub:
                result = x - y;
                return true;
            case PyOps.Mul:
                result = x * y;
                return true;
            case PyOps.TrueDiv when y != 0.0:
                result = x / y;
                return true;
        }
        result = null;
        return false;
    }

    // ───────────────────────── 可调用值 ─────────────────────────

    private struct CallEntry
    {
        public byte Valid;
        public byte Kind;
        public object? Target;
    }

    private static readonly CallEntry[] CallEntries = new CallEntry[MaxSites * EntriesPerSite];

    /// <summary>每个调用点复用的位置实参数组（只含实参，不含 self）。</summary>
    private static readonly object?[][] ArgScratch = new object?[MaxSites][];

    /// <summary>方法调用专用：`[self, arg0, ...]`，供 `PyFunction` 直接使用。</summary>
    private static readonly object?[][] PreparedScratch = new object?[MaxSites][];

    private const byte CallFunction = 1;
    private const byte CallBound = 2;
    private const byte CallClass = 3;

    /// <summary>带调用点编号的任意可调用值调用。</summary>
    public static object? CallValue(
        int site,
        object? callable,
        object?[] rawArgs,
        object?[] rawNames,
        object?[] rawValues
    )
    {
        if (!Enabled || (uint)site >= MaxSites || PyOps.NeedsFlatten(rawArgs, rawNames))
        {
            return PyOps.CallValue(callable, rawArgs, rawNames, rawValues);
        }

        int baseIndex = site * EntriesPerSite;
        for (int i = 0; i < EntriesPerSite; i++)
        {
            ref CallEntry entry = ref CallEntries[baseIndex + i];
            if (entry.Valid != 0 && ReferenceEquals(entry.Target, callable))
            {
                return InvokeCached(entry.Kind, callable, rawArgs, rawNames, rawValues);
            }
        }

        byte kind = KindOfCallable(callable);
        object? result = InvokeCached(kind, callable, rawArgs, rawNames, rawValues);
        if (kind != 0)
        {
            int slot = NextVictim(CallVictims, site);
            CallEntries[slot] = new CallEntry
            {
                Valid = 1,
                Kind = kind,
                Target = callable,
            };
        }
        return result;
    }

    /// <summary>`CallValueN`：纯位置调用，复用调用点数组。</summary>
    public static object? CallValue0(int site, object? callable) =>
        CallValueFast(site, callable, ArgFor(site, 0));

    /// <summary>`CallValueN`：1 个位置实参。</summary>
    public static object? CallValue1(int site, object? callable, object? a0)
    {
        object?[] args = ArgFor(site, 1);
        args[0] = a0;
        return CallValueFast(site, callable, args);
    }

    /// <summary>`CallValueN`：2 个位置实参。</summary>
    public static object? CallValue2(int site, object? callable, object? a0, object? a1)
    {
        object?[] args = ArgFor(site, 2);
        args[0] = a0;
        args[1] = a1;
        return CallValueFast(site, callable, args);
    }

    /// <summary>`CallValueN`：3 个位置实参。</summary>
    public static object? CallValue3(
        int site,
        object? callable,
        object? a0,
        object? a1,
        object? a2
    )
    {
        object?[] args = ArgFor(site, 3);
        args[0] = a0;
        args[1] = a1;
        args[2] = a2;
        return CallValueFast(site, callable, args);
    }

    private static object? CallValueFast(int site, object? callable, object?[] args)
    {
        if (!Enabled || (uint)site >= MaxSites)
        {
            return PyOps.CallValue(callable, args, PyOps.EmptyArray(), PyOps.EmptyArray());
        }
        int baseIndex = site * EntriesPerSite;
        byte kind = KindOfCallable(callable);
        for (int i = 0; i < EntriesPerSite; i++)
        {
            ref CallEntry entry = ref CallEntries[baseIndex + i];
            if (entry.Valid == 0 || !ReferenceEquals(entry.Target, callable))
            {
                continue;
            }
            if (kind == CallFunction)
            {
                return ((PyFunction)callable!).Invoke(args, EmptyNames, PyOps.EmptyArray());
            }
            if (kind == CallClass)
            {
                return ((PyClass)callable!).Invoke(args, EmptyNames, PyOps.EmptyArray());
            }
            break;
        }

        object? result = InvokeCached(kind, callable, args, PyOps.EmptyArray(), PyOps.EmptyArray());
        if (kind != 0)
        {
            int slot = NextVictim(CallVictims, site);
            CallEntries[slot] = new CallEntry
            {
                Valid = 1,
                Kind = kind,
                Target = callable,
            };
        }
        return result;
    }

    /// <summary>取（必要时扩容）调用点的实参数组；同一调用点参数个数固定。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object?[] ArgFor(int site, int length)
    {
        if ((uint)site >= MaxSites)
        {
            return length == 0 ? PyOps.EmptyArray() : new object?[length];
        }
        object?[]? args = ArgScratch[site];
        if (args is null || args.Length != length)
        {
            args = length == 0 ? PyOps.EmptyArray() : new object?[length];
            ArgScratch[site] = args;
        }
        return args;
    }

    private static readonly string[] EmptyNames = [];

    private static byte KindOfCallable(object? callable) =>
        callable switch
        {
            PyFunction => CallFunction,
            PyBoundMethod => CallBound,
            PyClass => CallClass,
            _ => 0,
        };

    private static object? InvokeCached(
        byte kind,
        object? callable,
        object?[] rawArgs,
        object?[] rawNames,
        object?[] rawValues
    )
    {
        string[] names = CastNames(rawNames);
        return kind switch
        {
            CallFunction => ((PyFunction)callable!).Invoke(rawArgs, names, rawValues),
            CallBound => ((PyBoundMethod)callable!).Invoke(rawArgs, names, rawValues),
            CallClass => ((PyClass)callable!).Invoke(rawArgs, names, rawValues),
            _ => PyOps.CallValue(callable, rawArgs, rawNames, rawValues),
        };
    }

    /// <summary>把 MIR 的 <c>object[]</c> 关键字名转成 <c>string[]</c>。</summary>
    private static string[] CastNames(object?[] rawNames)
    {
        if (rawNames.Length == 0)
        {
            return [];
        }
        var names = new string[rawNames.Length];
        for (int i = 0; i < rawNames.Length; i++)
        {
            names[i] = rawNames[i] as string ?? PyRepr.Str(rawNames[i]);
        }
        return names;
    }
}
