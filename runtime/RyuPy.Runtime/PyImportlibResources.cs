// importlib.resources 只提供已登记文件包的 files；不冒充完整 importlib、loader 或 zip 资源协议。
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>真实文件包资源模块的有限公开入口。</summary>
public static class PyImportlibResources
{
    private static readonly PyFunction FilesFunction = new("files", PySignature.Create("files", [], [], []),
        (_, args, names, values) => Files(args, names, values), [], false);

    /// <summary>父模块由通用importlib入口提供普通动态导入。</summary>
    public static Dictionary<string, object?> ParentAttributes() => PyImportlib.CreateAttributes();

    /// <summary>子模块复用稳定的 files 函数对象。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new() { ["files"] = FilesFunction };

    /// <summary>接受显式包名或真实模块对象；默认调用者和旧关键字警告明确不支持。</summary>
    public static object Files(object?[] args, string[] names, object?[] values)
    {
        bool hasAnchor = args.Length > 0, hasPackage = args.Length > 1;
        object? anchor = hasAnchor ? args[0] : null;
        for (int index = 0; index < names.Length; index++)
        {
            string name = names[index];
            if (name == "anchor")
            {
                if (hasAnchor) throw Error(new PyTypeError("files() got multiple values for argument 'anchor'"));
                hasAnchor = true; anchor = values[index];
            }
            else if (name == "package")
            {
                if (hasPackage) throw Error(new PyTypeError("files() got multiple values for argument 'package'"));
                hasPackage = true;
            }
            else throw Error(new PyTypeError($"files() got an unexpected keyword argument '{name}'"));
        }
        if (args.Length > 2) throw Error(new PyTypeError($"files() takes from 0 to 2 positional arguments but {args.Length} were given"));
        if (hasAnchor && hasPackage)
            throw Error(new PyTypeError("files() takes from 0 to 1 positional arguments but 2 were given"));
        if (hasPackage) throw Error(new PyNotImplementedError("files(package=...): deprecated keyword warnings are not implemented"));
        if (!hasAnchor || anchor is null or PyNone)
            throw Error(new PyNotImplementedError("files(): caller inference is not implemented"));
        PyModule module = anchor switch
        {
            string name => PyModules.Import(name),
            PyModule actual => actual,
            _ => throw Error(new PyNotImplementedError("files(): only explicit registered module anchors are supported")),
        };
        return PyModules.ResourceFiles(module);
    }

    private static PyBaseException Error(PyBaseException error) => (PyBaseException)PyErrors.PrepareRaise(error);
}
