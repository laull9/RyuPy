using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>普通 Enum 的稳定类/成员元数据；别名共享成员对象，不生成 CLR 枚举或逐实例反射。</summary>
public static partial class PyEnum
{
    private sealed class ClassInfo
    {
        internal bool IsFlag;
        internal System.Numerics.BigInteger FlagMask;
        internal readonly PyDict Members = new();
        internal readonly PyDict Values = new();
        internal readonly PyList Unique = new();
        internal readonly PyEnumMembers Proxy;
        internal ClassInfo() => Proxy = new PyEnumMembers(Members);
    }
    private sealed record MemberInfo(string? Name, object? Value);
    private static readonly ConditionalWeakTable<PyClass, ClassInfo> Classes = new();
    private static readonly ConditionalWeakTable<PyInstance, MemberInfo> Members = new();
    private static readonly Lazy<Dictionary<string, object?>> Attributes = new(Build);
    private static readonly PySignature ValueSignature = PySignature.Create("Enum", ["value"], ["pos"], ["0"]);

    /// <summary>缓存真实 Enum 基类，不把它伪装成构造函数。</summary>
    public static Dictionary<string, object?> CreateAttributes() => Attributes.Value;
    /// <summary>EnumType 的稳定类型元数据；只标识枚举类，不开放元类构造。</summary>
    public static PyNativeType MetaType => (PyNativeType)Attributes.Value["EnumType"]!;
    private static Dictionary<string, object?> Build()
    {
        var root = new PyClass("Enum"); Classes.Add(root, new ClassInfo()); root.HasEnumMetadata = true;
        PyClass flag = BuildFlagRoot(root);
        var meta = new PyNativeType("EnumType", "enum", null, IsEnumClass, "type");
        return new(StringComparer.Ordinal) { ["Enum"] = root, ["Flag"] = flag, ["EnumType"] = meta, ["EnumMeta"] = meta };
    }
    /// <summary>仅已验证原生 Enum 基类的 HIR 路径可调用；不借普通 MakeClass 绕过身份门禁。</summary>
    public static object Create(string name, object?[] bases, object? factory)
    {
        if (bases.Length != 1 || factory is not null and not PyNone
            || (!ReferenceEquals(bases[0], CreateAttributes()["Enum"]) && !ReferenceEquals(bases[0], CreateAttributes()["Flag"])))
            throw new PyNotImplementedError("Enum construction requires the stable native Enum or Flag base");
        return new PyClass(name) { BaseClass = (PyClass)bases[0]! };
    }

    /// <summary>编译器在类体完成、装饰器应用前调用；memberNames含源码顺序中的别名。</summary>
    public static object Finalize(object cls, object?[] memberNames)
    {
        if (cls is not PyClass type) throw new PyTypeError("Enum class must be a Python class");
        if (Classes.TryGetValue(type, out _)) throw new PyTypeError("Enum class already initialized");
        bool isFlag = ReferenceEquals(type.BaseClass, CreateAttributes()["Flag"]);
        if (!isFlag && !ReferenceEquals(type.BaseClass, CreateAttributes()["Enum"]))
            throw new PyNotImplementedError("only direct Enum subclasses are supported");
        var info = new ClassInfo { IsFlag = isFlag };
        foreach (object? rawName in memberNames)
        {
            if (rawName is not string name || !type.Attributes.TryGetValue(name, out object? value))
                throw new PyTypeError("invalid Enum member metadata");
            if (info.Members.Contains(name)) throw new PyTypeError($"{name} already defined");
            if (value is not (string or bool or long or int or System.Numerics.BigInteger or PyNone) && value is not null)
                throw new PyNotImplementedError("Enum values support scalar literals only");
            if (isFlag && (!PyInt.IsInt(value) || PyInt.ToBigInteger(value!).Sign < 0))
                throw new PyNotImplementedError("Flag members require non-negative integer values");
            PyInstance member;
            if (info.Values.TryGet(value, out object? existing)) member = (PyInstance)existing!;
            else
            {
                member = new PyInstance(type);
                Members.Add(member, new MemberInfo(name, value));
                info.Values.Set(value, member); info.Unique.Append(member);
            }
            info.Members.Set(name, member);
        }
        if (isFlag) FinalizeFlagInfo(info);
        // 元数据构建成功后再替换类成员；激活只读守卫前完成所有写入。
        foreach (var (name, member) in info.Members.Entries()) type.SetAttr((string)name!, member);
        Classes.Add(type, info); type.HasEnumMetadata = true;
        return type;
    }
    /// <summary>识别已完成初始化的枚举类（含根 Enum）。</summary>
    public static bool IsEnumClass(object? value) => value is PyClass { HasEnumMetadata: true };
    /// <summary>识别真实枚举成员，不按类名猜测。</summary>
    public static bool IsMember(object? value) => value is PyInstance member && member.Class.HasEnumMetadata && Members.TryGetValue(member, out _);

    /// <summary>枚举值查找，不重新实例化；拒绝未实现的 functional Enum API。</summary>
    public static object Construct(PyClass cls, object?[] args, string[] names, object?[] values)
    {
        ClassInfo info = Classes.GetValue(cls, _ => throw new PyTypeError("not an Enum class"));
        if (ReferenceEquals(cls, CreateAttributes()["Enum"]) || ReferenceEquals(cls, CreateAttributes()["Flag"]))
        {
            if (args.Length > 1 || Array.Exists(names, name => name != "value"))
                throw new PyNotImplementedError("functional Enum construction is not supported");
            _ = PyBinder.Bind(ValueSignature, args, names, values, []);
            throw new PyTypeError("Enum has no members defined");
        }
        object? value = PyBinder.Bind(ValueSignature, args, names, values, [])[0];
        if (value is PyInstance instance && ReferenceEquals(instance.Class, cls) && IsMember(instance)) return instance;
        if (info.Members.Count == 0) throw new PyTypeError($"{cls.Name} has no members defined");
        try { if (info.Values.TryGet(value, out object? found)) return found!; }
        catch (PyTypeError)
        {
            // 不可哈希查询值按 Python 行为退回逐项相等判断；不因输入列表泄漏 CLR 错误。
            foreach (object? item in info.Unique.Items)
                if (PyHash.Equal(Members.GetValue((PyInstance)item!, _ => throw new PyTypeError("invalid member")).Value, value)) return item!;
        }
        if (info.IsFlag) return ConstructFlag(cls, info, value);
        throw new PyValueError($"{PyRepr.Repr(value)} is not a valid {cls.Name}");
    }
    /// <summary>类名下标保留别名；缺失交给字典的 KeyError。</summary>
    public static object? GetItem(PyClass cls, object? key) => Classes.GetValue(cls, _ => throw new PyTypeError("not an Enum class")).Members.Get(key);
    /// <summary>类迭代只返回规范成员，按定义顺序跳过别名。</summary>
    public static IPyIterator Iterate(PyClass cls) => PyIter.Create(Classes.GetValue(cls, _ => throw new PyTypeError("not an Enum class")).Unique);
    /// <summary>成员数不包含别名。</summary>
    public static long Length(PyClass cls) => Classes.GetValue(cls, _ => throw new PyTypeError("not an Enum class")).Unique.Count;
    /// <summary>Python 3.12 的类包含测试接受成员或其原值。</summary>
    public static bool Contains(PyClass cls, object? value)
    {
        try { _ = Construct(cls, [value], [], []); return true; }
        catch (PyValueError) { return false; }
    }
    /// <summary>name/value与成员表为运行时受控属性，其它方法沿普通类机制绑定。</summary>
    public static bool TryGetMemberAttr(PyInstance member, string name, out object? value)
    {
        value = null;
        if (!member.Class.HasEnumMetadata || !Members.TryGetValue(member, out MemberInfo? info)) return false;
        if (name == "__class__") { value = member.Class; return true; }
        if (name == "name") { value = info.Name is null ? PyNone.Instance : info.Name; return true; }
        if (name == "value") { value = info.Value; return true; }
        return false;
    }
    /// <summary>只公开已实现的 EnumType 类协议。</summary>
    public static bool TryGetClassAttr(PyClass cls, string name, out object? value)
    {
        value = null;
        if (!cls.HasEnumMetadata || !Classes.TryGetValue(cls, out ClassInfo? info)) return false;
        value = name switch
        {
            "__class__" => MetaType,
            "__members__" => info.Proxy,
            "__iter__" => PyNativeFunctions.Create(name, [], [], 0, _ => Iterate(cls)),
            "__len__" => PyNativeFunctions.Create(name, [], [], 0, _ => Length(cls)),
            "__getitem__" => PyNativeFunctions.Create(name, ["name"], [null], 1, a => GetItem(cls, a[0])),
            "__contains__" => PyNativeFunctions.Create(name, ["value"], [null], 1, a => Contains(cls, a[0])),
            _ => null,
        };
        return value is not null;
    }
    /// <summary>类成员与成员表不可重绑定或删除；普通新增类属性不受影响。</summary>
    public static void GuardClassMutation(PyClass cls, string name)
    {
        if (!cls.HasEnumMetadata || !Classes.TryGetValue(cls, out ClassInfo? info)) return;
        if (info.Members.Contains(name) || name == "__members__")
            throw new PyAttributeError($"cannot reassign member '{name}'");
        if (name.StartsWith('_') || name is "name" or "value")
            throw new PyNotImplementedError("mutating Enum hooks or descriptor metadata is not supported");
    }
    /// <summary>成员只读属性使用同一赋值/删除守卫。</summary>
    public static void GuardMemberMutation(PyInstance member, string name)
    {
        if (!IsMember(member)) return;
        if (name is "name" or "value") throw new PyAttributeError($"cannot set attribute '{name}'");
        if (name is "_name_" or "_value_" or "__objclass__" or "__class__" or "_sort_order_")
            throw new PyNotImplementedError("mutating Enum internal member metadata is not supported");
    }
    /// <summary>类的显示形式不使用普通class表示。</summary>
    public static string ClassRepr(PyClass cls) => $"<{(Classes.GetValue(cls, _ => throw new PyTypeError("not an Enum class")).IsFlag ? "flag" : "enum")} '{cls.Name}'>";
    /// <summary>成员str与repr使用规范名字，别名自然共享。</summary>
    public static string MemberRepr(PyInstance member, bool repr)
    {
        MemberInfo info = Members.GetValue(member, _ => throw new PyTypeError("not an Enum member"));
        if (info.Name is null)
            return repr ? $"<{member.Class.Name}: {PyRepr.Repr(info.Value)}>" : $"{member.Class.Name}({PyRepr.Repr(info.Value)})";
        string name = member.Class.Name + "." + info.Name;
        return repr ? $"<{name}: {PyRepr.Repr(info.Value)}>" : name;
    }
    /// <summary>与普通Enum一致，成员哈希来自规范名字而非原值。</summary>
    public static long MemberHash(PyInstance member) => PyHash.Of(Members.GetValue(member, _ => throw new PyTypeError("not an Enum member")).Name);
}
