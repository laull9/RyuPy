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

    /// <summary>只登记原始动态数据类的必填字段，不提前执行 dataclass 装饰标记。</summary>
    /// <param name="cls">原始类对象。</param>
    /// <param name="fieldNames">按声明顺序排列的字段名。</param>
    /// <param name="frozen">是否冻结实例。</param>
    public static void DeclareDataclass(object cls, object?[] fieldNames, bool frozen)
    {
        if (cls is not PyClass target) throw new PyTypeError("dataclass declaration requires a class");
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
        if (cls.BaseClass is not null || cls.BuiltinBase is not null)
            throw new PyNotImplementedError("runtime dataclass inheritance is not supported yet");
        string[] fields = cls.DataclassFields ?? [];
        // 当前仅生成必填字段；普通类属性值是 dataclass 默认值，不能悄悄忽略。
        // property 仍保留显式传参时的 setter 路径，省略参数不承诺 descriptor 默认值语义。
        foreach (string field in fields)
            if (cls.Attributes.TryGetValue(field, out object? declared) && declared is not PyProperty)
                throw new PyNotImplementedError("runtime dataclass field defaults are not supported yet");
        // 装饰器选项属于此次应用，不能因下层替换了类而丢失或借用替换类的旧选项。
        cls.DataclassFrozen = frozen;
        if (!cls.Attributes.ContainsKey("__init__"))
        {
            var names = new string[fields.Length + 1];
            names[0] = "__dataclass_self__";
            while (Array.IndexOf(fields, names[0]) >= 0) names[0] += "_";
            Array.Copy(fields, 0, names, 1, fields.Length);
            cls.SetAttr("__init__", DataclassMethod(cls.Name + ".__init__", names, bound =>
            {
                PyInstance instance = DataclassInstance(bound[0]);
                for (int index = 0; index < fields.Length; index++)
                {
                    if (cls.DataclassFrozen)
                        PyObject.InitializeDataclassField(instance, fields[index], bound[index + 1] ?? PyNone.Instance);
                    else
                        PyObject.SetAttr(instance, fields[index], bound[index + 1] ?? PyNone.Instance);
                }
                return PyNone.Instance;
            }));
        }
        if (!cls.Attributes.ContainsKey("__repr__"))
            cls.SetAttr("__repr__", DataclassMethod(cls.Name + ".__repr__", ["self"], bound =>
                DataclassRepr(DataclassInstance(bound[0]), fields)));
        if (!cls.Attributes.ContainsKey("__eq__"))
            cls.SetAttr("__eq__", DataclassMethod(cls.Name + ".__eq__", ["self", "other"], bound =>
            {
                PyInstance self = DataclassInstance(bound[0]);
                if (bound[1] is not PyInstance other || !ReferenceEquals(self.Class, other.Class))
                    return PyNotImplemented.Instance;
                foreach (string field in fields)
                {
                    object? left = PyObject.GetAttr(self, field), right = PyObject.GetAttr(other, field);
                    if (!ReferenceEquals(left, right) && !PyHash.Equal(left, right)) return false;
                }
                return true;
            }));
        if (!cls.Attributes.ContainsKey("__hash__"))
            cls.SetAttr("__hash__", DataclassMethod(cls.Name + ".__hash__", ["self"], bound =>
            {
                if (!cls.DataclassFrozen) throw new PyTypeError($"unhashable type: '{cls.Name}'");
                var values = new object?[fields.Length];
                for (int index = 0; index < fields.Length; index++)
                    values[index] = PyObject.GetAttr(bound[0], fields[index]);
                return PyHash.Of(new PyTuple(values));
            }));
        if (!cls.Attributes.ContainsKey("__match_args__")) cls.SetAttr("__match_args__", new PyTuple(fields));
        cls.DataclassActive = true;
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
