// 公开默认模块创建冷路；仅原生未改规格/自有loader，不执行正文、不写sys.modules、不补builtins。
using System;
using System.Numerics;

namespace RyuPy.Runtime;

internal static class PyImportlibModuleFromSpec
{
    internal static readonly PyFunction Function = Build();

    private static PyFunction Build()
    {
        var function = new PyFunction("module_from_spec",
            PySignature.Create("module_from_spec", ["spec"], ["pos"], ["0"]),
            (_, args, names, values) => Invoke(args, names, values), [null], false);
        function.CaptureModule("importlib.util");
        return function;
    }

    // 定向红测确认通用binder先拒绝超量位置参；本API按CP先处理关键字，不改变其它函数热路。
    private static object Invoke(object?[] args, string[] names, object?[] values)
    {
        bool filled = args.Length > 0;
        object? spec = filled ? args[0] : null;
        for (int index = 0; index < names.Length; index++)
        {
            if (names[index] != "spec") throw BindingError($"module_from_spec() got an unexpected keyword argument '{names[index]}'");
            if (filled) throw BindingError("module_from_spec() got multiple values for argument 'spec'");
            filled = true; spec = values[index];
        }
        if (args.Length > 1) throw BindingError($"module_from_spec() takes 1 positional argument but {args.Length} were given");
        if (!filled) throw BindingError("module_from_spec() missing 1 required positional argument: 'spec'");
        return Create(spec);
    }

    private static PyTypeError BindingError(string message) =>
        (PyTypeError)PyErrors.PrepareRaise(new PyTypeError(message));

    private static PyModule Create(object? value)
    {
        if (value is not PyInstance spec || !PyModuleSpec.IsOriginal(spec))
            throw Unsupported("custom spec protocols");
        // 读取实际属性前先认证所有必需槽；缺槽/自定义字段协议保完整pending，不伪造CP错误顺序。
        foreach (string field in new[] { "name", "loader", "origin", "submodule_search_locations", "_set_fileattr", "_cached" })
            if (!spec.Class.TryGetInstanceSlot(field, out int slot) || !spec.TryGetSlot(slot, out _))
                throw Unsupported("incomplete specs");
        object? loader = PyModuleSpec.Get(spec, "loader");
        PyCompiledModuleCode? sourceCode = null;
        if (loader is PyInstance compiled)
        {
            PyCompiledModuleLoader.RequireOriginalProtocol(compiled);
            // 真正普通绑定/调用；不使用与公开方法脱离的专用快捷实现。
            _ = PyOps.CallMethod(compiled, "create_module", [spec], [], []);
            sourceCode = PyCompiledModuleLoader.RequireInstanceCode(compiled);
        }
        else if (loader is not (null or PyNone)) throw Unsupported("custom loaders");

        object? rawName = PyModuleSpec.Get(spec, "name");
        if (rawName is not string name)
        {
            if (rawName is null or PyNone or bool or int or long or BigInteger or double or PyList or PyDict or PyTuple)
                throw BindingError($"module() argument 'name' must be str, not {PyRepr.TypeName(rawName)}");
            throw Unsupported("custom spec name protocols");
        }
        object? path = PyModuleSpec.Get(spec, "submodule_search_locations");
        if (loader is null or PyNone && path is not (null or PyNone)) throw Unsupported("namespace packages");
        if (path is not (null or PyNone or PyList)) throw Unsupported("custom package paths");
        object? hasLocation = PyModuleSpec.Get(spec, "_set_fileattr");
        if (hasLocation is not bool) throw Unsupported("custom location truth protocols");
        object? origin = PyModuleSpec.Get(spec, "origin");
        object? cached = PyModuleSpec.Get(spec, "_cached");
        if (origin is not (null or PyNone or string) || cached is not (null or PyNone or string))
            throw Unsupported("custom origin/cache protocols");

        var module = PyModuleInstances.CreateEmpty(name);
        if (sourceCode is not null) PyModules.BindDefaultInstance(module, sourceCode);
        module.SetAttr("__loader__", loader);
        module.SetAttr("__package__", PyModuleSpec.Get(spec, "parent"));
        module.SetAttr("__spec__", spec);
        if (path is not (null or PyNone)) module.SetAttr("__path__", path);
        if (hasLocation is true)
        {
            module.SetAttr("__file__", origin);
            if (PyModuleSpec.Get(spec, "cached") is not (null or PyNone))
                module.SetAttr("__cached__", PyModuleSpec.Get(spec, "cached"));
        }
        return module;
    }

    private static PyNotImplementedError Unsupported(string detail) =>
        (PyNotImplementedError)PyErrors.PrepareRaise(new PyNotImplementedError("module_from_spec(): " + detail + " are not implemented"));
}
