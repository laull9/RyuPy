// 编译程序集与 stock provider 之间的中性冷 ABI；不包含 CP 指针、GIL 或 owned 引用。
// 静态业务体只借用本次调用的 environment，禁止把该租约或模块 context 缓存进 core。
using System;

namespace RyuPy.Runtime;

/// <summary>真实 F 完成参数绑定后的单槽静态 CIL 业务入口。</summary>
public delegate object? PyStockThinBody(IPyStockThinEnvironment environment, object? argument);

/// <summary>调用期间有效的名字读取能力；环境生命期由 provider 独立控制。</summary>
public interface IPyStockThinEnvironment
{
    /// <summary>按真实 F 的 globals 和定义时捕获的 builtins 查名，保留值与异常身份。</summary>
    object? LoadGlobal(string name);
}

/// <summary>业务定义来源；纯字符串/行号，无模块实例、owner 或执行环境强回边。</summary>
public sealed class PyStockThinDefinition(string entryId, string module, string name, string sourcePath, int sourceLine)
{
    /// <summary>编译器冻结的稳定来源标识。</summary>
    public string EntryId { get; } = entryId;
    /// <summary>业务定义的原模块。</summary>
    public string Module { get; } = module;
    /// <summary>公开函数初始名称。</summary>
    public string Name { get; } = name;
    /// <summary>业务源码位置，独立于固定胶水 code。</summary>
    public string SourcePath { get; } = sourcePath;
    /// <summary>业务定义行号。</summary>
    public int SourceLine { get; } = sourceLine;
}

/// <summary>显式 stock 薄函数的可选 provider 能力，不修改既有模块 provider ABI。</summary>
public interface IPyStockThinFunctionProvider
{
    /// <summary>以权威 globals 创建真实函数；body 必须为单一无捕获静态目标。</summary>
    object CreateStockThinFunction(object globals, PyStockThinDefinition definition, PyStockThinBody body);
}

/// <summary>供生成程序集调用的冷入口，普通 Runtime 函数不经过本类型。</summary>
public static class PyStockThinFunctions
{
    /// <summary>只调用显式安装的候选能力；缺失时报能力异常，不创建旧函数替身。</summary>
    public static object Create(object globals, PyStockThinDefinition definition, PyStockThinBody body)
    {
        ArgumentNullException.ThrowIfNull(globals);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(body);
        return PyProvider.Current is IPyStockThinFunctionProvider provider
            ? provider.CreateStockThinFunction(globals, definition, body)
            : throw new PyNotImplementedError("stock thin function provider is not installed");
    }

    /// <summary>动态隐藏参数只允许当前 provider 创建的活动名字读取环境。</summary>
    public static object? LoadGlobal(object environment, string name) =>
        environment is IPyStockThinEnvironment active ? active.LoadGlobal(name)
            : throw new PyRuntimeError("stock thin function environment is invalid");
}

public static partial class PyModuleInstances
{
    /// <summary>定义点只借用当前权威字典；返回的 core 不持有模块 context。</summary>
    public static object CreateStockThinFunction(object moduleContext, PyStockThinDefinition definition,
        PyStockThinBody body) => PyStockThinFunctions.Create(Context(moduleContext).State.View, definition, body);
}
