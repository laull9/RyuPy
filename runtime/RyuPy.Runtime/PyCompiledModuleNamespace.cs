// 编译入口的可选真实命名空间工厂；Runtime 只持中性协议，CP 指针及引用计数留在 provider。
namespace RyuPy.Runtime;

/// <summary>真实模块及其权威字典；二者必须属于同一解释器与同一个模块实例。</summary>
public sealed record PyCompiledModuleNamespace(IPyModuleNamespace Globals, IPyForeignObject Module);

/// <summary>仅编译器已准入的入口使用；创建存储不执行任何 Python 业务体。</summary>
public interface IPyCompiledModuleNamespaceProvider
{
    /// <summary>取得真实入口模块与原字典，不能复制已有 globals。</summary>
    PyCompiledModuleNamespace CreateEntryNamespace(string name);
    /// <summary>登记同一模块的双向弱身份；不得创建持有Runtime模块的CP代理。</summary>
    void PublishEntryNamespace(PyModule module, PyCompiledModuleNamespace storage);
}
