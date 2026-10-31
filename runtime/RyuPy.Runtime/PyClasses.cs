using System;
using System.Collections.Generic;

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
public sealed class PyClass
{
    /// <summary>类名（`__name__` 与错误消息使用）。</summary>
    public string Name { get; }

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

    /// <summary>属性查找缓存上限。</summary>
    private const int CacheLimit = 256;

    private readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);

    private readonly record struct CacheEntry(long Version, PyClass Owner, object? Value);

    /// <summary>创建一个类对象。</summary>
    public PyClass(string name) => Name = name;

    /// <summary>是否属于 `BaseException` 家族。</summary>
    public bool IsException => BuiltinBase is not null || BaseClass?.IsException == true;

    /// <summary>写入 / 替换类属性。</summary>
    public void SetAttr(string name, object? value)
    {
        Attributes[name] = value;
        Touch();
    }

    /// <summary>删除类属性；不存在时返回 <c>false</c>。</summary>
    public bool DeleteAttr(string name)
    {
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

    /// <summary>
    /// 沿类链查找原始属性（不绑定、不调用 property）。
    /// </summary>
    /// <remarks>
    /// 缓存失效靠全局版本：任何类的任何属性修改都会让条目过期，宁可多查一次也不能
    /// 返回旧绑定（陷阱 #22）。条目数超过上限时整体清空，容量有界。
    /// </remarks>
    public bool TryLookup(string name, out PyClass owner, out object? value)
    {
        long version = _globalVersion;
        if (_cache.TryGetValue(name, out CacheEntry cached) && cached.Version == version)
        {
            owner = cached.Owner;
            value = cached.Value;
            return true;
        }
        if (Walk(this, name, out owner, out value))
        {
            if (_cache.Count >= CacheLimit)
            {
                _cache.Clear();
            }
            _cache[name] = new CacheEntry(version, owner, value);
            return true;
        }
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

        if (kwNames.Length > 0)
        {
            throw new PyTypeError(
                $"{Name}() got an unexpected keyword argument '{kwNames[0]}'"
            );
        }
        var instance = new PyInstance(this);
        if (TryLookup("__init__", out PyClass initOwner, out object? initMethod))
        {
            PyObject.InvokeBound(initMethod, initOwner, instance, args, kwNames, kwValues);
        }
        else if (args.Length > 0)
        {
            throw new PyTypeError($"{Name}() takes no arguments");
        }
        return instance;
    }

    /// <summary>与 CPython 一致的 <c>&lt;class 'Name'&gt;</c> 形式。</summary>
    public override string ToString() => $"<class '{Name}'>";
}

/// <summary>普通类实例：类引用 + 实例属性字典（保持插入顺序）。</summary>
public sealed class PyInstance
{
    /// <summary>所属类。</summary>
    public PyClass Class { get; }

    /// <summary>实例属性。</summary>
    public Dictionary<string, object?> Attributes { get; } = new(StringComparer.Ordinal);

    /// <summary>创建实例。</summary>
    public PyInstance(PyClass cls) => Class = cls;

    /// <summary>默认 `repr` 的地址部分；用运行时身份代替真实地址（D002）。</summary>
    public string DefaultRepr() =>
        $"<{Class.Name} object at 0x{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this):x}>";

    /// <inheritdoc />
    public override string ToString() => PyRepr.Str(this);
}

/// <summary>`@property` 描述符。</summary>
public sealed class PyProperty
{
    /// <summary>getter（可调用值）。</summary>
    public object? Getter { get; }
    /// <summary>setter（可调用值）；没有时为 <c>null</c>。</summary>
    public object? Setter { get; }

    /// <summary>构造属性描述符。</summary>
    public PyProperty(object? getter, object? setter = null)
    {
        Getter = getter;
        Setter = setter;
    }

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
    private static PySuper? _current;

    /// <summary>执行一个绑定方法；期间登记上下文供 `super()` 使用。</summary>
    public static object? Run(PyClass owner, object? self, Func<object?> body)
    {
        PySuper? previous = _current;
        _current = new PySuper(owner, self);
        try
        {
            return body();
        }
        finally
        {
            _current = previous;
        }
    }

    /// <summary>零参数 `super()`。</summary>
    public static object MakeSuper() =>
        _current ?? throw new PyRuntimeError("super(): no arguments");
}

/// <summary>codegen 为每个用户异常类生成的 CLR 工厂委托。</summary>
/// <param name="cls">Python 类对象。</param>
/// <param name="args">构造实参。</param>
public delegate PyBaseException PyExceptionFactory(PyClass cls, object?[] args);

/// <summary>类与属性协议的运行时入口（codegen 只依赖这几个方法）。</summary>
public static partial class PyOps
{
    /// <summary>创建类对象。</summary>
    /// <param name="name">类名。</param>
    /// <param name="bases">基类列表（0 或 1 个）。</param>
    /// <param name="factory">用户异常类的 CLR 工厂；普通类为 <c>null</c>。</param>
    public static object MakeClass(string name, object?[] bases, object? factory)
    {
        var cls = new PyClass(name);
        if (bases.Length > 1)
        {
            throw new PyTypeError("多重继承暂不支持");
        }
        if (bases.Length == 1)
        {
            switch (bases[0])
            {
                case PyClass baseClass:
                    cls.BaseClass = baseClass;
                    break;
                case PyType type when type.Name == "object":
                    break;
                case PyType type:
                    cls.BuiltinBase = type.Name;
                    break;
                default:
                    throw new PyTypeError(
                        $"class base must be a class, not '{PyRepr.TypeName(bases[0])}'"
                    );
            }
        }
        cls.ExceptionFactory = factory as PyExceptionFactory;
        return cls;
    }

    /// <summary><c>obj.attr = value</c>。</summary>
    public static void SetAttr(object? obj, string name, object? value) =>
        PyObject.SetAttr(obj, name, value);

    /// <summary><c>del obj.attr</c>。</summary>
    public static void DelAttr(object? obj, string name) => PyObject.DelAttr(obj, name);

    /// <summary>应用类内修饰器：1 = property，2 = staticmethod，3 = classmethod。</summary>
    public static object Decorate(int kind, object? value) => kind switch
    {
        1 => new PyProperty(value),
        2 => new PyStaticMethod(value),
        3 => new PyClassMethod(value),
        _ => throw new PyRuntimeError($"unknown decorator kind {kind}"),
    };
}
