namespace RyuPy.Runtime;

/// <summary>原生可调用对象的有限入口；不把partial等对象伪装为自动绑定self的Python函数。</summary>
public interface IPyNativeCallable
{
    /// <summary>接收已展开位置/关键字参数，具体对象负责绑定和公开语义。</summary>
    object? Invoke(object?[] args, string[] names, object?[] values);
}

/// <summary>原生非数据描述符：只用于明确实现的包装器，不开放任意Python descriptor协议。</summary>
public interface IPyNativeDescriptor
{
    /// <summary>读取类或实例属性时绑定；类访问self为null，实例访问传实际接收者。</summary>
    object? Bind(PyClass owner, object? self);
}

/// <summary>原生dict派生包装的存储契约；转换复制条目而非把键迭代错误解释为键值对。</summary>
public interface IPyNativeMapping
{
    /// <summary>保序字典存储；缺键工厂及计数默认值仍由包装对象自己的下标协议处理。</summary>
    PyDict Dictionary { get; }
}
