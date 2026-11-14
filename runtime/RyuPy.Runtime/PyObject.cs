// Python 驻留类型身份及动态对象属性协议；专有模块异常保留各自限定类型键。
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
    public override string ToString() => $"<class '{(Name == "JSONDecodeError" ? "json.decoder." : Name == "error" ? "re." : Name == "BadGzipFile" ? "gzip." : Name == "BadZipFile" ? "zipfile." : Name == "GetoptError" ? "getopt." : Name == "ParseError" ? "xml.etree.ElementTree." : PyOptParseExceptions.IsType(Name) ? "optparse." : PyTyping.IsMetadataType(Name) ? "typing." : "")}{Name}'>";
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
public static partial class PyObject
{
    /// <summary><c>type(value)</c>。</summary>
    /// <param name="value">任意 Python 值。</param>
    public static object TypeOf(object? value) => value switch
    {
        PyBaseException { ForeignObject: { } origin } => origin.ForeignType,
        IPyForeignObject foreign => foreign.ForeignType,
        // 裸object身份不受原生类型短名注册表影响。
        PyBaseObject => PyBaseObject.Type,
        PyNativeObject native => native is PyTypingTypeVar ? PyTypingTypeVar.Type
            : native is PyStarMap ? PyStarMap.Type
            : native is PyCount ? PyCount.Type
            : (object?)PyNativeType.LookupByName(native.TypeName) ?? PyType.Get(native.TypeName),
        PyNativeType => PyType.Get("type"),
        PySuper => PyLexicalSuper.Type,
        PyUrlResult result => PyNativeType.LookupByName(result.Parsed ? "ParseResult" : "SplitResult")!,
        PyNamedTupleInstance instance => instance.Class,
        PyInstance instance => instance.Class,
        PyModule { ForeignObject: { } foreign } => foreign.ForeignType,
        PyModule => PyType.Get("module"),
        PyClass cls when cls.HasEnumMetadata => PyEnum.MetaType,
        PyClass => PyType.Get("type"),
        PyBaseException exception when exception.PythonClass is not null => exception.PythonClass,
        PyBoundMethod => PyType.Get("method"),
        PyFunction => PyType.Get("function"),
        PyStaticMethod => PyType.Get("staticmethod"),
        PyClassMethod => PyType.Get("classmethod"),
        PyProperty => PyType.Get("property"),
        _ => PyType.Get(PyRepr.TypeName(value)),
    };

    /// <summary>读取属性；不存在的属性抛 <c>AttributeError</c>。</summary>
    /// <param name="obj">被访问的对象。</param>
    /// <param name="name">属性名。</param>
    public static object? GetAttr(object? obj, string name) => obj switch
    {
        PyBaseException { ForeignObject: PyNativeObject origin } => origin.GetAttr(name),
        PySlice slice => slice.GetAttribute(name),
        PyNativeType native => native.GetAttr(name),
        PyNativeObject native => native.GetAttr(name),
        PyModule module => module.GetAttr(name),
        PyUrlResult result => result.Attribute(name),
        PyNamedTupleInstance instance => PyNamedTupleLayout.GetInstanceAttribute(instance, name),
        PyInstance instance => GetInstanceAttr(instance, name),
        PyClass cls => GetClassAttr(cls, name),
        PySuper super => GetSuperAttr(super, name),
        PyType type => GetTypeAttr(type, name),
        PyNotImplemented when name == "__class__" => PyType.Get("NotImplementedType"),
        PyBaseException exception => GetExceptionAttr(exception, name),
        PyFunction function => function.GetMetadata(name),
        PyBuiltinFunction function => function.GetMetadata(name),
        PyBuiltinMethod method => method.GetMetadata(name),
        // 精确内建载体的根类型属性；前面的用户/原生类分派保留真实描述符优先级。
        PyList or PyTuple or PyDict or PySet or PyFrozenSet or PyByteArray or PyBytes or string
            or int or long or System.Numerics.BigInteger or bool or double or PyNone or null
            when name == "__class__" => TypeOf(obj),
        PyDictView { UseLayoutProtocol: true } when name == "mapping"
            => throw PyDictLayout.Own(new PyNotImplementedError("dict view mapping metadata is not supported")),
        PyList or PyTuple or PyDict or PySet or PyFrozenSet or PyDictView or PyByteArray or PyBytes or string or PyGenerator
            => PyOps.PrepareMethod(obj, name),
        // 方法和包装的真实底层身份优先于函数普通字典中可伪装的同名属性。
        PyBoundMethod method when name == "__func__" => method.Target,
        PyStaticMethod method when name == "__func__" => method.Value,
        PyClassMethod method when name == "__func__" => method.Value,
        PyBoundMethod method => PyTypingIo.BoundMetadata(method, name),
        PyProperty property => property.GetAccessor(name),
        _ => throw new PyAttributeError(
            $"'{PyRepr.TypeName(obj)}' object has no attribute '{name}'"
        ),
    };

    /// <summary>写属性：实例字典、property setter 或类命名空间。</summary>
    public static void SetAttr(object? obj, string name, object? value)
    {
        switch (obj)
        {
            case PyBaseException { ForeignObject: PyNativeObject origin } error:
                origin.SetAttr(name, value); error.ForeignObject.SynchronizeException(error, false); return;
            case PySlice:
                PySliceMembers.RejectMutation(name, value, deleting: false);
                return;
            case PyType type when ReferenceEquals(type, PySlice.Type):
                PySliceMembers.RejectTypeMutation(name, deleting: false);
                return;
            case PyNamedTupleInstance namedTuple:
                if (PyTypingNamedTupleMembers.TryWriteProperty(namedTuple, name, value, deleting: false)) return;
                PyNamedTupleLayout.RejectInstanceMutation(namedTuple, name, deleting: false);
                return;
            case PyType type when ReferenceEquals(type, PyBaseObject.Type):
                PyBaseObject.RejectTypeMutation(name);
                return;
            case PyNativeObject native:
                native.SetAttr(name, value);
                return;
            case PyFunction function:
                function.SetMetadata(name, value);
                return;
            case PyModule module:
                module.SetAttr(name, value);
                return;
            case PyClass cls:
                cls.SetAttr(name, value);
                return;
            case PyInstance instance:
                SetInstanceAttr(instance, name, value);
                return;
            case PyBaseException exception when name == "__suppress_context__":
                exception.SuppressContextFlag = value is bool flag ? flag
                    : throw new PyTypeError("attribute value type must be bool");
                return;
            case PySyntaxError:
                throw PyXmlTreeBuilderErrors.Unsupported("SyntaxError/ParseError attribute mutation");
            case PyGetoptError getopt:
                getopt.Write(name, value);
                return;
            case PyOptParseError error:
                error.Write(name, value);
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

    /// <summary>普通属性删除，沿既有用户协议与布局守卫。</summary>
    public static void DelAttr(object? obj, string name) => DeleteObjectAttribute(obj, name, false);

    /// <summary>
    /// 属性绑定：把类链上的原始值变成实例可调用的形态。
    /// </summary>
    /// <param name="raw">类属性原值。</param>
    /// <param name="owner">属性定义所在的类。</param>
    /// <param name="self">接收者（实例或类对象）。</param>
    public static object? Bind(object? raw, PyClass owner, object? self) => raw switch
    {
        IPyNativeDescriptor descriptor => descriptor.Bind(owner, self),
        PyFunction fn => new PyBoundMethod(owner, self, fn),
        PyStaticMethod method => method.Value,
        PyClassMethod method => new PyBoundMethod(owner, ClassMethodReceiver(owner, self), method.Value),
        PyProperty property => property,
        _ => PyBaseObjectMethods.BindRootHash(raw, self),
    };

    // classmethod 绑定到访问者的实际类，不是定义方法的基类，也不是访问它的实例。
    private static PyClass ClassMethodReceiver(PyClass owner, object? self) => self switch
    {
        PyClass cls => cls,
        PyInstance instance => instance.Class,
        PyBaseException { PythonClass: not null } exception => exception.PythonClass,
        _ => owner,
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
        if (target is PyClassMethod) self = ClassMethodReceiver(owner, self);
        PyLexicalClassGuard.Receiver(self);
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
        PyLexicalClassGuard.Receiver(self);
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

    /// <summary>值是否可能带 Python 层方法（用户类实例、带 Python 类的异常）。</summary>
    /// <remarks>热路径先用它排除内建值，避免为 <see cref="TryMethod"/> 构造实参数组。</remarks>
    public static bool MayHaveMethods(object? value) =>
        value is PyInstance or PyNativeObject or PyHTTPError || value is PyBaseException { PythonClass: not null };

    /// <summary>单实参版本的 <see cref="TryMethod"/>：内建值直接返回 false，不分配实参数组。</summary>
    public static bool TryMethod1(object? self, string name, object? arg, out object? result)
    {
        if (!MayHaveMethods(self))
        {
            result = null;
            return false;
        }
        return TryMethod(self, name, [arg], out result);
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
        // 内建异常槽返回原msg；公开str负责验证返回对象而非提前剥离str子类。
        if (self is PyGetoptError getopt && name == "__str__")
        {
            result = PyOps.CallValue(getopt.StrMethod(), args, [], []);
            return true;
        }
        if (self is PyNativeObject native)
        {
            if (!TryGetNativeSpecial(native, name, out object? method)) { result = null; return false; }
            result = PyOps.CallValue(method, args, [], []);
            return true;
        }
        if (self is PyHTTPError httpError && name is "__enter__" or "__exit__")
        {
            result = PyOps.CallValue(httpError.Attribute(name), args, [], []);
            return true;
        }
        if (self is PyInstance { Class.HasDictC3Layout: true } mixed)
        {
            if (mixed.Class.TryResolveDictC3(name, out var resolved))
            { result = resolved.Invoke(mixed, args, [], []); return true; }
            result = null; return false;
        }
        if (self is PyInstance genericInstance) genericInstance.Class.GuardInheritedGenericProtocol(name);
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
    public static bool IsInstance(object? value, PyClass cls) => cls.IsTypingGenericRoot
        ? PyTypingGenericMetadata.InstanceOf(value, cls) : cls.IsTypingIoFamily
        ? value is PyInstance familyInstance && familyInstance.Class.IsSubclassOf(cls) : value switch
    {
        PyNamedTupleInstance instance => instance.Class.IsSubclassOf(cls),
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
        instance.Class.RequireLexicalFactoryReady();
        if (PyTypingGenericMetadata.InstanceAttribute(instance, name, out object? genericValue)) return genericValue;
        if (PyTypingIo.TryInstanceMetadata(instance, name, out object? ioValue)) return ioValue;
        if (PyStringLayout.TryInstanceMetadata(instance, name, out object? stringValue)) return stringValue;
        if (PyListLayout.TryInstanceMetadata(instance, name, out object? listValue)) return listValue;
        if (PyDictLayout.TryInstanceMetadata(instance, name, out object? dictValue)) return dictValue;
        if (PyEnum.TryGetMemberAttr(instance, name, out object? enumValue)) return enumValue;
        // 未被用户类项遮蔽的根__class__是数据描述符，压过同名实例字段；None也是有效覆盖。
        if (name == "__class__" && !instance.Class.TryLookup(name, out _, out _)) return instance.Class;
        // 未知数据描述符在实例字段之前消费；普通非数据描述符延后到类链绑定处。
        if (instance.Class.IsTypingGenericFamily && instance.Class.TryLookup(name, out _, out object? candidate))
            PyTypingGenericProtocols.GuardDescriptorRead(instance.Class, candidate, dataOnly: true);
        // 1. 数据描述符（property）优先，即使实例字典里有同名值。
        //    没有描述符的类直接跳过这次类查找。
        if (instance.Class.HasProperties
            && instance.Class.TryLookup(name, out PyClass dataOwner, out object? data)
            && data is PyProperty property)
        {
            if (property.Getter is null)
            {
                throw InstanceAttributeError(instance, $"property '{name}' of '{instance.Class.Name}' object has no getter");
            }
            return InvokeBound(property.Getter, dataOwner, instance, [], [], []);
        }
        if (instance.Class.HasSlotLayout)
        {
            if (instance.Class.TryLookup(name, out _, out object? fixedRaw) && fixedRaw is PyInstanceSlotDescriptor fixedSlot) return fixedSlot.Read(instance);
            if (name == "__dict__") return instance.SlotDictionary;
        }
        // 2. 实例属性权威：旧数组槽或新族真实dict。
        if (instance.TryOwnAttribute(name, out object? value))
        {
            return value;
        }
        // 3. 新native族的真实owner绑定；实例字段仍先于非数据native方法。
        if (instance.Class.HasDictC3Layout && instance.Class.TryResolveDictC3(name, out var resolved))
            return resolved.Bind(instance);
        // 旧族保留用户owner快路。
        if (instance.Class.TryLookup(name, out PyClass owner, out object? raw))
        {
            PyTypingGenericProtocols.GuardDescriptorRead(instance.Class, raw);
            return Bind(raw, owner, instance);
        }
        if (instance.Class.TryRootConstructionAttribute(name, instance, out object? rootMethod)) return rootMethod;
        // 4. `__getattr__` 兜底
        if (instance.Class.TryLookup("__getattr__", out PyClass getattrOwner, out object? getattr))
        {
            instance.Class.GuardInheritedGenericProtocol("__getattr__");
            return InvokeBound(
                getattr,
                getattrOwner,
                instance,
                [name],
                [],
                []
            );
        }
        throw InstanceAttributeError(instance,
            $"'{(instance.Class.IsTypingGenericRoot ? "typing.Generic" : instance.Class.Name)}' object has no attribute '{name}'"
        );
    }

    // 仅实例查找自身的新错误；str子类提前捕获不包住用户getter，普通实例错误路径不变。
    private static PyAttributeError InstanceAttributeError(PyInstance instance, string message)
    {
        var error = new PyAttributeError(message);
        if (instance is PyStringInstance or PyListInstance or PyDictInstance) PyErrors.PrepareRaise(error);
        return error;
    }

    /// <summary>读取类对象上的属性。</summary>
    private static object? GetClassAttr(PyClass cls, string name)
    {
        cls.RequireLexicalFactoryReady();
        if (PyTypingGenericMetadata.ClassAttribute(cls, name, out object? genericValue)) return genericValue;
        if (PyTypingIo.TryClassMetadata(cls, name, out object? ioValue)) return ioValue;
        if (PyNamedTupleLayout.TryClassMetadata(cls, name, out object? tupleValue)) return tupleValue;
        if (PyStringLayout.TryClassMetadata(cls, name, out object? stringValue)) return stringValue;
        if (PyListLayout.TryClassMetadata(cls, name, out object? listValue)) return listValue;
        if (PyDictLayout.TryClassMetadata(cls, name, out object? dictValue)) return dictValue;
        if (PyEnum.TryGetClassAttr(cls, name, out object? enumValue)) return enumValue;
        switch (name)
        {
            case "__class__":
                return TypeOf(cls); // 类字典的同名项属于实例读取，不遮蔽类对象的真实元类。
            case "__name__":
                return cls.Name;
            case "__qualname__":
                return cls.Attributes.TryGetValue(name, out object? qualified) ? qualified : cls.Name;
            case "__module__":
                if (cls.Attributes.TryGetValue(name, out object? module)) return module;
                return cls.NativeDefinitionModule is null ? "__main__" : cls.NativeModuleValue;
            case "__bases__":
                return cls.DirectBasesTuple();
            case "__mro__":
                return cls.MroTuple();
            case "__base__":
                return (object?)cls.BaseClass ?? (cls.BuiltinBase is { } baseName
                    ? PyType.Get(baseName) : PyBaseObject.Type);
            case "__dict__":
                {
                    var dict = new PyDict();
                    foreach (var (key, value) in cls.Attributes)
                    {
                        dict.Set(key, value);
                    }
                    return cls.IsExportedExceptionFrozen ? PyMappingProxy.Create([dict], [], []) : dict;
                }
        }
        if (cls.HasDictC3Layout && cls.TryResolveDictC3(name, out var resolved)) return resolved.Bind(cls, classAccess: true);
        if (!cls.TryLookup(name, out PyClass owner, out object? raw))
        {
            if (cls.TryRootConstructionAttribute(name, null, out object? rootMethod)) return rootMethod;
            throw new PyAttributeError(
                $"type object '{(cls.IsTypingGenericRoot ? "typing.Generic" : cls.Name)}' has no attribute '{name}'"
            );
        }
        PyTypingGenericProtocols.GuardDescriptorRead(cls, raw);
        return raw switch
        {
            IPyNativeDescriptor descriptor => descriptor.Bind(owner, null),
            // 类对象访问普通函数得到未绑定函数（Python 3 语义）
            PyFunction function => function,
            PyStaticMethod method => method.Value,
            PyClassMethod method => new PyBoundMethod(owner, cls, method.Value),
            // property 在类上访问时返回描述符本身
            _ => raw,
        };
    }


}
