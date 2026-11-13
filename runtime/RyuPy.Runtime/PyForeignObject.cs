// 外部执行域的中性协议与模块接入点；这里不持有解释器地址、GIL或原生引用。
using System;
using System.Threading;

namespace RyuPy.Runtime;

/// <summary>外部执行域对象的共同协议；值读取属于显式转换，对象传递保持身份。</summary>
public interface IPyForeignObject : IPyNativeCallable
{
    /// <summary>真实类型对象。</summary>
    object ForeignType { get; }
    /// <summary>按目标标量类型显式读取；不支持的转换抛 Python 类型错误。</summary>
    object ReadScalar(string kind);
    /// <summary>下标读取。</summary>
    object? Item(object? key);
    /// <summary>下标写入或删除。</summary>
    void WriteItem(object? key, object? value, bool deleting);
    /// <summary>迭代器。</summary>
    IPyIterator Iterate();
    /// <summary>长度。</summary>
    long Length();
    /// <summary>真值。</summary>
    bool Truth();
    /// <summary>哈希。</summary>
    long Hash();
    /// <summary>二元运算，reverse指明自己位于右侧。</summary>
    object Binary(int operation, object? other, bool reverse);
    /// <summary>比较，reverse指明自己位于右侧。</summary>
    bool Compare(int operation, object? other, bool reverse);
    /// <summary>保留比较表达式的原始Python结果，条件分支另外求真。</summary>
    object RichCompare(int operation, object? other, bool reverse);
    /// <summary>一元运算。</summary>
    object Unary(int operation);
    /// <summary>按外部执行域真实类型检查实例。</summary>
    bool IsInstanceOf(object type);
    /// <summary>当前对象作为类型时匹配任意输入。</summary>
    bool Accepts(object? value);
    /// <summary>是否为真实异常类。</summary>
    bool IsExceptionClass { get; }
    /// <summary>按真实异常身份与继承匹配。</summary>
    bool MatchesException(PyBaseException error);
    /// <summary>原异常解释栈元数据；数组按最外帧至最内帧排列，不运行用户属性钩子。</summary>
    PyStackFrame[] TracebackFrames();
    /// <summary>异常传播/字段修改的显式权威边界；write为真写回编译域链，否则刷新真实外部状态。</summary>
    void SynchronizeException(PyBaseException error, bool write);
}

/// <summary>模块provider：选择由入口冻结，Runtime只执行中性导入合同。</summary>
public interface IPyModuleProvider
{
    /// <summary>完整模块名是否属于本provider。</summary>
    bool Owns(string name);
    /// <summary>共享解释器sys的标准流；显式None以PyNone返回。</summary>
    object? SystemAttribute(string name);
    /// <summary>返回同一权威模块的Runtime视图。</summary>
    PyModule Import(string name);
    /// <summary>编译模块登记通知，供反向延迟导入使用。</summary>
    void Registered(string name);
    /// <summary>编译模块初始化前发布同一对象，供反向循环导入观察。</summary>
    void Initializing(PyModule module);
    /// <summary>初始化失败时撤销provider缓存，允许重新导入。</summary>
    void Failed(string name);
}

/// <summary>入口组合点；原生模式没有provider，也不载入外部组件。</summary>
public static class PyProvider
{
    /// <summary>本进程冻结的provider。</summary>
    public static IPyModuleProvider? Current => current;
    private static IPyModuleProvider? current;
    /// <summary>入口仅可安装一次，禁止运行中替换模块归属。</summary>
    public static void Install(IPyModuleProvider provider)
    {
        if (Interlocked.CompareExchange(ref current, provider, null) is not null)
            throw new InvalidOperationException("module provider is already installed");
    }
    /// <summary>关闭完成后只允许所属provider撤销登记。</summary>
    public static void Remove(IPyModuleProvider provider)
    {
        if (!ReferenceEquals(Interlocked.CompareExchange(ref current, null, provider), provider))
            throw new InvalidOperationException("module provider ownership mismatch");
    }
    /// <summary>构造全属性转发的模块视图，禁止建立第二份业务状态。</summary>
    public static PyModule Module(string name, PyModuleGet get, PyModuleSet set, IPyForeignObject? foreign = null) =>
        new(name, name.Contains('.') ? name[..name.LastIndexOf('.')] : "", "", () => { }, get, set,
            instanceGlobals: true) { State = 2, ForeignObject = foreign };
}
