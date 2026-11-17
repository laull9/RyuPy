// 原生边界冷读已证明的CIL调用来源；只返回实际instance环境，不从文件名/公开模块名重建globals。
namespace RyuPy.Runtime;

public static partial class PyTraceback
{
    /// <summary>0=无可信CIL现场，1=权威实例环境，2=Legacy静态槽，3=旧未知来源。不会执行用户协议。</summary>
    public static int CompiledNativeCallSite(out object? globals, out string? file)
    {
        PyCallerSource source = CallerSource(1);
        globals = null; file = null;
        if (source.Code is null) return _depth == 0 ? 0 : 3;
        file = source.Frame.File;
        if (source.Globals is null) return 2;
        globals = PyModuleNamespaceAccess.View(source.Globals);
        return 1;
    }
}
