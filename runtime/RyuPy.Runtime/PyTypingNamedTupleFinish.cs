// NamedTuple最终类namespace的冷转换；defaults/type/factory/name/prohibited顺序分开保留。
// 仅支持已验注解与属性域；功能式两遍fields消费由后续独立入口负责。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal static partial class PyTypingNamedTuple
{
    private static readonly HashSet<string> Prohibited = new(StringComparer.Ordinal)
    {
        "__new__", "__init__", "__slots__", "__getnewargs__", "_fields", "_field_defaults",
        "_make", "_replace", "_asdict", "_source",
    };

    private static void FinishDefinition(PyClass shell, object originalBase)
    {
        RejectTypingOverride("_NamedTuple");
        RejectTypingOverride("tuple");
        var ns = shell.Attributes;
        object? raw = ns.TryGetValue("__annotations__", out object? annotations) ? annotations : new PyDict();
        if (raw is not PyDict fields)
            throw PyNamedTupleLayout.Unsupported("NamedTuple annotations currently require a dict");
        var names = new List<string>();
        var defaults = new List<object?>();
        var defaultNames = new List<string>();
        foreach (object? key in fields.Keys)
        {
            if (key is not string name)
                throw PyNamedTupleLayout.Unsupported("NamedTuple annotation keys currently require exact strings");
            names.Add(name);
            if (ns.TryGetValue(name, out object? value))
            {
                defaults.Add(value);
                defaultNames.Add(name);
            }
            else if (defaultNames.Count != 0)
                throw PyNamedTupleLayout.Error(new PyTypeError($"Non-default namedtuple field {name} cannot follow default field"
                    + (defaultNames.Count > 1 ? "s " : " ") + string.Join(", ", defaultNames)));
        }
        RejectTypingOverride("_make_nmtuple");
        // module是_make_nmtuple调用的实参：缺键在内部注解转换之前失败。
        if (!ns.TryGetValue("__module__", out object? module))
            throw PyNamedTupleLayout.Error(PyKeyError.ForKey("__module__"));
        var normalized = new PyDict();
        foreach (string name in names)
        {
            fields.TryGet(name, out object? annotation);
            normalized.Set(name, PyTypingNamedTupleAnnotations.NormalizeAnnotation(annotation, name));
        }
        string[] fieldNames = names.ToArray();
        ValidateFactoryAndFields(shell.Name, fieldNames);
        object?[] values = defaults.ToArray();
        PyNamedTupleLayout.PreparedClass prepared = PyTypingNamedTupleShell.Prepare(shell, fieldNames, values, module);
        // collections在名称/defaults准备后才为None查询内部caller；不能猜成typing或保None。
        if (module is null or PyNone)
            throw PyNamedTupleLayout.Unsupported("NamedTuple class module=None caller fallback is not supported");
        ApplyTypedMetadata(prepared, normalized, new PyTuple(values));
        PyTypingGeneric.GuardExport(PyTyping.CreateAttributes());
        // CP在建成基础tuple类后才检查这些禁止覆盖成员；不能抢先吞字段转换错误。
        RejectTypingOverride("_prohibited");
        RejectTypingOverride("_special");
        var fieldSet = new HashSet<string>(fieldNames, StringComparer.Ordinal);
        foreach ((string name, object? value) in ns)
        {
            if (Prohibited.Contains(name))
                throw PyNamedTupleLayout.Error(new PyAttributeError("Cannot overwrite NamedTuple attribute " + name));
            if (name is "__module__" or "__name__" or "__annotations__" or "__orig_bases__" || fieldSet.Contains(name)) continue;
            if (name.StartsWith("__", StringComparison.Ordinal) && name is not ("__doc__" or "__qualname__"))
                throw PyNamedTupleLayout.Unsupported($"NamedTuple class slot {name} is not supported");
            if (name == "__qualname__")
                PyClassNamespaceMetadata.ValidateQualname(shell, value, typed: true);
            PyTypingNamedTupleMembers.Validate(value);
            prepared.Attributes[name] = value;
        }
        prepared.Attributes["__orig_bases__"] = new PyTuple([originalBase]);
        shell.CommitNamedTuple(prepared);
    }

    // C功能式可在它自己的两遍转换之后复用；此处不得提前缓存公开可写factory槽。
    internal static void ValidateFactoryAndFields(string name, string[] fields)
    {
        GuardActualFactory();
        PyNamedTupleFactory.ValidateExactFields(name, fields);
    }

    internal static void GuardActualFactory()
    {
        RejectTypingOverride("collections");
        object? factory = PyObject.GetAttr(PyModules.Import("collections"), "namedtuple");
        if (!ReferenceEquals(factory, PyNamedTupleExport.Instance))
            throw PyNamedTupleLayout.Unsupported("typing.NamedTuple with replaced collections.namedtuple is not supported");
    }

    // B/C共享元数据安装，不合并各自namespace/两遍迭代/caller来源规则。
    internal static void ApplyTypedMetadata(PyNamedTupleLayout.PreparedClass prepared,
        PyDict annotations, PyTuple defaults)
    {
        prepared.Attributes["__annotations__"] = annotations;
        prepared.Layout.PublicDefaults = defaults;
        prepared.Layout.MarkTyped();
    }
}
