using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>
/// Python 普通类对象（M2.5 / M2.6）。
/// </summary>
/// <remarks>
/// # 表示
///
/// 普通类不生成 CLR 类型：类体是运行时语句，属性 / 方法都在 `class` 语句执行时
/// 写进 <see cref="Attributes"/>。实例是 <see cref="PyInstance"/>，持有自己的属性字典。
///
/// # 单继承
///
/// 只支持单继承；<see cref="BaseClass"/> 指向用户基类，或由 <see cref="BuiltinBase"/>
/// 记录内建异常基类名（用于 `except ValueError` 与 `isinstance(e, Exception)`）。
///
/// # 缓存与失效
///
/// 属性查找结果按「类 + 名字」缓存，条目携带**全局修改版本**。任何类属性增删 / 替换
/// 都会递增版本，下一次查找自动失效；缓存条目超过上限直接清空，避免长期运行下无限增长。
/// 这与 semantics.md 陷阱 #22（方法 / 属性重绑定）对应。
/// </remarks>
public sealed partial class PyClass
{
    /// <summary>类名（`__name__` 与错误消息使用）。</summary>
    public string Name { get; }

    /// <summary>显式原生 Python 类的定义模块；null 保持普通类既有元数据行为。</summary>
    internal string? NativeDefinitionModule { get; init; }

    internal object? NativeModuleValue => Attributes.TryGetValue("__module__", out object? value)
        ? value : NativeDefinitionModule;

    /// <summary>枚举类冷元数据标记；普通类热路径不查询弱表。</summary>
    internal bool HasEnumMetadata { get; set; }

    /// <summary>用户基类；`None` 表示直接继承 `object` 或内建异常。</summary>
    public PyClass? BaseClass { get; internal set; }

    /// <summary>继承链末端的内建异常名（如 `ValueError`）。普通类为 <c>null</c>。</summary>
    public string? BuiltinBase { get; internal set; }

    /// <summary>类属性字典；保持插入顺序（Python 3.7+ 的类 `__dict__` 同样有序）。</summary>
    public Dictionary<string, object?> Attributes { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// 异常对象的 CLR 工厂。用户异常类由 codegen 生成 CLR 子类，运行时通过这个委托
    /// 实例化，从而保持 CLR 与 Python 继承树同构（类型化 catch 才能正确匹配）。
    /// </summary>
    public PyExceptionFactory? ExceptionFactory { get; set; }

    /// <summary>类定义修改版本（仅用于调试；缓存实际比较全局版本）。</summary>
    public long Version { get; private set; }

    /// <summary>全部类的修改版本。</summary>
    private static long _globalVersion;

    /// <summary>当前全局修改版本；PIC 用它判断缓存条目是否过期。</summary>
    public static long GlobalVersion => _globalVersion;

    /// <summary>属性查找缓存上限。</summary>
    private const int CacheLimit = 256;

    private readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);

    private readonly record struct CacheEntry(
        long Version,
        bool Found,
        PyClass? Owner,
        object? Value
    );

    /// <summary>属性查找缓存的版本与本类链是否含 property 的结果。</summary>
    private long _propertyVersion = -1;
    private bool _propertyFlag;

    /// <summary>
    /// 本类链是否定义过 <see cref="PyProperty"/>。大多数类没有描述符，属性读写可以
    /// 直接跳过「数据描述符优先」的查找。
    /// </summary>
    public bool HasProperties
    {
        get
        {
            RequireLexicalFactoryReady();
            long version = _globalVersion;
            if (_propertyVersion == version)
            {
                return _propertyFlag;
            }
            bool found = false;
            for (PyClass? current = this; current is not null && !found; current = current.BaseClass)
            {
                foreach (object? value in current.Attributes.Values)
                {
                    if (value is PyProperty)
                    {
                        found = true;
                        break;
                    }
                }
            }
            _propertyVersion = version;
            _propertyFlag = found;
            return found;
        }
    }

    /// <summary>创建一个类对象。</summary>
    public PyClass(string name) => Name = name;

    /// <summary>是否属于 `BaseException` 家族。</summary>
    public bool IsException => BuiltinBase is not null || BaseClass?.IsException == true;

    /// <summary>写入 / 替换类属性。</summary>
    public void SetAttr(string name, object? value)
    {
        PyNamedTupleLayout.GuardMutation(this);
        PyEnum.GuardClassMutation(this, name);
        GuardConstructionSet(name, value);
        GuardStringMutation(name, value, deleting: false);
        GuardListMutation(name, value, deleting: false);
        GuardDictMutation(name, value, deleting: false);
        GuardTypingIoMutation(name, deleting: false);
        PyTypingGenericMetadata.MutateClass(this, name);
        Attributes[name] = value;
        Touch();
    }

    /// <summary>删除类属性；不存在时返回 <c>false</c>。</summary>
    public bool DeleteAttr(string name)
    {
        PyNamedTupleLayout.GuardMutation(this);
        GuardTypingIoMutation(name, deleting: true);
        PyTypingGenericMetadata.MutateClass(this, name);
        if (NativeDefinitionModule is not null && name == "__module__")
            throw new PyTypeError($"cannot delete '__module__' attribute of type '{Name}'");
        PyEnum.GuardClassMutation(this, name);
        GuardConstructionDelete(name);
        GuardStringMutation(name, null, deleting: true);
        GuardListMutation(name, null, deleting: true);
        GuardDictMutation(name, null, deleting: true);
        if (!Attributes.Remove(name))
        {
            return false;
        }
        Touch();
        return true;
    }

    private void Touch()
    {
        Version = ++_globalVersion;
    }

    /// <summary>实例属性名 → 槽位下标；槽位只追加，不重排。</summary>
    private Dictionary<string, int>? _instanceSlots;

    /// <summary>已分配的实例属性槽位数。</summary>
    public int InstanceSlotCount { get; private set; }

    /// <summary>查找实例属性槽位；没有分配过时返回 <c>false</c>。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetInstanceSlot(string name, out int slot)
    {
        if (_instanceSlots is null)
        {
            slot = -1;
            return false;
        }
        return _instanceSlots.TryGetValue(name, out slot);
    }

    /// <summary>枚举某实例真正已赋值的属性名，不暴露同类其它实例或已删除的槽位。</summary>
    internal IEnumerable<string> AssignedInstanceNames(PyInstance instance)
    {
        if (_instanceSlots is null) yield break;
        foreach (var (name, slot) in _instanceSlots)
            if (instance.IsSlotSet(slot)) yield return name;
    }

    /// <summary>
    /// 取或分配实例属性槽位。新增槽位会让类版本递增，使方法 PIC 重新确认遮蔽关系。
    /// </summary>
    public int GetOrAddInstanceSlot(string name)
    {
        if (IsTypingGenericRoot) PyTypingGenericMetadata.MutateInstance(this, name, deleting: false);
        GuardTypingIoInstanceSlot(name);
        _instanceSlots ??= new Dictionary<string, int>(StringComparer.Ordinal);
        if (_instanceSlots.TryGetValue(name, out int slot))
        {
            return slot;
        }
        slot = InstanceSlotCount++;
        _instanceSlots[name] = slot;
        // 新增槽位可能让 `name` 从「类属性」变成「可被实例遮蔽」，方法缓存需要失效。
        Touch();
        return slot;
    }

    /// <summary>
    /// 沿类链查找原始属性（不绑定、不调用 property）。
    /// </summary>
    /// <remarks>
    /// 缓存失效靠全局版本：任何类的任何属性修改都会让条目过期，宁可多查一次也不能
    /// 返回旧绑定（陷阱 #22）。条目数超过上限时整体清空，容量有界。
    /// </remarks>
    public bool TryLookup(string name, out PyClass owner, out object? value)
    {
        RequireLexicalFactoryReady();
        long version = _globalVersion;
        if (_cache.TryGetValue(name, out CacheEntry cached) && cached.Version == version)
        {
            if (!cached.Found)
            {
                owner = null!;
                value = null;
                return false;
            }
            owner = cached.Owner!;
            value = cached.Value;
            return true;
        }
        if (_cache.Count >= CacheLimit)
        {
            _cache.Clear();
        }
        if (Walk(this, name, out owner, out value))
        {
            _cache[name] = new CacheEntry(version, true, owner, value);
            return true;
        }
        // 负结果也缓存：实例属性读取会先查 property，普通类没有同名类属性时
        // 每次沿继承链走一遍是纯浪费。
        _cache[name] = new CacheEntry(version, false, null, null);
        return false;
    }

    private static bool Walk(PyClass? cls, string name, out PyClass owner, out object? value)
    {
        for (PyClass? current = cls; current is not null; current = current.BaseClass)
        {
            if (current.Attributes.TryGetValue(name, out value))
            {
                owner = current;
                return true;
            }
        }
        owner = null!;
        value = null;
        return false;
    }

    /// <summary>是否为 <paramref name="other"/> 或其子类。</summary>
    public bool IsSubclassOf(PyClass other)
    {
        for (PyClass? current = this; current is not null; current = current.BaseClass)
        {
            if (ReferenceEquals(current, other))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>是否继承自某个内建异常类型。</summary>
    public bool IsSubclassOfBuiltin(string typeName)
    {
        for (PyClass? current = this; current is not null; current = current.BaseClass)
        {
            if (current.BuiltinBase is { } builtin
                && PyExceptionHierarchy.IsSubclass(builtin, typeName))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>创建实例并执行 `__init__`。</summary>
    public object? Invoke(object?[] args, string[] kwNames, object?[] kwValues)
    {
        RequireLexicalFactoryReady();
        if (IsTypingGenericRoot) return PyTypingGeneric.Construct(args, kwNames);
        RequireTypingIoReady();
        RequireTypingGenericReady();
        if (NamedTupleLayout is not null) return PyNamedTupleLayout.Construct(this, args, kwNames, kwValues);
        // 装饰后的布局/祖先late new变化按现版本重新核，必须早于fastplan分配。
        if (HasStringLayout) return PyStringLayout.Construct(this, args, kwNames, kwValues);
        if (HasListLayout) return PyListLayout.Construct(this, args, kwNames, kwValues);
        if (HasDictLayout) return PyDictLayout.Construct(this, args, kwNames, kwValues);
        ConstructionSlots slots = GuardSpecialNew();
        if (HasEnumMetadata) return PyEnum.Construct(this, args, kwNames, kwValues);
        if (ExceptionFactory is not null)
        {
            // 用户异常：CLR 工厂已经根据 args 构造了异常对象；如果用户写了 `__init__`
            // 仍按 Python 规则调用（参数就是构造实参）。
            object? created = ExceptionFactory(this, args);
            if (created is PyBaseException exception)
            {
                exception.BindPythonClass(this);
            }
            if (TryLookup("__init__", out PyClass owner, out object? init))
            {
                PyObject.InvokeBound(init, owner, created, args, kwNames, kwValues);
            }
            return created;
        }

        if (TryConstructDataclass(args, kwNames, out PyInstance? fastInstance)) return fastInstance;

        try { return slots.Special ? ConstructLegacySpecial(slots, args, kwNames, kwValues)
            : ConstructOrdinary(slots, args, kwNames, kwValues); }
        catch (PyBaseException error) { throw (PyBaseException)PyErrors.PrepareRaise(error); }
    }

    /// <summary>与 CPython 一致的 <c>&lt;class 'Name'&gt;</c> 形式。</summary>
    public override string ToString() => IsTypingGenericRoot ? PyTypingGenericMetadata.ClassRepr(this)
        : IsTypingIoFamily ? PyTypingIo.ClassRepr(this)
        : NamedTupleLayout is not null ? PyNamedTupleLayout.ClassRepr(this)
        : HasListLayout ? PyListLayout.ClassRepr(this)
        : HasDictLayout ? PyDictLayout.ClassRepr(this)
        : NativeDefinitionModule is not null
        && NativeModuleValue is string module && module != "builtins"
        ? $"<class '{module}.{Name}'>" : $"<class '{Name}'>";
}

/// <summary>普通类实例：类引用 + 实例属性字典（保持插入顺序）。</summary>
public class PyInstance
{
    /// <summary>所属类。</summary>
    public PyClass Class { get; }

    /// <summary>按类槽位表存放的实例属性；未赋值的槽位是 <see cref="PyUnbound.Instance"/>。</summary>
    private object?[] _slots = [];

    /// <summary>读取槽位；越界、<c>null</c> 与未赋值哨兵都视为不存在。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetSlot(int slot, out object? value)
    {
        if ((uint)slot < (uint)_slots.Length)
        {
            object? current = _slots[slot];
            if (current is not null && !ReferenceEquals(current, PyUnbound.Instance))
            {
                value = current;
                return true;
            }
        }
        value = null;
        return false;
    }

    /// <summary>判断槽位是否已经赋过值。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsSlotSet(int slot) =>
        (uint)slot < (uint)_slots.Length
        && _slots[slot] is not null
        && !ReferenceEquals(_slots[slot], PyUnbound.Instance);

    /// <summary>写入槽位，必要时扩容。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetSlot(int slot, object? value)
    {
        if ((uint)slot >= (uint)_slots.Length)
        {
            Array.Resize(ref _slots, Math.Max(slot + 1, Math.Max(4, _slots.Length * 2)));
        }
        _slots[slot] = value;
    }

    /// <summary>删除槽位里的值。</summary>
    internal void ClearSlot(int slot)
    {
        if ((uint)slot < (uint)_slots.Length)
        {
            _slots[slot] = PyUnbound.Instance;
        }
    }

    /// <summary>创建实例；槽位数组按类当前的槽位数预分配（`__init__` 写属性时不必逐次扩容）。</summary>
    public PyInstance(PyClass cls)
    {
        Class = cls;
        int count = cls.InstanceSlotCount;
        if (count > 0)
        {
            // null 即「未赋值」（见 TryGetSlot），与哨兵等价
            _slots = new object?[count];
        }
    }

    /// <summary>默认 `repr` 的地址部分；用运行时身份代替真实地址（D002）。</summary>
    public string DefaultRepr() => Class.IsTypingGenericRoot ? PyTypingGenericMetadata.InstanceRepr(this)
        : Class.IsTypingIoFamily ? PyTypingIo.InstanceRepr(this)
        : $"<{Class.Name} object at 0x{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this):x}>";

    /// <inheritdoc />
    public override string ToString() => PyRepr.Str(this);
}

/// <summary>`@property` 描述符。</summary>
public sealed partial class PyProperty
{
    /// <summary>getter（可调用值）。</summary>
    public object? Getter { get; }
    /// <summary>setter（可调用值）；没有时为 <c>null</c>。</summary>
    public object? Setter { get; }

    /// <summary>构造属性描述符。</summary>
    public PyProperty(object? getter, object? setter = null) : this(getter, setter, null) { }

    /// <inheritdoc />
    public override string ToString() => "<property object>";
}

/// <summary>`@staticmethod` 包装：访问时不绑定实例。</summary>
public sealed class PyStaticMethod
{
    /// <summary>被包装的可调用值。</summary>
    public object? Value { get; }

    /// <summary>构造包装。</summary>
    public PyStaticMethod(object? value) => Value = value;

    /// <inheritdoc />
    public override string ToString() => "<staticmethod object>";
}

/// <summary>`@classmethod` 包装：访问时绑定类对象。</summary>
public sealed class PyClassMethod
{
    /// <summary>被包装的可调用值。</summary>
    public object? Value { get; }

    /// <summary>构造包装。</summary>
    public PyClassMethod(object? value) => Value = value;

    /// <inheritdoc />
    public override string ToString() => "<classmethod object>";
}

/// <summary>绑定方法：持有定义类、接收者与函数对象。</summary>
public sealed class PyBoundMethod
{
    /// <summary>方法定义所在的类（`super()` 需要从这里继续向上查找）。</summary>
    public PyClass Owner { get; }
    /// <summary>绑定的接收者（实例或类对象）。</summary>
    public object? Self { get; }
    /// <summary>底层可调用值（通常是 <see cref="PyFunction"/>）。</summary>
    public object? Target { get; }

    /// <summary>构造绑定方法。</summary>
    public PyBoundMethod(PyClass owner, object? self, object? target)
    {
        Owner = owner;
        Self = self;
        Target = target;
    }

    /// <summary>调用绑定方法：把接收者作为第一个位置实参。</summary>
    public object? Invoke(object?[] args, string[] kwNames, object?[] kwValues) =>
        PyObject.InvokeBound(Target, Owner, Self, args, kwNames, kwValues);

    /// <inheritdoc />
    public override string ToString() =>
        $"<bound method {Owner.Name}.{PyRepr.MethodName(Target)} of {PyRepr.Str(Self)}>";
}

/// <summary>零参数 `super()` 的结果：从定义类的下一个基类开始查找。</summary>
public sealed class PySuper
{
    /// <summary>当前方法定义所在的类。</summary>
    public PyClass Owner { get; }
    /// <summary>当前接收者。</summary>
    public object? Self { get; }

    /// <summary>构造 super 代理。</summary>
    public PySuper(PyClass owner, object? self)
    {
        Owner = owner;
        Self = self;
    }
}

/// <summary>当前正在执行的方法上下文（零参数 `super()` 依赖它）。</summary>
public static class PyMethodContext
{
    [ThreadStatic]
    private static PyClass? _owner;

    [ThreadStatic]
    private static object? _self;

    /// <summary>当前方法定义类；没有正在执行的方法时为 <c>null</c>。</summary>
    internal static PyClass? CurrentOwner => _owner;

    /// <summary>当前接收者。</summary>
    internal static object? CurrentSelf => _self;

    /// <summary>设置当前方法上下文（调用方负责在 finally 中恢复旧值）。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetCurrent(PyClass? owner, object? self)
    {
        _owner = owner;
        _self = self;
    }

    /// <summary>零参数 `super()`；只在真正调用时分配代理对象。</summary>
    public static object MakeSuper() =>
        _owner is null ? throw new PyRuntimeError("super(): no arguments") : new PySuper(_owner, _self);
}

/// <summary>codegen 为每个用户异常类生成的 CLR 工厂委托。</summary>
/// <param name="cls">Python 类对象。</param>
/// <param name="args">构造实参。</param>
public delegate PyBaseException PyExceptionFactory(PyClass cls, object?[] args);
