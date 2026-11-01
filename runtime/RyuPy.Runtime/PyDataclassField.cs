using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>数据类字段声明；默认对象与工厂分别保存，不在类定义时调用工厂。</summary>
public sealed class PyDataclassField : PyNativeObject
{
    internal object? Default { get; }
    internal object? Factory { get; }
    internal bool Init { get; }
    internal bool Repr { get; }
    internal bool Compare { get; }
    internal string? Name { get; set; }
    internal object? Annotation { get; set; } = PyNone.Instance;
    internal PyDataclassField(object? value, object? factory, bool init, bool repr, bool compare)
    { Default = value; Factory = factory; Init = init; Repr = repr; Compare = compare; }
    /// <summary>Python公开类型名称。</summary>
    public override string TypeName => "Field";
    /// <summary>读取字段的受支持元数据。</summary>
    public override object? GetAttr(string name) => name switch
    {
        "name" => Name is null ? PyNone.Instance : Name,
        "type" => Annotation,
        "default" => Default, "default_factory" => Factory,
        "init" => Init, "repr" => Repr, "compare" => Compare,
        _ => throw Missing(name),
    };
}

/// <summary>dataclasses有限运行时API，复用已有类对象生成方法。</summary>
public static class PyDataclasses
{
    internal static readonly object Missing = new MissingValue();
    /// <summary>构造模块属性；MISSING与None严格区分。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new()
    {
        ["dataclass"] = PyNativeFunctions.Create("dataclass",
            ["cls", "init", "repr", "eq", "order", "unsafe_hash", "frozen", "match_args", "kw_only", "slots", "weakref_slot"],
            [PyNone.Instance, true, true, true, false, false, false, true, false, false, false], 0,
            args =>
            {
                if (!PyOps.Truthy(args[1]) || !PyOps.Truthy(args[2]) || !PyOps.Truthy(args[3])
                    || PyOps.Truthy(args[4]) || PyOps.Truthy(args[5]) || !PyOps.Truthy(args[7])
                    || PyOps.Truthy(args[8]) || PyOps.Truthy(args[9]) || PyOps.Truthy(args[10]))
                    throw new PyNotImplementedError("dataclass currently supports only the frozen option");
                bool frozen = PyOps.Truthy(args[6]);
                return args[0] is null or PyNone
                    ? PyNativeFunctions.Create("dataclass decorator", ["cls"], [null], 1,
                        bound => PyOps.ApplyDataclass(bound[0]!, frozen))
                    : PyOps.ApplyDataclass(args[0]!, frozen);
            }, ["pos", "kwonly", "kwonly", "kwonly", "kwonly", "kwonly", "kwonly", "kwonly", "kwonly", "kwonly", "kwonly"]),
        ["is_dataclass"] = PyNativeFunctions.Create("is_dataclass", ["obj"], [null], 1,
            args => DataclassType(args[0]) is { DataclassActive: true }),
        ["fields"] = PyNativeFunctions.Create("fields", ["class_or_instance"], [null], 1, args =>
        {
            PyClass cls = DataclassType(args[0]) is { DataclassActive: true } found ? found
                : throw new PyTypeError("must be called with a dataclass type or instance");
            var mapping = cls.Attributes["__dataclass_fields__"] as PyDict
                ?? throw new PyNotImplementedError("custom dataclass field mappings are not supported");
            var fields = new List<object?>();
            foreach (object? key in mapping.Keys) fields.Add(mapping.Get(key));
            return new PyTuple(fields.ToArray());
        }),
        ["MISSING"] = Missing, ["FrozenInstanceError"] = PyType.Get("FrozenInstanceError"),
        ["field"] = PyNativeFunctions.Create("field",
            ["default", "default_factory", "init", "repr", "hash", "compare", "metadata", "kw_only"],
            [Missing, Missing, true, true, PyNone.Instance, true, PyNone.Instance, Missing], 0,
            args =>
            {
                if (!ReferenceEquals(args[0], Missing) && !ReferenceEquals(args[1], Missing))
                    throw new PyValueError("cannot specify both default and default_factory");
                if (args[4] is not (null or PyNone) || args[6] is not (null or PyNone)
                    || !ReferenceEquals(args[7], Missing))
                    throw new PyNotImplementedError("field hash, metadata and kw_only options are not supported yet");
                return new PyDataclassField(args[0], args[1], PyOps.Truthy(args[2]),
                    PyOps.Truthy(args[3]), PyOps.Truthy(args[5]));
            }, ["kwonly", "kwonly", "kwonly", "kwonly", "kwonly", "kwonly", "kwonly", "kwonly"]),
    };

    private static PyClass? DataclassType(object? value) => value switch
    { PyClass cls => cls, PyInstance instance => instance.Class, _ => null };

    private sealed class MissingValue : PyNativeObject
    {
        public override string TypeName => "_MISSING_TYPE";
        public override object? GetAttr(string name) => throw Missing(name);
    }
}
