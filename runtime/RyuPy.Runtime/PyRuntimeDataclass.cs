using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

public sealed partial class PyClass
{
    internal string[]? DataclassFields { get; set; }
    internal bool DataclassFrozen { get; set; }
    internal bool DataclassActive { get; set; }

    internal void CheckDataclassWrite(string name, bool deleting)
    {
        if (DataclassActive && DataclassFrozen)
            throw new PyFrozenInstanceError($"cannot {(deleting ? "delete" : "assign to")} field '{name}'");
    }
}

/// <summary>冻结数据类的写入错误；保留 AttributeError 继承关系。</summary>
public sealed class PyFrozenInstanceError : PyAttributeError
{
    /// <summary>构造冻结实例错误。</summary>
    /// <param name="message">错误消息。</param>
    public PyFrozenInstanceError(string message) : base("FrozenInstanceError", message, [message]) { }
}

public static partial class PyOps
{
    [ThreadStatic]
    private static HashSet<PyInstance>? _dataclassReprActive;
    // 工厂缺参标记不是公开MISSING；显式传入MISSING仍是用户的实际值。
    private static readonly object DataclassFactoryDefault = new();

    /// <summary>只登记原始动态数据类的必填字段，不提前执行 dataclass 装饰标记。</summary>
    /// <param name="cls">原始类对象。</param>
    /// <param name="fieldNames">按声明顺序排列的字段名。</param>
    /// <param name="frozen">是否冻结实例。</param>
    public static void DeclareDataclass(object cls, object?[] fieldNames, bool frozen)
    {
        if (cls is not PyClass target) throw new PyTypeError("dataclass declaration requires a class");
        if (target.NamedTupleLayout is { IsTyped: true }) return;
        if (target.IsNativeRuntimeClass) PyTypingNamedTupleShell.RejectPending(target);
        PyNamedTupleLayout.GuardMutation(target);
        var fields = new string[fieldNames.Length];
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < fields.Length; index++)
        {
            if (fieldNames[index] is not string name || !seen.Add(name))
                throw new PyTypeError("dataclass field names must be unique strings");
            fields[index] = name;
        }
        target.DataclassFields = fields;
        target.DataclassFrozen = frozen;
    }

    /// <summary>在装饰器逆序应用位置激活当前类自己的声明，不借用被替换类的字段。</summary>
    /// <param name="current">下层装饰器返回的当前对象。</param>
    /// <param name="frozen">当前 dataclass 标记的选项，与字段声明所属类分开。</param>
    public static object ApplyDataclass(object current, bool frozen)
    {
        if (current is not PyClass cls) throw new PyTypeError("dataclass decorator requires a class");
        // 普通带注解的类仍可 Declare；真正装饰前拒绝，不能留下字段或快计划半变更。
        if (cls.IsTypingIoFamily) throw PyTypingIo.Unsupported("dataclass on typing IO classes is not supported");
        PyNamedTupleLayout.GuardMutation(cls);
        if (cls.HasStringLayout) throw new PyNotImplementedError("dataclass on str subclass is not supported");
        if (cls.HasListLayout) throw new PyNotImplementedError("dataclass on list subclass is not supported");
        if (cls.HasDictLayout) throw new PyNotImplementedError("dataclass on dict subclass is not supported");
        if (cls.DataclassActive) throw new PyNotImplementedError("reapplying dataclass is not supported yet");
        if (cls.BaseClass is not null || cls.BuiltinBase is not null)
            throw new PyNotImplementedError("runtime dataclass inheritance is not supported yet");
        string[] fields = cls.DataclassFields ?? [];
        PyDict? annotations = null;
        if (cls.Attributes.TryGetValue("__annotations__", out object? annotationObject))
        {
            annotations = annotationObject as PyDict
                ?? throw new PyNotImplementedError("dataclass annotations require a dict");
            var names = new List<string>();
            foreach (object? key in annotations.Keys)
                names.Add(key as string ?? throw new PyTypeError("dataclass field names must be strings"));
            fields = names.ToArray();
        }
        var declarations = new PyDataclassField[fields.Length];
        bool hasDefault = false;
        for (int index = 0; index < fields.Length; index++)
        {
            string field = fields[index];
            object? value = cls.Attributes.TryGetValue(field, out object? declared)
                ? declared : PyDataclasses.Missing;
            var metadata = value as PyDataclassField ?? new PyDataclassField(value,
                PyDataclasses.Missing, true, true, true);
            metadata.Name = field;
            metadata.Annotation = annotations?.GetOrDefault(field, PyNone.Instance) ?? PyNone.Instance;
            declarations[index] = metadata;
            bool optional = !ReferenceEquals(metadata.Default, PyDataclasses.Missing)
                || !ReferenceEquals(metadata.Factory, PyDataclasses.Missing);
            if (metadata.Init && hasDefault && !optional)
                throw new PyTypeError($"non-default argument '{field}' follows default argument");
            hasDefault |= metadata.Init && optional;
            // 真实list派生的根hash为None；不借类名字或普通用户hash策略猜测可变布局。
            if (metadata.Default is PyList or PyListInstance or PyDict or PyDictInstance or PySet or PyByteArray or PyDeque or IPyNativeMapping
                || metadata.Default is PyInstance instanceDefault
                && instanceDefault.Class.TryLookup("__hash__", out _, out var hash)
                && hash is null or PyNone)
                throw new PyValueError(metadata.Default is PyListInstance listDefault
                    ? $"mutable default {PyListLayout.ClassRepr(listDefault.Class)} for field {field} is not allowed: use default_factory"
                    : metadata.Default is PyDictInstance dictDefault
                    ? $"mutable default {PyDictLayout.ClassRepr(dictDefault.Class)} for field {field} is not allowed: use default_factory"
                    : $"mutable default for field {field} is not allowed: use default_factory");
            // field()声明本身不是实例默认值；公开类属性只保留真实default。
            if (value is PyDataclassField)
            {
                if (ReferenceEquals(metadata.Default, PyDataclasses.Missing)) cls.DeleteAttr(field);
                else cls.SetAttr(field, metadata.Default);
            }
        }
        // CP先移除/替换field声明，再于生成eq之前快照own hash/eq。
        // None+own eq（即使None显式写入）视作隐式hash；不能把field占位当显式hash保留。
        bool hasExplicitHash = cls.Attributes.TryGetValue("__hash__", out object? declaredHash)
            && !((declaredHash is null or PyNone) && cls.Attributes.ContainsKey("__eq__"));
        // 装饰器选项属于此次应用，不能因下层替换了类而丢失或借用替换类的旧选项。
        cls.DataclassFrozen = frozen;
        if (!cls.Attributes.ContainsKey("__init__"))
        {
            var parameters = new List<string> { "__dataclass_self__" };
            while (Array.IndexOf(fields, parameters[0]) >= 0) parameters[0] += "_";
            var defaults = new List<object?> { null };
            var flags = new List<string> { "0" };
            foreach (var field in declarations)
            {
                if (!field.Init) continue;
                parameters.Add(field.Name!);
                bool factory = !ReferenceEquals(field.Factory, PyDataclasses.Missing);
                defaults.Add(factory ? DataclassFactoryDefault : field.Default);
                flags.Add(factory || !ReferenceEquals(field.Default, PyDataclasses.Missing) ? "1" : "0");
            }
            var kinds = new string[parameters.Count];
            Array.Fill(kinds, "pos");
            var signature = PySignature.Create(cls.Name + ".__init__", parameters.ToArray(), kinds, flags.ToArray());
            // CPython在生成方法时决定是否包含post-init调用；之后只动态解析已生成的调用。
            bool hasPostInit = cls.TryLookup("__post_init__", out _, out _);
            var generatedInit = new PyFunction("__init__", signature, (self, args, names, values) =>
            {
                object?[] bound = PyBinder.Bind(signature, args, names, values, self.Defaults);
                PyInstance instance = DataclassInstance(bound[0]);
                int next = 1;
                foreach (var field in declarations)
                {
                    object? value = field.Init ? bound[next++] : DataclassFactoryDefault;
                    if (ReferenceEquals(value, DataclassFactoryDefault))
                    {
                        if (ReferenceEquals(field.Factory, PyDataclasses.Missing)) continue;
                        value = PyOps.CallValue(field.Factory, [], [], []);
                    }
                    if (cls.DataclassFrozen) PyObject.InitializeDataclassField(instance, field.Name!, value);
                    else PyObject.SetAttr(instance, field.Name!, value);
                }
                if (hasPostInit)
                    PyOps.CallValue(PyObject.GetAttr(instance, "__post_init__"), [], [], []);
                return PyNone.Instance;
            }, defaults.ToArray(), false);
            cls.SetAttr("__init__", generatedInit);
            cls.RegisterDataclassConstructor(generatedInit, declarations, hasPostInit);
        }
        string[] reprFields = Array.ConvertAll(Array.FindAll(declarations, field => field.Repr), field => field.Name!);
        string[] compareFields = Array.ConvertAll(Array.FindAll(declarations, field => field.Compare), field => field.Name!);
        if (!cls.Attributes.ContainsKey("__repr__"))
            cls.SetAttr("__repr__", DataclassMethod(cls.Name + ".__repr__", ["self"], bound =>
                DataclassRepr(DataclassInstance(bound[0]), reprFields)));
        if (!cls.Attributes.ContainsKey("__eq__"))
            cls.SetAttr("__eq__", DataclassMethod(cls.Name + ".__eq__", ["self", "other"], bound =>
            {
                PyInstance self = DataclassInstance(bound[0]);
                if (bound[1] is not PyInstance other || !ReferenceEquals(self.Class, other.Class))
                    return PyNotImplemented.Instance;
                // CPython先分别构造左右字段tuple，再比较，getter副作用不可交错。
                var left = new object?[compareFields.Length];
                var right = new object?[compareFields.Length];
                for (int index = 0; index < compareFields.Length; index++)
                    left[index] = PyObject.GetAttr(self, compareFields[index]);
                for (int index = 0; index < compareFields.Length; index++)
                    right[index] = PyObject.GetAttr(other, compareFields[index]);
                return PyHash.Equal(new PyTuple(left), new PyTuple(right));
            }));
        if (!hasExplicitHash && !frozen) cls.SetAttr("__hash__", PyNone.Instance);
        if (!hasExplicitHash && frozen)
            cls.SetAttr("__hash__", DataclassMethod(cls.Name + ".__hash__", ["self"], bound =>
            {
                if (!cls.DataclassFrozen) throw new PyTypeError($"unhashable type: '{cls.Name}'");
                var values = new object?[compareFields.Length];
                for (int index = 0; index < compareFields.Length; index++)
                    values[index] = PyObject.GetAttr(bound[0], compareFields[index]);
                return PyHash.Of(new PyTuple(values));
            }));
        if (!cls.Attributes.ContainsKey("__match_args__")) cls.SetAttr("__match_args__", new PyTuple(Array.ConvertAll(Array.FindAll(declarations, field => field.Init), field => field.Name!)));
        var fieldMap = new PyDict();
        foreach (var field in declarations) fieldMap.Set(field.Name!, field);
        cls.SetAttr("__dataclass_fields__", fieldMap);
        cls.DataclassActive = true;
        cls.ConstructionLayoutChanged();
        return cls;
    }

    /// <summary>读取当前类命名空间原始属性，不进行描述符绑定或继承查找。</summary>
    /// <param name="cls">正在执行类体的原始类对象。</param>
    /// <param name="name">类体绑定名。</param>
    public static object? ClassNamespaceGet(object cls, string name)
    {
        if (cls is not PyClass target) throw new PyTypeError("class namespace target must be a class");
        return target.Attributes.TryGetValue(name, out object? value) ? value
            : throw new PyNameError($"name '{name}' is not defined");
    }

    private static PyFunction DataclassMethod(string name, string[] parameters, Func<object?[], object?> body)
    {
        var kinds = new string[parameters.Length];
        var defaults = new string[parameters.Length];
        Array.Fill(kinds, "pos");
        Array.Fill(defaults, "0");
        PySignature signature = PySignature.Create(name, parameters, kinds, defaults);
        string functionName = name[(name.LastIndexOf('.') + 1)..];
        return new PyFunction(functionName, signature,
            (self, args, names, values) => body(PyBinder.Bind(signature, args, names, values, self.Defaults)), [], false);
    }

    private static PyInstance DataclassInstance(object? value) => value as PyInstance
        ?? throw new PyTypeError("dataclass method requires an instance");

    private static string DataclassRepr(PyInstance instance, string[] fields)
    {
        var active = _dataclassReprActive ??= new HashSet<PyInstance>(ReferenceEqualityComparer.Instance);
        if (!active.Add(instance)) return "...";
        try
        {
            var parts = new string[fields.Length];
            for (int index = 0; index < fields.Length; index++)
                parts[index] = fields[index] + "=" + PyRepr.Repr(PyObject.GetAttr(instance, fields[index]));
            return instance.Class.Name + "(" + string.Join(", ", parts) + ")";
        }
        finally { active.Remove(instance); }
    }
}
