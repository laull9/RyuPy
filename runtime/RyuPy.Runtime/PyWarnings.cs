using System;
using System.Collections.Generic;
using System.IO;

namespace RyuPy.Runtime;

/// <summary>有限warnings原生入口：真实过滤/记录/异常与当前stderr，不用空模块掩盖未实现行为。</summary>
public static class PyWarnings
{
    internal static PyList? Records;

    /// <summary>创建可用函数；复杂正则过滤和启动选项不伪装成已支持。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new()
    {
        ["warn"] = PyNativeFunctions.Create("warn",
            ["message", "category", "stacklevel", "source", "skip_file_prefixes"],
            [null, PyNone.Instance, 1L, PyNone.Instance, new PyTuple([])], 1, a =>
            {
                if (a[4] is not PyTuple prefixes) throw new PyTypeError("skip_file_prefixes must be a tuple");
                if (prefixes.Items.Length != 0) throw new PyNotImplementedError("warnings skip_file_prefixes is not supported");
                Warn(a[0], a[1], PyItertools.Index(a[2]), a[3]);
                return null;
            }, ["pos", "pos", "pos", "pos", "kwonly"]),
        ["simplefilter"] = PyNativeFunctions.Create("simplefilter",
            ["action", "category", "lineno", "append"], [null, PyType.Get("Warning"), 0L, false], 1,
            a => { PyWarningFilters.Simple(a[0], a[1], a[2], a[3]); return null; }),
        ["catch_warnings"] = ContextType(),
    };

    // catch_warnings 是类，不以函数对象伪装；仍复用同一关键字参数绑定器。
    private static PyNativeType ContextType()
    {
        var constructor = PyNativeFunctions.Create("catch_warnings",
            ["record", "module", "action", "category", "lineno", "append"],
            [false, PyNone.Instance, PyNone.Instance, PyType.Get("Warning"), 0L, false], 0,
            a => new PyWarningContext(a), ["kwonly", "kwonly", "kwonly", "kwonly", "kwonly", "kwonly"]);
        return new PyNativeType("catch_warnings", "warnings",
            (args, names, values) => PyOps.CallValue(constructor, args, names, values), value => value is PyWarningContext);
    }

    /// <summary>未实现的替换钩子不能接受后悄悄忽略；其它模块属性按普通Python模块保留。</summary>
    public static void BeforeSet(string name, object? value)
    {
        if (name is "filters" or "showwarning" or "formatwarning" or "_showwarnmsg_impl")
            throw new PyNotImplementedError("replacing warnings." + name + " is not supported");
    }

    /// <summary>固定 Python 3.12 的哨兵真值规则：先发弃用警告，再返回 true。</summary>
    internal static bool BooleanNotImplemented()
    {
        Warn("NotImplemented should not be used in a boolean context", PyType.Get("DeprecationWarning"), 1, null);
        return true;
    }

    // Element 的真值沿同一过滤/记录/异常与调用位置合同，不忽略 CP3.12 可见弃用警告。
    internal static bool BooleanElement(int children)
    {
        Warn("Testing an element's truth value will always return True in future versions.  Use specific 'len(elem)' or 'elem is not None' test instead.",
            PyType.Get("DeprecationWarning"), 1, null);
        return children != 0;
    }

    // 整数协议的窄通知复用真实过滤、caller与stderr；直接bool参数不经过这里。
    internal static void IndexReturnedStrictSubclass(string typeName) =>
        Warn($"__index__ returned non-int (type {typeName}).  The ability to return an instance of a strict subclass of int is deprecated, and may be removed in a future version of Python.",
            PyType.Get("DeprecationWarning"), 1, null);

    // Enum 3.12默认生成规则的弃用通知，复用真实过滤、记录、error与Python调用行。
    internal static void EnumAutoGeneration() =>
        Warn("In 3.13 the default `auto()`/`_generate_next_value_` will require all values to be sortable and support adding +1\n"
            + "and the value returned will be the largest value in the enum incremented by 1",
            PyType.Get("DeprecationWarning"), 1, null);

    private static void Warn(object? message, object? category, long stacklevel, object? source)
    {
        if (source is not (null or PyNone))
            throw new PyNotImplementedError("warnings source allocation tracing is not supported");
        // 类别选择/验证先于None抑制；已有Warning实例忽略显式category。
        PyBaseException? existing = message is PyBaseException candidate
            && (candidate is PyWarning || candidate.PythonClass?.IsSubclassOfBuiltin("Warning") == true)
            ? candidate : null;
        category = existing is not null ? (object?)existing.PythonClass ?? PyType.Get(existing.PyTypeName)
            : category is null or PyNone ? PyType.Get("UserWarning") : category;
        if (!PyWarningFilters.IsCategory(category)) throw new PyTypeError("category must be a Warning subclass");
        PyCallerSource caller = PyTraceback.CallerSource(stacklevel);
        string? module = null;
        if (caller.Known)
        {
            // CP先取得选中帧的活globals，再构造/格式化消息；后续用户副作用不重选环境。
            bool present = caller.TryReadName(out object? rawName);
            if (present && rawName is null or PyNone) return;
            module = present && PyStringLayout.TryGet(rawName, out string name) ? name : "<string>";
        }
        object? text = message;
        PyBaseException warning;
        if (existing is not null)
        {
            warning = existing;
            text = PyRepr.Str(existing);
        }
        else
        {
            warning = category switch
            {
                PyType type => PyWarningTypes.Create(type.Name, [message]),
                PyClass cls => cls.Invoke([message], [], []) as PyBaseException
                    ?? throw new PyTypeError("warning category did not return an exception"),
                _ => throw new PyTypeError("category must be a Warning subclass"),
            };
        }
        // 真旧载体仍保消息构造后的旧扫描；新Static已经是Known，不能混入旧表。
        PyStackFrame frame = caller.Known ? caller.Frame : PyTraceback.GetCallerLocation(stacklevel);
        module ??= frame.File == "sys" ? "sys" : PyModules.ModuleForFile(frame.File)
            ?? throw new PyNotImplementedError("warning module cannot be resolved for source " + frame.File);
        string action = caller.Known
            ? PyWarningFilters.ActionKnown(text, category!, module, frame.Line, caller.Environment)
            : PyWarningFilters.Action(text, category!, module, frame.Line);
        if (action == "ignore") return;
        if (action == "error") throw warning;
        var record = new PyWarningMessage(warning, category!, frame.File, frame.Line, source);
        if (Records is { } records) { records.Append(record); return; }
        Write(record);
    }

    private static void Write(PyWarningMessage record)
    {
        object? target = PySystem.Stderr;
        if (target is null or PyNone) return;
        // 输出才格式化新构造的警告；若原message已是Warning，过滤阶段也曾调用str。
        string text = PyRepr.Str(record.Message);
        string category = record.Category is PyClass cls ? cls.Name : ((PyType)record.Category).Name;
        string output = $"{record.Filename}:{record.LineNumber}: {category}: {text}\n";
        // 和Python linecache一样，源码不可读时仍显示首行；不让显示源码的失败掩盖警告。
        try
        {
            if (File.Exists(record.Filename))
            {
                int line = 0;
                foreach (string sourceLine in File.ReadLines(record.Filename))
                    if (++line == record.LineNumber) { output += "  " + sourceLine.Trim() + "\n"; break; }
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        // 默认showwarning忽略IO失败，但自定义write的其它Python异常必须传播。
        try { PySystem.Write(target, output); }
        catch (PyOSError) { }
    }
}
