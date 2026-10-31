using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>
/// Python 类型对象的最小表示（<c>type(x)</c> 的结果）。
/// </summary>
/// <remarks>
/// 内建类型对象按名字驻留：<c>type(True) is bool</c>、<c>int is int</c> 这类身份比较
/// 依赖它。用户类不用 <see cref="PyType"/>，而是直接使用 <see cref="PyClass"/> 对象，
/// 这样 <c>type(instance) is SomeClass</c> 仍是引用比较。
/// </remarks>
public sealed class PyType
{
    private static readonly ConcurrentDictionary<string, PyType> Cache = new(StringComparer.Ordinal);

    /// <summary>类型名，例如 <c>ValueError</c>。</summary>
    public string Name { get; }

    private PyType(string name) => Name = name;

    /// <summary>取驻留的类型对象。</summary>
    public static PyType Get(string name) => Cache.GetOrAdd(name, static n => new PyType(n));

    /// <summary>与 CPython 一致的 <c>repr</c> 形式。</summary>
    public override string ToString() => $"<class '{Name}'>";
}

/// <summary>
/// 动态对象的属性读写、绑定方法调用与类型判定（M2.5）。
/// </summary>
/// <remarks>
/// 属性查找优先级与 CPython 一致：
/// <list type="number">
/// <item>数据描述符（这里只有 <c>property</c>）优先于实例字典；</item>
/// <item>实例字典；</item>
/// <item>类链上的非数据描述符（方法 / staticmethod / classmethod）与普通值。</item>
/// </list>
/// 普通方法在读取时绑定接收者，`super()` 依赖绑定方法记录的定义类。
/// </remarks>
public static class PyObject
{
    /// <summary><c>type(value)</c>。</summary>
    /// <param name="value">任意 Python 值。</param>
    public static object TypeOf(object? value) => value switch
    {
        PyInstance instance => instance.Class,
        PyModule => PyType.Get("module"),
        PyClass => PyType.Get("type"),
        PyBaseException exception when exception.PythonClass is not null => exception.PythonClass,
        PyBoundMethod or PyFunction => PyType.Get("method"),
        PyProperty => PyType.Get("property"),
        _ => PyType.Get(PyRepr.TypeName(value)),
    };

    /// <summary>读取属性；不存在的属性抛 <c>AttributeError</c>。</summary>
    /// <param name="obj">被访问的对象。</param>
    /// <param name="name">属性名。</param>
    public static object? GetAttr(object? obj, string name) => obj switch
    {
        PyModule module => module.GetAttr(name),
        PyInstance instance => GetInstanceAttr(instance, name),
        PyClass cls => GetClassAttr(cls, name),
        PySuper super => GetSuperAttr(super, name),
        PyType type => GetTypeAttr(type, name),
        PyBaseException exception => GetExceptionAttr(exception, name),
        _ => throw new PyAttributeError(
            $"'{PyRepr.TypeName(obj)}' object has no attribute '{name}'"
        ),
    };

    /// <summary>写属性：实例字典、property setter 或类命名空间。</summary>
    public static void SetAttr(object? obj, string name, object? value)
    {
        switch (obj)
        {
            case PyModule module:
                module.SetAttr(name, value);
                return;
            case PyClass cls:
                cls.SetAttr(name, value);
                return;
            case PyInstance instance:
                SetInstanceAttr(instance, name, value);
                return;
            case PyBaseException exception when exception.PythonClass is not null:
                exception.UserAttributes[name] = value;
                return;
            default:
                throw new PyAttributeError(
                    $"'{PyRepr.TypeName(obj)}' object has no attribute '{name}'"
                );
        }
    }

    /// <summary>删除属性。</summary>
    public static void DelAttr(object? obj, string name)
    {
        switch (obj)
        {
            case PyClass cls:
                if (!cls.DeleteAttr(name))
                {
                    throw new PyAttributeError(
                        $"type object '{cls.Name}' has no attribute '{name}'"
                    );
                }
                return;
            case PyInstance instance:
                if (instance.Class.TryGetInstanceSlot(name, out int slot) && instance.IsSlotSet(slot))
                {
                    instance.ClearSlot(slot);
                    return;
                }
                throw new PyAttributeError(
                    $"'{instance.Class.Name}' object has no attribute '{name}'"
                );
            case PyBaseException exception when exception.PythonClass is not null:
                if (exception.UserAttributes.Remove(name))
                {
                    return;
                }
                throw new PyAttributeError(
                    $"'{exception.PyTypeName}' object has no attribute '{name}'"
                );
            default:
                throw new PyAttributeError(
                    $"'{PyRepr.TypeName(obj)}' object has no attribute '{name}'"
                );
        }
    }

    /// <summary>
    /// 属性绑定：把类链上的原始值变成实例可调用的形态。
    /// </summary>
    /// <param name="raw">类属性原值。</param>
    /// <param name="owner">属性定义所在的类。</param>
    /// <param name="self">接收者（实例或类对象）。</param>
    public static object? Bind(object? raw, PyClass owner, object? self) => raw switch
    {
        PyFunction fn => new PyBoundMethod(owner, self, fn),
        PyStaticMethod method => method.Value,
        PyClassMethod method => new PyBoundMethod(owner, self, method.Value),
        PyProperty property => property,
        _ => raw,
    };

    /// <summary>调用一个绑定方法：把接收者作为第一个位置实参。</summary>
    public static object? InvokeBound(
        object? target,
        PyClass owner,
        object? self,
        object?[] args,
        string[] kwNames,
        object?[] kwValues
    )
    {
        // 绝大多数方法不调用 `super()`，不需要维护上下文，直接调用。
        if (target is PyFunction plain && !plain.UsesSuper)
        {
            return plain.Invoke(Prepend(self, args), kwNames, kwValues);
        }
        // 不用闭包：`super()` 上下文用两个 ThreadStatic 字段保存，避免每次调用分配
        // `PySuper` 与捕获 lambda。嵌套调用通过局部变量恢复。
        PyClass? previousOwner = PyMethodContext.CurrentOwner;
        object? previousSelf = PyMethodContext.CurrentSelf;
        PyMethodContext.SetCurrent(owner, self);
        try
        {
            if (target is PyFunction function)
            {
                return function.Invoke(Prepend(self, args), kwNames, kwValues);
            }
            if (target is PyStaticMethod method)
            {
                return PyOps.CallValue(method.Value, args, kwNames, kwValues);
            }
            if (target is PyClassMethod classMethod)
            {
                return PyOps.CallValue(
                    classMethod.Value,
                    Prepend(self, args),
                    kwNames,
                    kwValues
                );
            }
            return PyOps.CallValue(target, Prepend(self, args), kwNames, kwValues);
        }
        finally
        {
            PyMethodContext.SetCurrent(previousOwner, previousSelf);
        }
    }

    /// <summary>
    /// 调用已经准备好的绑定方法参数（`[self, arg...]`），供 PIC 快速路径使用。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static object? InvokePrepared(
        PyFunction function,
        PyClass owner,
        object? self,
        object?[] args,
        string[] kwNames,
        object?[] kwValues
    )
    {
        if (!function.UsesSuper)
        {
            return function.Invoke(args, kwNames, kwValues);
        }
        PyClass? previousOwner = PyMethodContext.CurrentOwner;
        object? previousSelf = PyMethodContext.CurrentSelf;
        PyMethodContext.SetCurrent(owner, self);
        try
        {
            return function.Invoke(args, kwNames, kwValues);
        }
        finally
        {
            PyMethodContext.SetCurrent(previousOwner, previousSelf);
        }
    }

    /// <summary>
    /// 尝试调用对象上的魔术方法（`__repr__` / `__eq__` / `__add__` …）。
    /// </summary>
    /// <returns>找到并调用了方法返回 <c>true</c>；结果可能合法地为 <c>None</c>。</returns>
    public static bool TryMethod(
        object? self,
        string name,
        object?[] args,
        out object? result
    )
    {
        if (self is PyInstance instance
            && instance.Class.TryLookup(name, out PyClass owner, out object? raw))
        {
            result = InvokeBound(raw, owner, instance, args, [], []);
            return true;
        }
        if (self is PyBaseException exception
            && exception.PythonClass is not null
            && exception.PythonClass.TryLookup(name, out PyClass excOwner, out object? excRaw))
        {
            result = InvokeBound(excRaw, excOwner, exception, args, [], []);
            return true;
        }
        result = null;
        return false;
    }

    /// <summary>按名字调用实例 / 类上的方法（`obj.name(...)`）。</summary>
    public static object? InvokeMethod(
        object? self,
        string name,
        object?[] args,
        string[] kwNames,
        object?[] kwValues
    )
    {
        object? attribute = GetAttr(self, name);
        return PyOps.CallValue(attribute, args, kwNames, kwValues);
    }

    /// <summary><c>isinstance(value, cls)</c> 的类部分。</summary>
    public static bool IsInstance(object? value, PyClass cls) => value switch
    {
        PyInstance instance => instance.Class.IsSubclassOf(cls),
        PyBaseException exception => exception.PythonClass is not null
            ? exception.PythonClass.IsSubclassOf(cls)
            : string.Equals(exception.PyTypeName, cls.Name, StringComparison.Ordinal),
        // dataclass 的实例是 codegen 生成的 CLR 类，没有 PyClass 链接，按类型名匹配。
        _ => value is not null
            && string.Equals(value.GetType().Name, cls.Name, StringComparison.Ordinal),
    };

    /// <summary>读取实例属性。</summary>
    private static object? GetInstanceAttr(PyInstance instance, string name)
    {
        // 1. 数据描述符（property）优先，即使实例字典里有同名值。
        //    没有描述符的类直接跳过这次类查找。
        if (instance.Class.HasProperties
            && instance.Class.TryLookup(name, out PyClass dataOwner, out object? data)
            && data is PyProperty property)
        {
            if (property.Getter is null)
            {
                throw new PyAttributeError($"property '{name}' of '{instance.Class.Name}' object has no getter");
            }
            return InvokeBound(property.Getter, dataOwner, instance, [], [], []);
        }
        // 2. 实例属性槽位
        if (instance.Class.TryGetInstanceSlot(name, out int instanceSlot)
            && instance.TryGetSlot(instanceSlot, out object? value))
        {
            return value;
        }
        // 3. 类链上的方法 / 值
        if (instance.Class.TryLookup(name, out PyClass owner, out object? raw))
        {
            return Bind(raw, owner, instance);
        }
        // 4. `__getattr__` 兜底
        if (instance.Class.TryLookup("__getattr__", out PyClass getattrOwner, out object? getattr))
        {
            return InvokeBound(
                getattr,
                getattrOwner,
                instance,
                [name],
                [],
                []
            );
        }
        throw new PyAttributeError(
            $"'{instance.Class.Name}' object has no attribute '{name}'"
        );
    }

    /// <summary>读取类对象上的属性。</summary>
    private static object? GetClassAttr(PyClass cls, string name)
    {
        switch (name)
        {
            case "__name__":
            case "__qualname__":
                return cls.Name;
            case "__module__":
                return "__main__";
            case "__bases__":
                if (cls.BaseClass is not null)
                {
                    return new PyTuple([cls.BaseClass]);
                }
                if (cls.BuiltinBase is { } builtin)
                {
                    return new PyTuple([PyType.Get(builtin)]);
                }
                return new PyTuple([]);
            case "__dict__":
                {
                    var dict = new PyDict();
                    foreach (var (key, value) in cls.Attributes)
                    {
                        dict.Set(key, value);
                    }
                    return dict;
                }
        }
        if (!cls.TryLookup(name, out PyClass owner, out object? raw))
        {
            throw new PyAttributeError(
                $"type object '{cls.Name}' has no attribute '{name}'"
            );
        }
        return raw switch
        {
            // 类对象访问普通函数得到未绑定函数（Python 3 语义）
            PyFunction function => function,
            PyStaticMethod method => method.Value,
            PyClassMethod method => new PyBoundMethod(owner, cls, method.Value),
            // property 在类上访问时返回描述符本身
            _ => raw,
        };
    }

    /// <summary>读取 `super()` 代理的属性：从定义类的下一个基类开始。</summary>
    private static object? GetSuperAttr(PySuper super, string name)
    {
        for (PyClass? current = super.Owner.BaseClass; current is not null; current = current.BaseClass)
        {
            if (current.Attributes.TryGetValue(name, out object? raw))
            {
                if (raw is PyProperty property)
                {
                    return InvokeBound(property.Getter, current, super.Self, [], [], []);
                }
                return Bind(raw, current, super.Self);
            }
        }
        throw new PyAttributeError(
            $"'super' object has no attribute '{name}'"
        );
    }

    /// <summary>写实例属性；property setter 优先。</summary>
    private static void SetInstanceAttr(PyInstance instance, string name, object? value)
    {
        if (instance.Class.HasProperties
            && instance.Class.TryLookup(name, out PyClass owner, out object? raw)
            && raw is PyProperty property)
        {
            if (property.Setter is null)
            {
                throw new PyAttributeError(
                    $"property '{name}' of '{instance.Class.Name}' object has no setter"
                );
            }
            InvokeBound(property.Setter, owner, instance, [value], [], []);
            return;
        }
        int slot = instance.Class.GetOrAddInstanceSlot(name);
        instance.SetSlot(slot, value);
    }

    private static object? GetTypeAttr(PyType type, string name) =>
        name switch
        {
            "__name__" or "__qualname__" => type.Name,
            _ => throw new PyAttributeError($"type object '{type.Name}' has no attribute '{name}'"),
        };

    private static object? GetExceptionAttr(PyBaseException exception, string name)
    {
        // 用户异常类上定义的方法 / 属性优先（`e.describe()` 之类）。
        if (exception.PythonClass is not null
            && exception.PythonClass.TryLookup(name, out PyClass owner, out object? raw))
        {
            return Bind(raw, owner, exception);
        }
        if (exception.PythonClass is not null
            && exception.UserAttributes.TryGetValue(name, out object? value))
        {
            return value;
        }
        return name switch
        {
            "args" => new PyTuple(exception.Args),
            "__cause__" => (object?)exception.Cause ?? PyNone.Instance,
            "__context__" => (object?)exception.Context ?? PyNone.Instance,
            "__class__" => (object?)exception.PythonClass ?? PyType.Get(exception.PyTypeName),
            "__traceback__" => PyNone.Instance,
            _ => throw new PyAttributeError(
                $"'{exception.PyTypeName}' object has no attribute '{name}'"
            ),
        };
    }

    private static object?[] Prepend(object? self, object?[] args)
    {
        var result = new object?[args.Length + 1];
        result[0] = self;
        Array.Copy(args, 0, result, 1, args.Length);
        return result;
    }
}
