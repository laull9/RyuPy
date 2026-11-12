// importlib.util.find_spec 的有限真实查询入口：先绑定与相对解析，再缓存，最后进入已编译finder。
// module_from_spec独立冷路仅支持原生规格与真实已编译loader；不执行目标正文。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>公开有限find_spec/module_from_spec；所有别名和容器调用共享同一函数对象。</summary>
public static class PyImportlibUtil
{
    private static readonly PyFunction FindSpecFunction = CreateFunction();

    private static PyFunction CreateFunction()
    {
        var function = new PyFunction("find_spec",
            PySignature.Create("find_spec", ["name", "package"], ["pos", "pos"], ["0", "1"]),
            (_, args, names, values) => Invoke(args, names, values), [null, PyNone.Instance], false);
        function.CaptureModule("importlib.util");
        return function;
    }

    /// <summary>新属性字典保持原函数身份；模块属性重绑定仍走普通模块协议。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new() { ["find_spec"] = FindSpecFunction, ["module_from_spec"] = PyImportlibModuleFromSpec.Function };

    // 通用绑定器目前不报告带默认值的参数范围；本API窄绑定还保留CP关键字错误优先级。
    private static object? Invoke(object?[] args, string[] names, object?[] values)
    {
        bool hasName = args.Length > 0, hasPackage = args.Length > 1;
        object? name = hasName ? args[0] : null, package = hasPackage ? args[1] : PyNone.Instance;
        for (int index = 0; index < names.Length; index++)
        {
            string key = names[index];
            switch (key)
            {
                case "name":
                    if (hasName) throw Error(new PyTypeError("find_spec() got multiple values for argument 'name'"));
                    hasName = true; name = values[index]; break;
                case "package":
                    if (hasPackage) throw Error(new PyTypeError("find_spec() got multiple values for argument 'package'"));
                    hasPackage = true; package = values[index]; break;
                default: throw Error(new PyTypeError($"find_spec() got an unexpected keyword argument '{key}'"));
            }
        }
        if (args.Length > 2)
            throw Error(new PyTypeError($"find_spec() takes from 1 to 2 positional arguments but {args.Length} were given"));
        if (!hasName) throw Error(new PyTypeError("find_spec() missing 1 required positional argument: 'name'"));
        return FindSpec(name, package);
    }

    private static object? FindSpec(object? name, object? package)
    {
        string original = ExactString(name, "startswith");
        string fullname = original.StartsWith(".", StringComparison.Ordinal) ? Resolve(original, package) : original;
        if (PyModules.TryCached(fullname, out PyModule? module))
        {
            // 受限sys.modules别名不允许Python写None；仍保留None缓存读取的自然语义。
            if (module is null) return PyNone.Instance;
            object? spec;
            try { spec = module.GetAttr("__spec__"); }
            catch (PyAttributeError missing)
            {
                PyErrors.Prepare(missing);
                var error = new PyValueError($"{original}.__spec__ is not set") { Context = missing };
                throw Error(error.WithCause(null));
            }
            if (spec is null or PyNone) throw Error(new PyValueError($"{original}.__spec__ is None"));
            return spec;
        }
        return PyModules.DiscoverSpec(fullname);
    }

    private static string Resolve(string name, object? package)
    {
        if (!PackageTruthy(package))
            throw Error(new PyImportError($"no package specified for {PyRepr.Repr(name)} (required for relative module names)"));
        string parent = ExactString(package, "rsplit");
        int level = 0;
        while (level < name.Length && name[level] == '.') level++;
        int end = parent.Length;
        for (int index = 1; index < level; index++)
        {
            int dot = end > 0 ? parent.LastIndexOf('.', end - 1) : -1;
            if (dot < 0) throw Error(new PyImportError("attempted relative import beyond top-level package"));
            end = dot;
        }
        string root = parent[..end], tail = name[level..];
        return tail.Length == 0 ? root : root + "." + tail;
    }

    // 已知内建真值没有用户回调；自定义真值协议在首个调用前明确拒绝。
    private static bool PackageTruthy(object? value)
    {
        if (value is PyInstance instance)
        {
            if (instance is PyStringInstance or PyListInstance or PyDictInstance
                || instance.Class.TryLookup("__bool__", out _, out _)
                || instance.Class.TryLookup("__len__", out _, out _))
                throw Unsupported("relative package truth protocols");
            return true;
        }
        if (value is PyBaseException { PythonClass: { } exceptionClass }
            && (exceptionClass.TryLookup("__bool__", out _, out _) || exceptionClass.TryLookup("__len__", out _, out _)))
            throw Unsupported("relative package truth protocols");
        if (value is PyNativeObject and not PyBaseObject || value is PyNotImplemented)
            throw Unsupported("relative package truth protocols");
        return PyOps.Truthy(value);
    }

    private static string ExactString(object? value, string attribute)
    {
        if (value is string text) return text;
        // bytes/bytearray拥有方法，但固定的str分隔符本身非法；这不是延期的合法协议。
        if (value is PyBytes or PyByteArray)
            throw Error(new PyTypeError(attribute == "startswith"
                ? "startswith first arg must be bytes or a tuple of bytes, not str"
                : "a bytes-like object is required, not 'str'"));
        if (value is PyInstance instance)
        {
            // 不读取property或__getattr__，否则拒绝前已经产生用户副作用。
            if (instance is PyStringInstance
                || instance.Class.TryLookup(attribute, out _, out _)
                || instance.Class.TryLookup("__getattr__", out _, out _)
                || instance.Class.TryLookup("__getattribute__", out _, out _)
                || (instance.TryOwnAttribute(attribute, out _)))
                throw Unsupported("custom name/package protocols");
            throw Error(new PyAttributeError($"'{PyRepr.TypeName(value)}' object has no attribute '{attribute}'"));
        }
        // 模块getter和用户异常/类描述符也能执行用户代码；先检查原字段，不为拒绝而绑定。
        if (value is PyModule
            || value is PyClass cls && cls.TryLookup(attribute, out _, out _)
            || value is PyBaseException { PythonClass: { } exceptionClass } exception
                && (exceptionClass.TryLookup(attribute, out _, out _)
                    || exceptionClass.TryLookup("__getattr__", out _, out _)
                    || exceptionClass.TryLookup("__getattribute__", out _, out _)
                    || exception.UserAttributes.ContainsKey(attribute)))
            throw Unsupported("custom name/package protocols");
        // super可能绑定用户描述符，原生对象可自行实现属性协议，均不提前执行未知逻辑。
        if (value is PySuper || value is PyNativeObject and not PyBaseObject)
            throw Unsupported("custom name/package protocols");
        try { _ = PyObject.GetAttr(value, attribute); }
        catch (PyAttributeError missing) { throw Error(missing); }
        throw Unsupported("custom name/package protocols");
    }

    private static PyBaseException Unsupported(string protocol) =>
        Error(new PyNotImplementedError($"find_spec(): {protocol} are not implemented"));

    private static PyBaseException Error(PyBaseException error) => (PyBaseException)PyErrors.PrepareRaise(error);
}
