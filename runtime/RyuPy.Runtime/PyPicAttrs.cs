using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>
/// PIC 的实例属性部分：<c>obj.name</c> 读取与 <c>obj.name = v</c> 写入的按调用点缓存。
/// </summary>
/// <remarks>
/// # 条目种类
///
/// 每个调用点最多 <see cref="EntriesPerSite"/> 条，键为「类对象 + 全局类版本」：
///
/// <list type="bullet">
/// <item><b>实例槽位</b>：属性存放在 <see cref="PyInstance"/> 的值数组里，命中后直接按下标读写；
/// 读取时槽位未赋值（被 <c>del</c>）仍回退通用路径，以得到正确的 AttributeError / 类属性；</item>
/// <item><b>property getter</b>（只用于读取）：类链上的同名属性是 <see cref="PyProperty"/> 且 getter
/// 是普通 Python 函数时，命中后直接调用 getter，跳过类链查找与实参数组分配；</item>
/// <item><b>类属性值</b>（只用于读取）：类链上的普通值（常量、类级计数等，不含需要绑定的函数 /
/// 描述符）。命中时先看同名实例槽位是否已赋值（实例属性遮蔽类属性），没有才返回缓存值。</item>
/// </list>
///
/// # 正确性
///
/// 填充条目时按 Python 的查找顺序判断：数据描述符（property）优先于实例属性。写入只在
/// 「该名字不是 property」时缓存槽位——类上有别的 property 不影响普通属性的缓存
/// （M2.11 曾因此整类放弃缓存）。任何类修改（增删方法 / property、新增槽位）都会递增
/// <see cref="PyClass.GlobalVersion"/>，旧条目随之失效。
/// </remarks>
public static partial class PyPic
{
    private struct AttrEntry
    {
        public byte Valid;
        public PyClass? Class;
        public long ClassVersion;
        public int Slot;
        // property getter 条目：非 null 时忽略 Slot
        public PyFunction? Getter;
        public PyClass? GetterOwner;
        // 类属性值条目：IsClassValue 时 Slot 是同名实例槽位（-1 表示没有），命中先查遮蔽
        public bool IsClassValue;
        public object? ClassValue;
    }

    private static readonly AttrEntry[] GetAttrEntries = new AttrEntry[MaxSites * EntriesPerSite];
    private static readonly AttrEntry[] SetAttrEntries = new AttrEntry[MaxSites * EntriesPerSite];

    /// <summary>property getter 调用复用的 `[self]` 实参数组（每个调用点一个，规则同 ArgScratch）。</summary>
    private static readonly object?[][] GetterScratch = new object?[MaxSites][];

    /// <summary>带调用点编号的实例属性读取。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? GetAttr(int site, object? obj, string name)
    {
        if (!Enabled || (uint)site >= MaxSites || obj is not PyInstance instance || instance.Class.HasEnumMetadata)
        {
            return PyObject.GetAttr(obj, name);
        }
        long classVersion = PyClass.GlobalVersion;
        int baseIndex = site * EntriesPerSite;
        for (int i = 0; i < EntriesPerSite; i++)
        {
            ref AttrEntry entry = ref GetAttrEntries[baseIndex + i];
            if (entry.Valid == 0
                || !ReferenceEquals(entry.Class, instance.Class)
                || entry.ClassVersion != classVersion)
            {
                continue;
            }
            if (entry.Getter is not null)
            {
                return CallGetter(site, entry.Getter, entry.GetterOwner!, instance);
            }
            if (entry.IsClassValue)
            {
                return entry.Slot >= 0 && instance.TryGetSlot(entry.Slot, out object? shadow)
                    ? shadow
                    : entry.ClassValue;
            }
            if (instance.TryGetSlot(entry.Slot, out object? cached))
            {
                return cached;
            }
        }

        object? value = PyObject.GetAttr(obj, name);
        FillGetAttr(site, instance, name, classVersion);
        return value;
    }

    /// <summary>通用读取成功后，按 Python 查找顺序决定能缓存哪种条目。</summary>
    private static void FillGetAttr(int site, PyInstance instance, string name, long classVersion)
    {
        PyClass cls = instance.Class;
        var entry = new AttrEntry { Valid = 1, Class = cls, ClassVersion = classVersion };
        if (cls.HasProperties
            && cls.TryLookup(name, out PyClass owner, out object? raw)
            && raw is PyProperty property)
        {
            // property 优先于实例属性：只有 getter 是普通函数时才缓存，其它形态每次走通用路径
            if (property.Getter is not PyFunction getter)
            {
                return;
            }
            entry.Getter = getter;
            entry.GetterOwner = owner;
        }
        else if (cls.TryGetInstanceSlot(name, out int slot) && instance.TryGetSlot(slot, out _))
        {
            entry.Slot = slot;
        }
        else if (cls.TryLookup(name, out _, out object? classValue) && IsPlainClassValue(classValue))
        {
            entry.IsClassValue = true;
            entry.ClassValue = classValue;
            entry.Slot = cls.TryGetInstanceSlot(name, out int shadowSlot) ? shadowSlot : -1;
        }
        else
        {
            return;
        }
        // 读取期间 getter 可能修改了类（版本变化），此时条目已经过期，不再写入
        if (classVersion == PyClass.GlobalVersion)
        {
            GetAttrEntries[NextVictim(GetAttrVictims, site)] = entry;
        }
    }

    /// <summary>读取时不需要绑定 / 描述符协议的类属性值（函数、property 等每次都要绑定，不缓存）。</summary>
    private static bool IsPlainClassValue(object? value) =>
        value is not (PyFunction or PyStaticMethod or PyClassMethod or PyProperty or PyBoundMethod);

    /// <summary>调用缓存的 property getter：实参数组按站点复用，getter 入口即读出 self。</summary>
    private static object? CallGetter(int site, PyFunction getter, PyClass owner, PyInstance instance)
    {
        object?[] args = GetterScratch[site] ??= new object?[1];
        args[0] = instance;
        return PyObject.InvokePrepared(getter, owner, instance, args, [], []);
    }

    /// <summary>带调用点编号的实例属性写入；名字不是 property 时缓存实例槽位。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetAttr(int site, object? obj, string name, object? value)
    {
        if (!Enabled || (uint)site >= MaxSites || obj is not PyInstance instance || instance.Class.HasEnumMetadata)
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
        PyClass cls = instance.Class;
        bool isProperty = cls.HasProperties
            && cls.TryLookup(name, out _, out object? raw)
            && raw is PyProperty;
        if (!isProperty && cls.TryGetInstanceSlot(name, out int slot))
        {
            SetAttrEntries[NextVictim(SetAttrVictims, site)] = new AttrEntry
            {
                Valid = 1,
                Class = cls,
                // 写入可能新增槽位并递增版本：用写入之后的版本
                ClassVersion = PyClass.GlobalVersion,
                Slot = slot,
            };
        }
    }

    // ───────────────────────── super().method(...) ─────────────────────────

    private struct SuperEntry
    {
        public byte Valid;
        public string? Name;
        /// <summary>`super()` 所在方法的定义类（查找从它的基类开始）。</summary>
        public PyClass? Start;
        public long ClassVersion;
        /// <summary>找到方法的类（作为被调方法的 `super()` 上下文）。</summary>
        public PyClass? Owner;
        public PyFunction? Target;
    }

    private static readonly SuperEntry[] SuperEntries = new SuperEntry[MaxSites];

    /// <summary>
    /// 小参数方法调用的非实例分支：`super().name(...)` 走按站点缓存，其它接收者走通用路径。
    /// </summary>
    /// <remarks>
    /// 零参数 <c>super()</c> 的结果每次都是新的 <see cref="PySuper"/>，但「从哪个类开始找、找到哪个
    /// 函数」只取决于定义类与类版本，可以缓存。命中时直接以 <c>super.Self</c> 为第一个实参调用目标
    /// 函数，省掉绑定方法对象与类链遍历；只缓存普通函数（property / classmethod 等走通用路径）。
    /// </remarks>
    private static object? CallMethodFallback(int site, object? obj, string name, object?[] args)
    {
        if (PyOps.TryCapturedModuleMethod(obj, out object? captured))
            return PyOps.CallValue(captured, args, [], []);
        if (!Enabled || (uint)site >= MaxSites || obj is not PySuper super)
        {
            return PyOps.CallMethod(obj, name, args, PyOps.EmptyArray(), PyOps.EmptyArray());
        }
        long classVersion = PyClass.GlobalVersion;
        ref SuperEntry entry = ref SuperEntries[site];
        if (entry.Valid == 0
            || !ReferenceEquals(entry.Start, super.Owner)
            || entry.ClassVersion != classVersion
            || !string.Equals(entry.Name, name, System.StringComparison.Ordinal))
        {
            if (!TryResolveSuper(super.Owner, name, out PyClass? owner, out PyFunction? target))
            {
                return PyOps.CallMethod(obj, name, args, PyOps.EmptyArray(), PyOps.EmptyArray());
            }
            entry = new SuperEntry
            {
                Valid = 1,
                Name = name,
                Start = super.Owner,
                ClassVersion = classVersion,
                Owner = owner,
                Target = target,
            };
        }
        object?[] prepared = PreparedFor(site, args.Length + 1);
        prepared[0] = super.Self;
        System.Array.Copy(args, 0, prepared, 1, args.Length);
        return PyObject.InvokePrepared(entry.Target!, entry.Owner!, super.Self, prepared, [], []);
    }

    /// <summary>沿 `start` 的基类链找第一个定义 `name` 的类；只接受普通函数。</summary>
    private static bool TryResolveSuper(
        PyClass start,
        string name,
        out PyClass? owner,
        out PyFunction? target
    )
    {
        for (PyClass? current = start.BaseClass; current is not null; current = current.BaseClass)
        {
            if (current.Attributes.TryGetValue(name, out object? raw))
            {
                owner = current;
                target = raw as PyFunction;
                return target is not null;
            }
        }
        owner = null;
        target = null;
        return false;
    }
}
