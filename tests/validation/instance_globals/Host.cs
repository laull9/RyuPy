// A1静态宿主草稿：真实公开loader/context/dict，不反射。调用Run前由真实生成入口完成注册。
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using RyuPy.Runtime;

public static class InstanceGlobalsHost
{
    static object? Attr(object? value, string name) => PyObject.GetAttr(value, name);
    static void Check(bool ok, string label)
    { if (!ok) throw new InvalidOperationException(label); }
    static object? Call(object? fn, params object?[] args)
    {
        long depth = PyTraceback.CaptureDepth();
        object? saved = PyErrors.EnterHandler(null);
        try { return PyOps.CallValue(fn, args, [], []); }
        catch (PyBaseException error) { PyTraceback.Attach(error); throw; }
        finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(saved); }
    }
    static void Write(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null: writer.WriteNullValue(); break;
            case bool x: writer.WriteBooleanValue(x); break;
            case string x: writer.WriteStringValue(x); break;
            case long x: writer.WriteNumberValue(x); break;
            case int x: writer.WriteNumberValue(x); break;
            case object?[] values:
                writer.WriteStartArray();
                foreach (var x in values) Write(writer, x);
                writer.WriteEndArray(); break;
            default: throw new InvalidOperationException("禁止不受审JSON类型 " + value.GetType().FullName);
        }
    }
    static void Emit(string label, object? value)
    {
        using var bytes = new MemoryStream();
        using (var writer = new Utf8JsonWriter(bytes)) Write(writer, new object?[] { label, value });
        Console.WriteLine(Encoding.UTF8.GetString(bytes.ToArray()));
    }
    static PyDict Copy(PyDict source)
    {
        var copy = new PyDict();
        foreach (var (key, value) in source.Entries()) copy.Set(key, value);
        return copy;
    }
    static object Native(string name, int arity, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, arity == 0 ? [] : ["value"], arity == 0 ? [] : [null], arity, body);
    static object? loader;
    static object? spec;
    static object? util;
    static PyDict defaults = null!;
    static string metadataPath = "";
    static int serial;
    // 完整原始字典逐槽记录；仅已知metadata形状，不调用repr/用户比较。
    static object?[] Metadata(PyDict globals)
    {
        return globals.Entries().Select(pair => new object?[] {
            pair.Key is string key ? key : throw new InvalidOperationException("非字符串初始键"),
            MetadataValue(pair.Key as string ?? "", pair.Value)
        }).ToArray();
    }
    static object? MetadataValue(string key, object? value)
    {
        if (value is null or PyNone) return new object?[] { "None", null };
        if (value is string text) return new object?[] { "str", text };
        if (key == "__builtins__" && value is PyDict dictionary)
            return new object?[] { "builtin-dict", dictionary.Keys.ToArray(), ReferenceEquals(dictionary, defaults) };
        if (key == "__spec__")
            return new object?[] { "ModuleSpec", Attr(value, "name"), Attr(value, "origin"),
                MetadataValue("__loader__", Attr(value, "loader")) };
        if (key == "__loader__")
            return new object?[] { "compiled-loader", Attr(value, "name"), Attr(value, "path") };
        if (key == "__path__" && value is PyList list)
            return new object?[] { "path-list", list.Items.ToArray() };
        throw new InvalidOperationException("未审初始metadata类型: " + key);
    }
    static object Make(PyDict environment)
    {
        var module = Call(Attr(util, "module_from_spec"), spec)!;
        var globals = (PyDict)Attr(module, "__dict__")!;
        // 原始metadata完整键序另流记录；业务dict受控种子是真实清空/写入，不净化输出。
        using (var file = File.Open(metadataPath + "." + serial++ + ".json", FileMode.CreateNew))
        using (var writer = new Utf8JsonWriter(file)) Write(writer, Metadata(globals));
        globals.Clear();
        globals.Set("__name__", "subject");
        foreach (string key in new[] { "__doc__", "__package__", "__loader__", "__spec__" })
            globals.Set(key, PyNone.Instance);
        globals.Set("__builtins__", environment);
        Call(Attr(loader, "exec_module"), module);
        return module;
    }
    static object? Invoke(object module, string name, params object?[] args) => Call(Attr(module, name), args);
    static void Expected<T>(Action body, string label) where T : PyBaseException
    {
        try { body(); }
        catch (T) { Emit(label, typeof(T).Name[2..]); return; }
        throw new InvalidOperationException(label + " 未抛预期类别");
    }
    public static void Run(string mode, string output)
    {
        metadataPath = output;
        util = PyModules.Import("importlib.util");
        spec = Call(Attr(util, "find_spec"), "subject");
        loader = Attr(spec, "loader");
        // Create保留真实注册来源；只取实际默认builtins对象，不构造canonical替代。
        var original = PyModuleInstances.Create("subject");
        defaults = (PyDict)((PyDict)Attr(original, "__dict__")!).Get("__builtins__")!;
        Check(defaults.TryGet("globals", out _), "默认真实globals标记缺失");
        if (mode == "functional") { Supplemental(); Functional(); }
        else if (mode == "default") DefaultTable();
        else if (mode == "indirect") Indirect();
        else throw new InvalidOperationException("未知模式");
    }
    static void Supplemental()
    {
        var bodySpec = Call(Attr(util, "find_spec"), "module_body");
        var bodyLoader = Attr(bodySpec, "loader");
        var left = Call(Attr(util, "module_from_spec"), bodySpec)!;
        var right = Call(Attr(util, "module_from_spec"), bodySpec)!;
        Call(Attr(bodyLoader, "exec_module"), left);
        Call(Attr(bodyLoader, "exec_module"), right);
        var lg = Attr(left, "__dict__"); var rg = Attr(right, "__dict__");
        Check(!ReferenceEquals(lg, rg), "module-body独立dict");
        foreach (var module in new[] { left, right })
        {
            Check(ReferenceEquals(Attr(module, "body_dict"), Attr(module, "__dict__")), "module-body直接身份");
            Check(ReferenceEquals(Invoke(module, "body_read"), Attr(module, "__dict__")), "module-body owned身份");
            Check(Equals(Attr(module, "body_seen"), 17L) && Equals(Attr(module, "body_written"), 17L), "module-body真实写读17");
        }
        var legacy = PyModules.Import("legacy_callback");
        Check(Equals(Invoke(legacy, "invoke", Native("legacy-real-callback", 0, _ => 23L)), 23L), "Legacy参数回调");
        Check(Equals(Invoke(legacy, "invoke_one", Native("legacy-one", 1, args =>
            Equals(args[0], 1L) ? 2L : throw new InvalidOperationException("Legacy实参不是1"))), 2L), "Legacy带参回调");
        // 无新输出标签；具名产品附加断言，不冒新CP或dateutil/comp通过。
    }
    static void Functional()
    {
        var env = Copy(defaults);
        var a = Make(env); var b = Make(Copy(env));
        var g = (PyDict)Attr(a, "__dict__")!;
        var ids = (PyTuple)Invoke(a, "identities")!;
        Check(ReferenceEquals(ids.Items[0], g) && ReferenceEquals(ids.Items[1], g)
            && ReferenceEquals(Attr(Attr(a, "identities"), "__globals__"), g), "三重身份");
        Check(!ReferenceEquals(g, Attr(b, "__dict__")), "两实例");
        Emit("identity", true); Emit("keys", g.Keys.ToArray());
        g.Set("value", 3L); Emit("set", Invoke(a, "read_value"));
        var update = new PyDict(); update.Set("value", 4L);
        Call(Attr(g, "update"), update); Emit("update", Invoke(a, "read_value"));
        g.Delete("value"); Expected<PyNameError>(() => Invoke(a, "read_value"), "delete");
        PyObject.SetAttr(b, "external", g);
        ((PyDict)Attr(b, "external")!).Set("value", 5L); Emit("external", Invoke(a, "read_value"));
        PyObject.SetAttr(b, "foreign", Attr(a, "identities"));
        Check(ReferenceEquals(((PyTuple)Invoke(b, "foreign")!).Items[0], g), "跨实例owner");
        Emit("foreign-owner", true);
        var replacement = Native("replacement", 0, _ => "replacement");
        g.Set("globals", replacement); Emit("rebound", Invoke(a, "direct_rebound"));
        g.Set("globals", 42L); Expected<PyTypeError>(() => Invoke(a, "direct_rebound"), "noncallable");
        g.Delete("globals"); Emit("shadow", Invoke(a, "shadow", replacement));
        // 产品附加合同，不冒充原CP新增调用：直接Name参数持canonical仍取当前owner。
        Check(ReferenceEquals(Invoke(a, "shadow", defaults.Get("globals")), g), "参数canonical直接owner");
        env.Set("len", Native("zero", 1, _ => 0L));
        var old = Attr(a, "captured"); var newer = Copy(env);
        newer.Set("len", Native("ninety_nine", 1, _ => 99L));
        g.Set("__builtins__", newer); Call(Attr(loader, "exec_module"), a);
        Check(ReferenceEquals(Attr(old, "__globals__"), g)
            && ReferenceEquals(Attr(Attr(a, "captured"), "__globals__"), g), "重执行owner");
        Emit("reexec", new object?[] { Call(old), Invoke(a, "captured"),
            ReferenceEquals(Attr(old, "__builtins__"), env),
            ReferenceEquals(Attr(Attr(a, "captured"), "__builtins__"), newer) });
        bool failed = false;
        try { Invoke(a, "fail_publish"); }
        catch (PyValueError error) { failed = true; Emit("failure-visible", new object?[] { error.Message, g.Get("before_failure") }); }
        Check(failed, "失败写缺少异常");
        var keep = Attr(a, "identities"); g.Clear();
        var cleared = (PyTuple)Call(keep)!;
        Check(ReferenceEquals(cleared.Items[0], g) && ReferenceEquals(cleared.Items[1], g) && g.Count == 0, "clear");
        Emit("clear", true);
        var custom = Copy(defaults); var c = Make(custom);
        Check(ReferenceEquals(((PyTuple)Invoke(c, "identities")!).Items[0], Attr(c, "__dict__")), "custom");
        custom.Delete("globals"); Expected<PyNameError>(() => Invoke(c, "direct_rebound"), "custom-delete");
        custom.Set("globals", replacement); Emit("custom-rebind", Invoke(c, "direct_rebound"));
        var d = Make(defaults);
        Check(ReferenceEquals(((PyTuple)Invoke(d, "identities")!).Items[0], Attr(d, "__dict__")), "default");
        Check(ReferenceEquals(((PyDict)Attr(Attr(d, "identities"), "__builtins__")!).Get("globals"), defaults.Get("globals")), "canonical identity");
        Emit("default", true);
    }
    static void DefaultTable()
    {
        var module = Make(defaults); var canonical = defaults.Get("globals");
        try
        {
            Emit("default-identity", ReferenceEquals(((PyTuple)Invoke(module, "identities")!).Items[0], Attr(module, "__dict__")));
            defaults.Delete("globals"); Expected<PyNameError>(() => Invoke(module, "direct_rebound"), "default-delete");
            defaults.Set("globals", Native("replacement", 0, _ => "replacement"));
            Emit("default-rebind", Invoke(module, "direct_rebound"));
        }
        finally { defaults.Set("globals", canonical); }
        Emit("default-restored", ReferenceEquals(((PyTuple)Invoke(module, "identities")!).Items[0], Attr(module, "__dict__")));
    }
    static void Indirect()
    {
        // canonical本身真实无context调用；产品明确NI，与CP间接true分账。
        var indirectSpec = Call(Attr(util, "find_spec"), "indirect_subject");
        var module = Call(Attr(util, "module_from_spec"), indirectSpec)!;
        var sourceLoader = Attr(indirectSpec, "loader");
        Call(Attr(sourceLoader, "exec_module"), module);
        PyObject.SetAttr(module, "alias", defaults.Get("globals"));
        try { Invoke(module, "indirect"); }
        catch (PyNotImplementedError error)
        {
            if (error.Message != "globals requires a direct instance call context")
                throw;
            Emit("product-indirect-canonical", "NotImplementedError");
            return;
        }
        throw new InvalidOperationException("indirect 未抛 canonical 冷路径拒绝");
    }
}
