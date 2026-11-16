// 跨域类继承的中性协议与组合点：Runtime 不接触 PyObject*、GIL 或具体宿主实现，
// 仅通过本接口将外部基类校验、真实派生类创建、实例构造与 super 委托交给已安装的 provider。
using System;
using System.Collections.Generic;
using System.Threading;

namespace RyuPy.Runtime;

/// <summary>外部类型继承桥接合同：供 CPython 等宿主为编译类提供真实类型与实例协议。</summary>
public interface IPyForeignClassBridge
{
    /// <summary>判断外部对象是否为真实类型对象（<c>isinstance(value, type)</c>）。</summary>
    bool IsType(IPyForeignObject value);

    /// <summary>判断外部类型对象是否为内建根元类 <c>type</c>。</summary>
    bool IsPlainType(object? typeObject);

    /// <summary>在外部执行域按真实 MRO 检查 <c>issubclass(child, parent)</c>。</summary>
    bool IsSubclass(object? child, object? parent);

    /// <summary>外部实例匹配真实编译类或已有 Mixin 镜像；未知本地类不导出新类型。</summary>
    bool IsInstance(IPyForeignObject value, PyClass target) =>
        PyForeignClassBridge.GetForeignClass(target)?.Accepts(value) ?? false;

    /// <summary>在外部执行域创建真实的派生类型对象；失败时抛 Python 语义异常。</summary>
    IPyForeignObject CreateDerivedClass(
        PyClass compiledClass,
        string name,
        object?[] bases,
        IReadOnlyDictionary<string, object?> attributes
    );

    /// <summary>类体前选择元类并建立外部命名空间；旧 provider 可保留原终结合同。</summary>
    IPyForeignClassDefinition? PrepareDerivedClass(PyClass owner, object?[] bases) => null;

    /// <summary>同步写入或替换已导出外部类的单个属性。</summary>
    void SetClassAttribute(IPyForeignObject foreignClass, PyClass ownerClass, string name, object? value);

    /// <summary>同步删除已导出外部类的单个属性。</summary>
    void DeleteClassAttribute(IPyForeignObject foreignClass, string name);

    /// <summary>同步已经导出的普通 Mixin 镜像；没有镜像的类保持本地归属。</summary>
    void SynchronizeMirrorAttribute(PyClass ownerClass, string name, object? value, bool deleting) { }

    /// <summary>等价于 <c>object.__new__(cls, *args, **kwargs)</c>。</summary>
    object? AllocateFromBaseObject(IPyForeignObject foreignClass, object?[] extraArgs, string[] kwNames, object?[] kwValues);

    /// <summary>等价于 <c>object.__init__(self, *args, **kwargs)</c>。</summary>
    void InitializeFromBaseObject(IPyForeignObject foreignInstance, object?[] extraArgs, string[] kwNames, object?[] kwValues);

    /// <summary>绕过用户钩子执行 <c>object.__setattr__(self, name, value)</c>。</summary>
    void SetAttrFromBaseObject(IPyForeignObject foreignInstance, string name, object? value);

    /// <summary>绕过用户钩子执行 <c>object.__delattr__(self, name)</c>。</summary>
    void DelAttrFromBaseObject(IPyForeignObject foreignInstance, string name);

    /// <summary>使用外部真实 <c>super(start, receiver)</c> 读取属性或绑定方法。</summary>
    object? GetSuperAttribute(object start, object? receiver, string name);

    /// <summary>构造真实外部 super，并返回具有统一内建类型身份的中性代理。</summary>
    object? CreateSuper(object start, object? receiver);

    /// <summary>查询成员资格（<c>value in foreignInstance</c>）。</summary>
    bool Contains(IPyForeignObject foreignInstance, object? value);

    /// <summary>读取外部对象或外部派生类/实例的 <c>dir()</c> 名称列表。</summary>
    object? Dir(IPyForeignObject foreignObject);
}

/// <summary>跨域类桥接组合点；原生模式保持未安装，由显式入口或单元测试注册。</summary>
public static class PyForeignClassBridge
{
    private static IPyForeignClassBridge? _current;

    /// <summary>当前激活的跨域类桥接实现；未安装时为 <c>null</c>。</summary>
    public static IPyForeignClassBridge? Current => _current;

    /// <summary>显式安装跨域类桥接实现；同一时刻仅允许存在一个所有者。</summary>
    public static void Install(IPyForeignClassBridge bridge)
    {
        ArgumentNullException.ThrowIfNull(bridge);
        if (Interlocked.CompareExchange(ref _current, bridge, null) is not null)
        {
            throw new InvalidOperationException("foreign class bridge is already installed");
        }
    }

    /// <summary>卸载跨域类桥接实现；必须由原安装者调用。</summary>
    public static void Remove(IPyForeignClassBridge bridge)
    {
        ArgumentNullException.ThrowIfNull(bridge);
        if (!ReferenceEquals(Interlocked.CompareExchange(ref _current, null, bridge), bridge))
        {
            throw new InvalidOperationException("foreign class bridge ownership mismatch");
        }
    }

    /// <summary>创建关联外部真实实例的跨域派生类 <see cref="PyInstance"/>。</summary>
    public static PyInstance CreateForeignInstance(PyClass cls, IPyForeignObject foreignInstance) =>
        new(cls, foreignInstance);

    /// <summary>读取跨域派生实例背后的外部真实实例；普通原生实例返回 <c>null</c>。</summary>
    public static IPyForeignObject? GetForeignInstance(PyInstance instance) => instance.ForeignInstance;

    /// <summary>读取跨域派生类对应的外部真实类型对象；普通原生类返回 <c>null</c>。</summary>
    public static IPyForeignObject? GetForeignClass(PyClass cls) => cls.ForeignClass;

    /// <summary>构造回调的临时类型身份；外部 owner 只在成功终结后正式发布。</summary>
    public static void SetConstructingClass(PyClass owner, IPyForeignObject? value) => owner.ForeignClass = value;

    /// <summary>provider 完成或撤回构造时撤销 Runtime 的冷事务登记。</summary>
    public static void ForgetClassDefinition(PyClass owner) => PyForeignClassConstruction.Forget(owner);

    /// <summary>读取或首次建立 super 的真实外部代理；普通本地布局返回 null。</summary>
    public static IPyForeignObject? GetForeignSuper(PySuper value) => value.ExportForeign();

    /// <summary>读取类声明时的直接基类序列，供桥接构造混合多继承镜像。</summary>
    public static object?[] GetDirectBases(PyClass cls) => cls.DirectBasesTuple().Items;

    /// <summary>读取编译类命名空间字典，供桥接生成外部类/mixin 镜像。</summary>
    public static IReadOnlyDictionary<string, object?> GetClassAttributes(PyClass cls) => cls.Attributes;

    internal static void TryInstall(IPyForeignClassBridge bridge) =>
        Interlocked.CompareExchange(ref _current, bridge, null);

    internal static void TryRemove(IPyForeignClassBridge bridge) =>
        Interlocked.CompareExchange(ref _current, null, bridge);

    // 保留历史object返回ABI；在Runtime冷边界把旧provider返回值规范为同一个super类型。
    internal static PySuper CanonicalSuper(object start, object? receiver) => Require().CreateSuper(start, receiver) switch
    {
        PySuper value => value,
        IPyForeignObject foreign => PySuper.FromForeign(foreign, name => PyObject.GetAttr(foreign, name)),
        _ => throw new PyRuntimeError("foreign class bridge did not return a super proxy"),
    };

    internal static IPyForeignClassBridge Require() =>
        _current ?? throw (PyBaseException)PyErrors.PrepareRaise(
            new PyNotImplementedError("foreign class inheritance requires an active CPython/foreign class bridge")
        );

    internal static bool IsType(IPyForeignObject value) =>
        _current is { } bridge ? bridge.IsType(value) : value.IsInstanceOf(PyType.Get("type"));

    internal static bool IsPlainType(object? typeObject) =>
        _current is { } bridge && bridge.IsPlainType(typeObject);

    internal static bool IsSubclass(object? child, object? parent) =>
        Require().IsSubclass(child, parent);

    internal static bool IsInstance(IPyForeignObject value, PyClass target) =>
        _current is { } bridge ? bridge.IsInstance(value, target) : target.ForeignClass?.Accepts(value) ?? false;

    internal static bool Contains(IPyForeignObject foreignInstance, object? value) =>
        Require().Contains(foreignInstance, value);

    internal static object? Dir(IPyForeignObject foreignObject) =>
        Require().Dir(foreignObject);
}
