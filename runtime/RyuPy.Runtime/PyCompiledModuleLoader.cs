// 已编译loader的来源对象保独立身份；相等比较真实实例字段，不冒充SourceFileLoader；公开执行仅消费已捕获的实例代码。
using System;
using System.Linq;
using System.Numerics;

namespace RyuPy.Runtime;

internal static partial class PyCompiledModuleLoader
{
    private static readonly PyFunction Equality = PyNativeFunctions.Create("__eq__", ["self", "other"],
        [null, null], 2, args => Equal(args[0], args[1]));
    private static readonly Lazy<PyClass> LoaderClass = new(Build);

    private static PyClass Build()
    {
        var cls = new PyClass("CompiledModuleLoader") { NativeDefinitionModule = "_ryupy_importlib" };
        cls.SetAttr("__module__", "_ryupy_importlib");
        cls.SetAttr("__eq__", Equality);
        cls.SetAttr("create_module", CreateModuleFunction);
        cls.SetAttr("exec_module", ExecModuleFunction);
        cls.SetAttr("__hash__", PyNativeFunctions.Create("__hash__", ["self"], [null], 1,
            args => PyHash.Of(PyObject.GetAttr(args[0], "name")) ^ PyHash.Of(PyObject.GetAttr(args[0], "path"))));
        PyOps.FinalizeOrdinaryClass(cls);
        OriginalAttributes = new(cls.Attributes, StringComparer.Ordinal);
        return cls;
    }

    internal static PyInstance Create(string name, string origin, PyCompiledModuleCode? code = null)
    {
        var loader = new PyInstance(LoaderClass.Value);
        PyObject.SetAttr(loader, "name", name);
        PyObject.SetAttr(loader, "path", origin);
        Capabilities.Add(loader, new(code, name, origin));
        return loader;
    }

    // 仅此未被替换的实现保证bool结果；用户替换__eq__仍走ModuleSpec已有明确边界。
    internal static bool HasBuiltinEquality(PyInstance instance) => LoaderClass.IsValueCreated
        && ReferenceEquals(instance.Class, LoaderClass.Value)
        && instance.Class.TryLookup("__eq__", out _, out object? method) && ReferenceEquals(method, Equality);

    private static bool Equal(object? left, object? right)
    {
        if (left is not PyInstance a || right is not PyInstance b || !ReferenceEquals(a.Class, b.Class)) return false;
        // 普通来源对象只有两个精确字符串槽；此守卫无用户代码且不分配字典。
        // 新增/删除字段或改成用户对象即退回完整字段比较，不能忽略可观察修改。
        if (a.Class.InstanceSlotCount == 2
            && StringField(a, "name", out string? an) && StringField(b, "name", out string? bn)
            && StringField(a, "path", out string? ap) && StringField(b, "path", out string? bp))
            return string.Equals(an, bn, StringComparison.Ordinal) && string.Equals(ap, bp, StringComparison.Ordinal);
        // 字段字典先按身份、再按值比较；同一个NaN不能误走表达式NaN != NaN。
        // 此有限路径不进入用户比较，故快照不会掩盖比较回调重入修改后续字段。
        PyDict first = Fields(a), second = Fields(b);
        if (first.Count != second.Count) return false;
        foreach (var (key, value) in first.Entries())
        {
            if (!second.TryGet(key, out object? other)) return false;
            if (ReferenceEquals(value, other)) continue;
            if (!ScalarField(value) || !ScalarField(other))
                throw new PyNotImplementedError("compiled loader equality with custom or container fields is not implemented");
            if (!PyHash.Equal(value, other)) return false;
        }
        return true;
    }

    private static bool ScalarField(object? value) => value is null or PyNone or bool or int or long
        or BigInteger or double or string or PyBaseObject or PyEllipsis or PyNotImplemented;

    private static bool StringField(PyInstance instance, string name, out string? text)
    {
        text = null;
        if (!instance.Class.TryGetInstanceSlot(name, out int slot) || !instance.TryGetSlot(slot, out object? value)) return false;
        text = value as string;
        return text is not null;
    }

    private static PyDict Fields(PyInstance instance)
    {
        var fields = new PyDict();
        foreach (string name in instance.Class.AssignedInstanceNames(instance).ToArray())
            if (instance.Class.TryGetInstanceSlot(name, out int slot) && instance.TryGetSlot(slot, out object? value))
                fields.Set(name, value);
        return fields;
    }
}
