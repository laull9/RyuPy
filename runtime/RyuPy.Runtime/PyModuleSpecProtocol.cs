// ModuleSpec的repr/eq遵守逐属性消费顺序；比较的合法但尚未支持域只在实际到达该字段时拒绝。
using System;

namespace RyuPy.Runtime;

public static partial class PyModuleSpec
{
    private static void AddObjectProtocol(PyClass cls)
    {
        cls.SetAttr("__repr__", PyNativeFunctions.Create("__repr__", ["self"], [null], 1, a => Representation(a[0]!)));
        cls.SetAttr("__eq__", PyNativeFunctions.Create("__eq__", ["self", "other"], [null, null], 2,
            a => Equal(a[0]!, a[1])));
        cls.SetAttr("__hash__", PyNone.Instance);
    }

    private static string Representation(object self)
    {
        string fields = "name=" + PyRepr.Repr(Get(self, "name")) + ", loader=" + PyRepr.Repr(Get(self, "loader"));
        // 与CP一样，条件与格式化各读取一次属性；不缓存可能有副作用的描述符结果。
        if (Get(self, "origin") is not (null or PyNone)) fields += ", origin=" + PyRepr.Repr(Get(self, "origin"));
        if (Get(self, "submodule_search_locations") is not (null or PyNone))
            fields += ", submodule_search_locations=" + EmptyFormat(Get(self, "submodule_search_locations"));
        object? cls = Get(self, "__class__");
        return EmptyFormat(PyObject.GetAttr(cls, "__name__")) + "(" + fields + ")";
    }

    private static string EmptyFormat(object? value)
    {
        // CP无转换f-string调用空format；复用已支持用户__format__的公共format核心，
        // 旧FormatValue的无转换分支仍走str，不能拿它省掉此处用户协议。
        object formatted = PyBuiltinFormat.Format(value, string.Empty, string.Empty);
        return PyStringLayout.TryGet(formatted, out string text) ? text
            : throw new PyTypeError("__format__ must return a str");
    }

    private static object Equal(object self, object? other)
    {
        // self.locations读取在try之外；此处缺失必须抛AttributeError，而非NotImplemented。
        object? locations = Get(self, "submodule_search_locations");
        try
        {
            foreach (string field in new[] { "name", "loader", "origin" })
                if (!FieldEqual(Get(self, field), PyObject.GetAttr(other, field))) return false;
            if (!FieldEqual(locations, PyObject.GetAttr(other, "submodule_search_locations"))) return false;
            if (!FieldEqual(Get(self, "cached"), PyObject.GetAttr(other, "cached"))) return false;
            return FieldEqual(Get(self, "has_location"), PyObject.GetAttr(other, "has_location"));
        }
        catch (PyAttributeError) { return PyNotImplemented.Instance; }
    }

    private static bool FieldEqual(object? left, object? right)
    {
        // 现通用比较ABI返回bool；ModuleSpec的and链还可返回自定义比较的原非bool值，不能静默吞掉。
        // 普通数据、容器及无自定义eq的对象沿既有协议，含用户eq的直接字段延期到该消费点。
        if (CustomEquality(left) || CustomEquality(right))
            throw new PyNotImplementedError("ModuleSpec equality with custom field comparison is not implemented");
        return PyHash.Equal(left, right);
    }

    // sealed PyBaseObject 无可变实例槽，object根类型不可改；它的比较只有身份语义。
    private static bool CustomEquality(object? value) => value is PyNativeObject and not PyBaseObject
        || value is PyInstance instance && instance.Class.TryLookup("__eq__", out _, out _)
            && !PyCompiledModuleLoader.HasBuiltinEquality(instance);
}
