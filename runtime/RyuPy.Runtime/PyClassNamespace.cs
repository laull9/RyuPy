namespace RyuPy.Runtime;

/// <summary>类体冷路径的原始命名空间查询；不执行继承、描述符或用户属性协议。</summary>
public static partial class PyOps
{
    /// <summary>判断当前类体是否已经绑定名字；包括值为None的真实绑定。</summary>
    /// <param name="cls">正在执行类体的原始类对象。</param>
    /// <param name="name">待查的类体绑定名。</param>
    public static bool ClassNamespaceContains(object cls, string name)
    {
        if (cls is not PyClass target) throw new PyTypeError("class namespace target must be a class");
        return target.Attributes.ContainsKey(name);
    }
}
