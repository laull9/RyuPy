// importlib.resources提供已登记文件包files与文件系统Path的as_file；其它资源载体明确拒绝。
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>真实文件包资源模块的有限公开入口。</summary>
public static class PyImportlibResources
{
    private static readonly PyFunction FilesFunction = new("files", PySignature.Create("files", [], [], []),
        (_, args, names, values) => Files(args, names, values), [], false);

    private static readonly PyFunction AsFileFunction = new("as_file", PySignature.Create("as_file", [], [], []),
        (_, args, names, values) => AsFile(args, names, values), [], false);

    private static readonly PyFunction OtherAsFileFunction = PyNativeFunctions.Create("as_file", ["path"], [null], 1, a =>
    {
        // 非Traversable的错误参数保留CPython缺少is_dir的错误；其它合法载体等待独立解包实现。
        PyObject.GetAttr(a[0], "is_dir");
        throw Error(new PyNotImplementedError("as_file(): zip and custom Traversable resources are not implemented; expected a filesystem Path"));
    });

    /// <summary>父模块由通用importlib入口提供普通动态导入。</summary>
    public static Dictionary<string, object?> ParentAttributes() => PyImportlib.CreateAttributes();

    /// <summary>子模块复用稳定的 files 与 as_file 函数对象。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new() { ["files"] = FilesFunction, ["as_file"] = AsFileFunction };

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

    /// <summary>按CPython singledispatch的首位置参数选择Path退化分支，不提前探测存在性。</summary>
    internal static object AsFile(object?[] args, string[] names, object?[] values)
    {
        if (args.Length == 0) throw Error(new PyTypeError("as_file requires at least 1 positional argument"));
        if (args[0] is PyPath) return PyResourcePathContext.Create(args, names, values);
        if (args.Length > 1) PyBinder.Bind(OtherAsFileFunction.Signature, args[..1], names, values, OtherAsFileFunction.Defaults);
        return OtherAsFileFunction.Invoke(args, names, values)!;
    }

    private static PyBaseException Error(PyBaseException error) => (PyBaseException)PyErrors.PrepareRaise(error);
}
