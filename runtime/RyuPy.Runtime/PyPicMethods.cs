using System;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>
/// PIC 的方法调用部分：<c>obj.method(...)</c> 按调用点缓存「类 + 版本 → 目标函数」。
/// </summary>
/// <remarks>
/// 命中条件：类对象相同、全局类版本未变、同名实例属性槽位未赋值（实例属性会遮蔽方法）。
/// 命中后把接收者放进按站点复用的实参数组，直接调用目标函数（跳过绑定方法对象）；
/// 未命中走通用查找并按轮转写入条目。<c>super().method(...)</c> 的分支见 <c>PyPicAttrs.cs</c>。
/// </remarks>
public static partial class PyPic
{
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
        return CallMethodFallback(site, obj, name, fallback);
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
        return CallMethodFallback(site, obj, name, fallbackArgs);
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
        return CallMethodFallback(site, obj, name, fallbackArgs);
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
        return CallMethodFallback(site, obj, name, fallbackArgs);
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
            int slot = NextVictim(MethodVictims, site);
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
            return CallMethodFallback(site, obj, name, args);
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
            int slot = NextVictim(MethodVictims, site);
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
}
