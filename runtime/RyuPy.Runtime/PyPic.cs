using System;
using System.Numerics;

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
public static class PyPic
{
    /// <summary>最大调用点数；超出后这些位置直接使用通用路径。</summary>
    private const int MaxSites = 4096;

    /// <summary>每个调用点保留的条目数。</summary>
    private const int EntriesPerSite = 2;

    private static readonly bool Enabled = !string.Equals(
        Environment.GetEnvironmentVariable("RYUPY_PIC"),
        "0",
        StringComparison.Ordinal
    );

    // ───────────────────────── 二元运算 ─────────────────────────

    private struct BinaryEntry
    {
        public byte Valid;
        public int Op;
        public byte LeftKind;
        public byte RightKind;
    }

    private static readonly BinaryEntry[] BinaryEntries = new BinaryEntry[MaxSites * EntriesPerSite];

    /// <summary>整数值种类：普通 <c>long</c>。</summary>
    private const byte KindLong = 1;
    /// <summary>浮点值种类。</summary>
    private const byte KindDouble = 2;
    /// <summary>布尔值种类（数值语义按 0/1）。</summary>
    private const byte KindBool = 3;
    /// <summary>字符串种类。</summary>
    private const byte KindString = 4;
    /// <summary>大整数种类。</summary>
    private const byte KindBigInteger = 5;
    /// <summary>其它对象（可能带魔术方法）。</summary>
    private const byte KindOther = 6;

    private static byte KindOf(object? value) =>
        value switch
        {
            long => KindLong,
            double => KindDouble,
            bool => KindBool,
            string => KindString,
            BigInteger => KindBigInteger,
            _ => KindOther,
        };

    /// <summary>带调用点编号的二元运算入口。</summary>
    public static object Binary(int site, int op, object a, object b)
    {
        if (!Enabled || (uint)site >= MaxSites)
        {
            return PyOps.Binary(op, a, b);
        }
        byte left = KindOf(a);
        byte right = KindOf(b);
        int baseIndex = site * EntriesPerSite;
        for (int i = 0; i < EntriesPerSite; i++)
        {
            ref BinaryEntry entry = ref BinaryEntries[baseIndex + i];
            if (entry.Valid != 0
                && entry.Op == op
                && entry.LeftKind == left
                && entry.RightKind == right)
            {
                return FastBinary(op, left, right, a, b);
            }
        }

        object result = FastBinary(op, left, right, a, b);
        // 只缓存能走数值 / 字符串快路径的形状；其它形状每次仍走通用路径。
        if (left != KindOther && right != KindOther)
        {
            int slot = baseIndex + (int)((uint)site % EntriesPerSite);
            BinaryEntries[slot] = new BinaryEntry
            {
                Valid = 1,
                Op = op,
                LeftKind = left,
                RightKind = right,
            };
        }
        return result;
    }

    /// <summary>按操作数种类直接分派；只覆盖数值与字符串，其它交给通用入口。</summary>
    private static object FastBinary(int op, byte left, byte right, object a, object b)
    {
        if (left == KindLong && right == KindLong)
        {
            switch (op)
            {
                case PyOps.Add:
                    return PyInt.Add(a, b);
                case PyOps.Sub:
                    return PyInt.Subtract(a, b);
                case PyOps.Mul:
                    return PyInt.Multiply(a, b);
            }
        }
        else if (left == KindDouble && right == KindDouble)
        {
            double x = (double)a;
            double y = (double)b;
            switch (op)
            {
                case PyOps.Add:
                    return x + y;
                case PyOps.Sub:
                    return x - y;
                case PyOps.Mul:
                    return x * y;
            }
        }
        else if ((left == KindLong || left == KindDouble)
            && (right == KindLong || right == KindDouble))
        {
            double x = left == KindLong ? (long)a : (double)a;
            double y = right == KindLong ? (long)b : (double)b;
            switch (op)
            {
                case PyOps.Add:
                    return x + y;
                case PyOps.Sub:
                    return x - y;
                case PyOps.Mul:
                    return x * y;
            }
        }
        else if (left == KindBigInteger && right == KindBigInteger)
        {
            switch (op)
            {
                case PyOps.Add:
                    return PyInt.Add(a, b);
                case PyOps.Sub:
                    return PyInt.Subtract(a, b);
                case PyOps.Mul:
                    return PyInt.Multiply(a, b);
            }
        }
        else if (left == KindString && right == KindString && op == PyOps.Add)
        {
            return string.Concat((string)a, (string)b);
        }
        return PyOps.Binary(op, a, b);
    }

    // ───────────────────────── 可调用值 ─────────────────────────

    private struct CallEntry
    {
        public byte Valid;
        public byte Kind;
        public object? Target;
    }

    private static readonly CallEntry[] CallEntries = new CallEntry[MaxSites * EntriesPerSite];

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
            int slot = baseIndex + (int)((uint)site % EntriesPerSite);
            CallEntries[slot] = new CallEntry
            {
                Valid = 1,
                Kind = kind,
                Target = callable,
            };
        }
        return result;
    }

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

    // ───────────────────────── 方法调用 ─────────────────────────

    private struct MethodEntry
    {
        public byte Valid;
        public string? Name;
        public PyClass? Class;
        public long ClassVersion;
        public long InstanceVersion;
        public PyClass? Owner;
        public object? Target;
        /// <summary>1 表示接收者是实例，2 表示接收者是类（classmethod）。</summary>
        public byte SelfKind;
        public PyClass? SelfClass;
    }

    private static readonly MethodEntry[] MethodEntries = new MethodEntry[MaxSites * EntriesPerSite];

    /// <summary>带调用点编号的方法调用；只缓存普通实例 / 类方法。</summary>
    public static object? CallMethod(
        int site,
        object? obj,
        string name,
        object?[] rawArgs,
        object?[] rawNames,
        object?[] rawValues
    )
    {
        if (!Enabled
            || (uint)site >= MaxSites
            || PyOps.NeedsFlatten(rawArgs, rawNames)
            || obj is not PyInstance instance)
        {
            return PyOps.CallMethod(obj, name, rawArgs, rawNames, rawValues);
        }

        long classVersion = PyClass.GlobalVersion;
        long instanceVersion = instance.Version;
        int baseIndex = site * EntriesPerSite;
        for (int i = 0; i < EntriesPerSite; i++)
        {
            ref MethodEntry entry = ref MethodEntries[baseIndex + i];
            if (entry.Valid == 0
                || !string.Equals(entry.Name, name, StringComparison.Ordinal)
                || !ReferenceEquals(entry.Class, instance.Class)
                || entry.ClassVersion != classVersion
                || entry.InstanceVersion != instanceVersion)
            {
                continue;
            }
            object? self = entry.SelfKind == 2 ? entry.SelfClass : instance;
            return PyObject.InvokeBound(
                entry.Target,
                entry.Owner!,
                self,
                rawArgs,
                CastNames(rawNames),
                rawValues
            );
        }

        object? attribute = PyObject.GetAttr(obj, name);
        if (attribute is PyBoundMethod bound && bound.Target is PyFunction)
        {
            byte selfKind = bound.Self is PyClass selfClass ? (byte)2 : (byte)1;
            int slot = baseIndex + (int)((uint)site % EntriesPerSite);
            MethodEntries[slot] = new MethodEntry
            {
                Valid = 1,
                Name = name,
                Class = instance.Class,
                ClassVersion = classVersion,
                InstanceVersion = instanceVersion,
                Owner = bound.Owner,
                Target = bound.Target,
                SelfKind = selfKind,
                SelfClass = selfKind == 2 ? bound.Self as PyClass : null,
            };
        }
        return PyOps.CallValue(attribute, rawArgs, rawNames, rawValues);
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
