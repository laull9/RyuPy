// 完整源码模块的可选真实命名空间工厂；与旧入口首切分开，执行域由编译注册明确声明。
namespace RyuPy.Runtime;

/// <summary>为已编译源码创建真实模块与唯一globals；创建阶段不执行源码或发布导入缓存。</summary>
public interface IPyCompiledSourceNamespaceProvider
{
    /// <summary>普通模块创建独立真实存储，入口沿其规范原字典；失败由模块导入事务处理。</summary>
    PyCompiledModuleNamespace CreateModuleNamespace(string name);
    /// <summary>发布同一模块的双向弱身份；真实字典的所有修改立即进入同一后端。</summary>
    void PublishModuleNamespace(PyModule module, PyCompiledModuleNamespace storage);
}
