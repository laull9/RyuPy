// namedtuple内部布局与字段构造绑定；输入已由未来工厂验证，当前不注册任何Python导出。
// schema、签名和defaults数组只由布局持有；公开元数据未来必须与这些私有数据分离。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal sealed partial class PyNamedTupleLayout
{
    private readonly string[] _fields;
    private readonly Dictionary<string, int> _indices;
    private readonly PySignature _constructor;
    private readonly object?[] _defaults;

    internal int FieldCount => _fields.Length;
    // 仅内部typed元数据准备可置位，不根据公开可变annotations/类名推断类型来源。
    internal bool IsTyped { get; private set; }
    internal void MarkTyped() => IsTyped = true;
    // 对外函数元数据保留稳定 tuple/None；私有绑定数组仍与公开 tuple 的槽分离。
    internal object PublicDefaults { get; set; } = PyNone.Instance;

    private PyNamedTupleLayout(string name, string[] fields, object?[] defaults)
    {
        if (defaults.Length > fields.Length)
            throw Own(new PyTypeError("Got more default values than field names"));

        // 拷贝结构，不拷贝字段值；调用方以后修改输入数组不能改变类的布局。
        _fields = (string[])fields.Clone();
        if (defaults.Length != 0) PublicDefaults = new PyTuple((object?[])defaults.Clone());
        _indices = new Dictionary<string, int>(StringComparer.Ordinal);
        var parameters = new string[_fields.Length + 1];
        var kinds = new string[parameters.Length];
        var flags = new string[parameters.Length];
        _defaults = new object?[parameters.Length];
        parameters[0] = "_cls";
        Array.Fill(kinds, "pos");
        Array.Fill(flags, "0");
        for (int index = 0; index < _fields.Length; index++)
        {
            _indices.Add(_fields[index], index);
            parameters[index + 1] = _fields[index];
        }
        int defaultStart = parameters.Length - defaults.Length;
        for (int index = 0; index < defaults.Length; index++)
        {
            _defaults[defaultStart + index] = defaults[index];
            flags[defaultStart + index] = "1";
        }
        // 隐藏类参数独立占槽，不借用合法用户字段self/cls。
        _constructor = PySignature.Create(name + ".__new__", parameters, kinds, flags);
    }

    /// <summary>创建独立的内部类身份；名称与字段的Python校验留待工厂阶段接入。</summary>
    internal static PyClass CreateClass(string name, string[] fields, object?[] defaults) =>
        CreateClassWithModule(name, fields, defaults, PyNone.Instance);

    // 来源由未来工厂显式提供；内部建类不猜模块，也不把None当成__main__。
    internal static PyClass CreateClassWithModule(string name, string[] fields, object?[] defaults, object? module)
    {
        var cls = new PyClass(name);
        PreparedClass prepared = PrepareClass(cls, fields, defaults, module);
        cls.CommitNamedTuple(prepared);
        return cls;
    }

    // 所有分配/metadata准备都先完成；helper的Owner是最终身份，不提前附着layout。
    internal sealed record PreparedClass(PyClass Owner, PyNamedTupleLayout Layout,
        Dictionary<string, object?> Attributes);

    internal static PreparedClass PrepareClass(PyClass owner, string[] fields,
        object?[] defaults, object? module)
    {
        var layout = new PyNamedTupleLayout(owner.Name, fields, defaults);
        return new PreparedClass(owner, layout, layout.PrepareAttributes(owner, module));
    }

    internal string FieldName(int index) => _fields[index];

    internal bool TryFieldIndex(string name, out int index) => _indices.TryGetValue(name, out index);

    /// <summary>生成类的专用构造路径；不经过普通PyInstance分配。</summary>
    internal static PyTuple Construct(PyClass cls, object?[] args, string[] names, object?[] values)
    {
        if (cls.NamedTupleLayout is null)
            throw Own(new PyTypeError("namedtuple construction requires a namedtuple class"));
        // 与真实__new__一样先传类；_cls的关键字冲突由同一绑定器处理。
        var withClass = new object?[args.Length + 1];
        withClass[0] = cls;
        Array.Copy(args, 0, withClass, 1, args.Length);
        return ConstructExplicit(cls, withClass, names, values);
    }

    // 显式 __new__ 按定义方签名完整绑定；不同 cls 不能借普通 Construct 伪造错误布局。
    internal static PyTuple ConstructExplicit(PyClass definingClass, object?[] args, string[] names, object?[] values)
    {
        PyNamedTupleLayout layout = definingClass.NamedTupleLayout!;
        object?[] bound = PyNamedTupleBinding.Bind(layout._constructor, args, names, values, layout._defaults);
        if (!ReferenceEquals(bound[0], definingClass))
            throw Unsupported("namedtuple __new__ with a different explicit class is not supported");

        // 满额位置绑定可能直接返回输入数组。无论哪个绑定分支，实例都另取独占槽。
        var ownedItems = new object?[layout.FieldCount];
        Array.Copy(bound, 1, ownedItems, 0, ownedItems.Length);
        return new PyNamedTupleInstance(definingClass, ownedItems);
    }

    private static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}
