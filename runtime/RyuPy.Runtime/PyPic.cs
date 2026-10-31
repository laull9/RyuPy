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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Binary(int site, int op, object a, object b)
    {
        // 最常见形态直接走 long 快路径：跳过形状分类与缓存查找。
        // 装箱数值都是 long；溢出由 PyInt 升级 BigInteger，语义不变。
        if (a is long leftLong && b is long rightLong)
        {
            switch (op)
            {
                case PyOps.Add:
                    return PyInt.AddI64(leftLong, rightLong);
                case PyOps.Sub:
                    return PyInt.SubtractI64(leftLong, rightLong);
                case PyOps.Mul:
                    return PyInt.MultiplyI64(leftLong, rightLong);
            }
        }
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
            long x = (long)a;
            long y = (long)b;
            switch (op)
            {
                case PyOps.Add:
                    return PyInt.AddI64(x, y);
                case PyOps.Sub:
                    return PyInt.SubtractI64(x, y);
                case PyOps.Mul:
                    return PyInt.MultiplyI64(x, y);
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

    // ───────────────────────── 方法调用 ─────────────────────────

    private struct MethodEntry
    {
        public byte Valid;
        public string? Name;
        public PyClass? Class;
        public long ClassVersion;
        /// <summary>方法名对应的实例属性槽位；-1 表示该类从未给这个名字分配槽位。</summary>
        public int ShadowSlot;
        public PyClass? Owner;
        public object? Target;
        /// <summary>1 表示接收者是实例，2 表示接收者是类（classmethod）。</summary>
        public byte SelfKind;
        public PyClass? SelfClass;
    }

    private static readonly MethodEntry[] MethodEntries = new MethodEntry[MaxSites * EntriesPerSite];

    /// <summary>`CallMethodN`：0 个位置实参。</summary>
    public static object? CallMethod0(int site, object? obj, string name)
    {
        if (Enabled && (uint)site < MaxSites && obj is PyInstance instance)
        {
            long classVersion = PyClass.GlobalVersion;
            if (TryGetMethodEntry(site, instance, name, classVersion, out MethodEntry entry))
            {
                object?[] prepared = PreparedFor(site, 1);
                prepared[0] = entry.SelfKind == 2 ? entry.SelfClass : instance;
                return InvokePreparedMethod(entry, prepared);
            }
            return CallMethodSlow(site, instance, name, classVersion, ArgFor(site, 0));
        }
        object?[] fallback = (uint)site < MaxSites ? ArgFor(site, 0) : PyOps.EmptyArray();
        return PyOps.CallMethod(obj, name, fallback, PyOps.EmptyArray(), PyOps.EmptyArray());
    }

    /// <summary>`CallMethodN`：1 个位置实参。</summary>
    public static object? CallMethod1(int site, object? obj, string name, object? a0)
    {
        if (Enabled && (uint)site < MaxSites && obj is PyInstance instance)
        {
            long classVersion = PyClass.GlobalVersion;
            if (TryGetMethodEntry(site, instance, name, classVersion, out MethodEntry entry))
            {
                object?[] prepared = PreparedFor(site, 2);
                prepared[0] = entry.SelfKind == 2 ? entry.SelfClass : instance;
                prepared[1] = a0;
                return InvokePreparedMethod(entry, prepared);
            }
            object?[] args = ArgFor(site, 1);
            args[0] = a0;
            return CallMethodSlow(site, instance, name, classVersion, args);
        }
        object?[] fallbackArgs = ArgFor(site, 1);
        fallbackArgs[0] = a0;
        return PyOps.CallMethod(
            obj,
            name,
            fallbackArgs,
            PyOps.EmptyArray(),
            PyOps.EmptyArray()
        );
    }

    /// <summary>`CallMethodN`：2 个位置实参。</summary>
    public static object? CallMethod2(int site, object? obj, string name, object? a0, object? a1)
    {
        if (Enabled && (uint)site < MaxSites && obj is PyInstance instance)
        {
            long classVersion = PyClass.GlobalVersion;
            if (TryGetMethodEntry(site, instance, name, classVersion, out MethodEntry entry))
            {
                object?[] prepared = PreparedFor(site, 3);
                prepared[0] = entry.SelfKind == 2 ? entry.SelfClass : instance;
                prepared[1] = a0;
                prepared[2] = a1;
                return InvokePreparedMethod(entry, prepared);
            }
            object?[] args = ArgFor(site, 2);
            args[0] = a0;
            args[1] = a1;
            return CallMethodSlow(site, instance, name, classVersion, args);
        }
        object?[] fallbackArgs = ArgFor(site, 2);
        fallbackArgs[0] = a0;
        fallbackArgs[1] = a1;
        return PyOps.CallMethod(
            obj,
            name,
            fallbackArgs,
            PyOps.EmptyArray(),
            PyOps.EmptyArray()
        );
    }

    /// <summary>`CallMethodN`：3 个位置实参。</summary>
    public static object? CallMethod3(
        int site,
        object? obj,
        string name,
        object? a0,
        object? a1,
        object? a2
    )
    {
        if (Enabled && (uint)site < MaxSites && obj is PyInstance instance)
        {
            long classVersion = PyClass.GlobalVersion;
            if (TryGetMethodEntry(site, instance, name, classVersion, out MethodEntry entry))
            {
                object?[] prepared = PreparedFor(site, 4);
                prepared[0] = entry.SelfKind == 2 ? entry.SelfClass : instance;
                prepared[1] = a0;
                prepared[2] = a1;
                prepared[3] = a2;
                return InvokePreparedMethod(entry, prepared);
            }
            object?[] args = ArgFor(site, 3);
            args[0] = a0;
            args[1] = a1;
            args[2] = a2;
            return CallMethodSlow(site, instance, name, classVersion, args);
        }
        object?[] fallbackArgs = ArgFor(site, 3);
        fallbackArgs[0] = a0;
        fallbackArgs[1] = a1;
        fallbackArgs[2] = a2;
        return PyOps.CallMethod(
            obj,
            name,
            fallbackArgs,
            PyOps.EmptyArray(),
            PyOps.EmptyArray()
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? InvokePreparedMethod(MethodEntry entry, object?[] prepared) =>
        PyObject.InvokePrepared(
            (PyFunction)entry.Target!,
            entry.Owner!,
            prepared[0],
            prepared,
            EmptyNames,
            PyOps.EmptyArray()
        );

    /// <summary>缓存未命中：走完整属性查找，并把可缓存的绑定方法登记下来。</summary>
    private static object? CallMethodSlow(
        int site,
        PyInstance instance,
        string name,
        long classVersion,
        object?[] args
    )
    {
        object? attribute = PyObject.GetAttr(instance, name);
        if (attribute is PyBoundMethod bound && bound.Target is PyFunction)
        {
            byte selfKind = bound.Self is PyClass selfClass ? (byte)2 : (byte)1;
            int slot = site * EntriesPerSite + (int)((uint)site % EntriesPerSite);
            MethodEntries[slot] = new MethodEntry
            {
                Valid = 1,
                Name = name,
                Class = instance.Class,
                ClassVersion = classVersion,
                ShadowSlot = instance.Class.TryGetInstanceSlot(name, out int shadowSlot)
                    ? shadowSlot
                    : -1,
                Owner = bound.Owner,
                Target = bound.Target,
                SelfKind = selfKind,
                SelfClass = selfKind == 2 ? bound.Self as PyClass : null,
            };
        }
        return PyOps.CallValue(attribute, args, PyOps.EmptyArray(), PyOps.EmptyArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetMethodEntry(
        int site,
        PyInstance instance,
        string name,
        long classVersion,
        out MethodEntry found
    )
    {
        int baseIndex = site * EntriesPerSite;
        for (int i = 0; i < EntriesPerSite; i++)
        {
            ref MethodEntry entry = ref MethodEntries[baseIndex + i];
            if (MethodEntryMatches(ref entry, name, instance, classVersion))
            {
                found = entry;
                return true;
            }
        }
        found = default;
        return false;
    }

    private static object? CallMethodFast(int site, object? obj, string name, object?[] args)
    {
        if (!Enabled || (uint)site >= MaxSites || obj is not PyInstance instance)
        {
            return PyOps.CallMethod(obj, name, args, PyOps.EmptyArray(), PyOps.EmptyArray());
        }
        long classVersion = PyClass.GlobalVersion;
        if (TryGetMethodEntry(site, instance, name, classVersion, out MethodEntry entry))
        {
            object?[] prepared = PreparedFor(site, args.Length + 1);
            prepared[0] = entry.SelfKind == 2 ? entry.SelfClass : instance;
            Array.Copy(args, 0, prepared, 1, args.Length);
            return InvokePreparedMethod(entry, prepared);
        }
        return CallMethodSlow(site, instance, name, classVersion, args);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object?[] PreparedFor(int site, int length)
    {
        if ((uint)site >= MaxSites)
        {
            return new object?[length];
        }
        object?[]? prepared = PreparedScratch[site];
        if (prepared is null || prepared.Length != length)
        {
            prepared = new object?[length];
            PreparedScratch[site] = prepared;
        }
        return prepared;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MethodEntryMatches(
        ref MethodEntry entry,
        string name,
        PyInstance instance,
        long classVersion
    ) =>
        entry.Valid != 0
        && string.Equals(entry.Name, name, StringComparison.Ordinal)
        && ReferenceEquals(entry.Class, instance.Class)
        && entry.ClassVersion == classVersion
        // 只有同名实例属性会遮蔽方法；槽位未赋值就是没有遮蔽，O(1) 检查。
        && (entry.ShadowSlot < 0 || !instance.IsSlotSet(entry.ShadowSlot));

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
        int baseIndex = site * EntriesPerSite;
        for (int i = 0; i < EntriesPerSite; i++)
        {
            ref MethodEntry entry = ref MethodEntries[baseIndex + i];
            if (!MethodEntryMatches(ref entry, name, instance, classVersion))
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
                ShadowSlot = instance.Class.TryGetInstanceSlot(name, out int shadowSlot)
                    ? shadowSlot
                    : -1,
                Owner = bound.Owner,
                Target = bound.Target,
                SelfKind = selfKind,
                SelfClass = selfKind == 2 ? bound.Self as PyClass : null,
            };
        }
        return PyOps.CallValue(attribute, rawArgs, rawNames, rawValues);
    }

    // ───────────────────────── 实例属性 ─────────────────────────

    private struct AttrEntry
    {
        public byte Valid;
        public PyClass? Class;
        public long ClassVersion;
        public int Slot;
    }

    private static readonly AttrEntry[] GetAttrEntries = new AttrEntry[MaxSites * EntriesPerSite];
    private static readonly AttrEntry[] SetAttrEntries = new AttrEntry[MaxSites * EntriesPerSite];

    /// <summary>带调用点编号的实例属性读取；只缓存实例槽位。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? GetAttr(int site, object? obj, string name)
    {
        if (!Enabled || (uint)site >= MaxSites || obj is not PyInstance instance)
        {
            return PyObject.GetAttr(obj, name);
        }
        long classVersion = PyClass.GlobalVersion;
        int baseIndex = site * EntriesPerSite;
        for (int i = 0; i < EntriesPerSite; i++)
        {
            ref AttrEntry entry = ref GetAttrEntries[baseIndex + i];
            if (entry.Valid != 0
                && ReferenceEquals(entry.Class, instance.Class)
                && entry.ClassVersion == classVersion
                && instance.TryGetSlot(entry.Slot, out object? cached))
            {
                return cached;
            }
        }

        object? value = PyObject.GetAttr(obj, name);
        if (instance.Class.TryGetInstanceSlot(name, out int slot)
            && instance.TryGetSlot(slot, out _))
        {
            int index = baseIndex + (int)((uint)site % EntriesPerSite);
            GetAttrEntries[index] = new AttrEntry
            {
                Valid = 1,
                Class = instance.Class,
                ClassVersion = classVersion,
                Slot = slot,
            };
        }
        return value;
    }

    /// <summary>带调用点编号的实例属性写入；只缓存无描述符类的实例槽位。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetAttr(int site, object? obj, string name, object? value)
    {
        if (!Enabled
            || (uint)site >= MaxSites
            || obj is not PyInstance instance
            || instance.Class.HasProperties)
        {
            PyObject.SetAttr(obj, name, value);
            return;
        }
        long classVersion = PyClass.GlobalVersion;
        int baseIndex = site * EntriesPerSite;
        for (int i = 0; i < EntriesPerSite; i++)
        {
            ref AttrEntry entry = ref SetAttrEntries[baseIndex + i];
            if (entry.Valid != 0
                && ReferenceEquals(entry.Class, instance.Class)
                && entry.ClassVersion == classVersion)
            {
                instance.SetSlot(entry.Slot, value);
                return;
            }
        }

        PyObject.SetAttr(obj, name, value);
        if (instance.Class.TryGetInstanceSlot(name, out int slot))
        {
            int index = baseIndex + (int)((uint)site % EntriesPerSite);
            SetAttrEntries[index] = new AttrEntry
            {
                Valid = 1,
                Class = instance.Class,
                ClassVersion = PyClass.GlobalVersion,
                Slot = slot,
            };
        }
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
