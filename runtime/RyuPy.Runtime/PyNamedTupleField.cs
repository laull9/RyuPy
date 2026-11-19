// 固定下标的tuple字段描述符；__get__接受整个tuple家族，不错误绑定某个生成类身份。
using System;

namespace RyuPy.Runtime;

internal sealed class PyNamedTupleField(int index) : PyNativeObject, IPyNativeDataDescriptor
{
    private readonly string Doc = $"Alias for field number {index}";
    public override string TypeName => "_tuplegetter";
    public object? Bind(PyClass owner, object? self) => Read(self);

    private object? Read(object? instance)
    {
        if (instance is null or PyNone) return this;
        if (instance is not PyTuple tuple)
            throw PyNamedTupleLayout.Error(new PyTypeError($"descriptor for index '{index}' for tuple subclasses doesn't apply to '{PyRepr.TypeName(instance)}' object"));
        if ((uint)index >= (uint)tuple.Length)
            throw PyNamedTupleLayout.Error(new PyIndexError("tuple index out of range"));
        return tuple.Items[index];
    }

    public override object? GetAttr(string name) => name switch
    {
        "__doc__" => Doc,
        "__class__" => PyType.Get("_tuplegetter"),
        "__get__" or "__set__" or "__delete__" => new Accessor(this, name),
        "__dict__" or "__reduce__" or "__reduce_ex__" => throw PyNamedTupleLayout.Unsupported("namedtuple field descriptor reflection is not supported"),
        _ => throw Missing(name),
    };

    public override void SetAttr(string name, object? value) => throw PyNamedTupleLayout.Unsupported("changing namedtuple field descriptor metadata is not supported");
    public override void DelAttr(string name) => throw PyNamedTupleLayout.Unsupported("changing namedtuple field descriptor metadata is not supported");
    public override string PythonRepr() => $"_tuplegetter({index}, {PyRepr.Repr(Doc)})";

    // 使用独立受保护callable，不暴露一个可假写__code__/__defaults__的PyFunction。
    private sealed class Accessor(PyNamedTupleField field, string name) : PyNativeObject, IPyNativeCallable
    {
        public override string TypeName => "method-wrapper";
        public override object? GetAttr(string attribute) => attribute switch
        {
            "__name__" => name,
            "__qualname__" => "_tuplegetter." + name,
            "__self__" => field,
            "__objclass__" => PyType.Get("_tuplegetter"),
            "__class__" => PyType.Get("method-wrapper"),
            "__dict__" or "__code__" or "__defaults__" => throw PyNamedTupleLayout.Unsupported("namedtuple descriptor accessor reflection is not supported"),
            _ => throw Missing(attribute),
        };
        public override void SetAttr(string attribute, object? value) => throw PyNamedTupleLayout.Unsupported("changing namedtuple descriptor accessor metadata is not supported");
        public override void DelAttr(string attribute) => throw PyNamedTupleLayout.Unsupported("changing namedtuple descriptor accessor metadata is not supported");

        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            int minimum = name == "__set__" ? 2 : 1;
            int maximum = name == "__get__" ? 2 : minimum;
            if (names.Length != 0 || args.Length < minimum || args.Length > maximum)
                throw PyNamedTupleLayout.Error(new PyTypeError($"{name} requires {minimum} to {maximum} positional arguments"));
            if (name == "__get__")
            {
                // 显式 wrapper 的双 None 无效；内部 Bind(None) 类读取仍返回描述符本身。
                if ((args[0] is null or PyNone) && (args.Length == 1 || args[1] is null or PyNone))
                    throw PyNamedTupleLayout.Error(new PyTypeError("__get__(None, None) is invalid"));
                return field.Read(args[0]);
            }
            throw PyNamedTupleLayout.Error(new PyAttributeError(name == "__set__" ? "can't set attribute" : "can't delete attribute"));
        }
    }
}
