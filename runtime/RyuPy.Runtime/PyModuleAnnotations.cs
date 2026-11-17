// 编译模块的SETUP_ANNOTATIONS冷操作：先检查真实D，只在缺键时创建目标域精确dict。
namespace RyuPy.Runtime;

/// <summary>namespace后端创建自身域的精确字典，避免用managed proxy替代Python内建dict。</summary>
public interface IPyModuleDictionaryNamespace
{
    /// <summary>创建一个空的目标域精确dict，不执行用户协议。</summary>
    object CreateDictionary();
}

public static partial class PyModuleInstances
{
    /// <summary>保留已有任意__annotations__对象；后续写入继续走它的真实mapping协议。</summary>
    public static void SetupAnnotations(object state)
    {
        var context = Context(state);
        if (context.State.TryGet("__annotations__", out _)) return;
        object dictionary = context.State.ForeignGlobals is null ? new PyDict()
            : context.State.ForeignGlobals is IPyModuleDictionaryNamespace factory ? factory.CreateDictionary()
            : throw new PyNotImplementedError("module namespace cannot create an exact annotation dictionary");
        context.State.Set("__annotations__", dictionary);
    }
}
