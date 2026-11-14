// zip_longest 内部状态载体：按槽永久退休，非耗尽异常关闭整个对象；不预先物化输入。
// 每次推进独立结果数组，保共享输入和有限重入；真实原生类型与实例共用既有协议入口。
using System;

namespace RyuPy.Runtime;

internal sealed class PyZipLongest : PyToolIterator
{
    private static readonly Lazy<PyNativeType> NativeType = new(() => new PyNativeType(
        "zip_longest", "itertools", (args, names, values) => Create(args, names, values), value => value is PyZipLongest));
    private readonly IPyIterator?[] iterators;
    private readonly object? fill;
    private int active;
    private object? current;

    private PyZipLongest(IPyIterator?[] iterators, object? fill)
    {
        this.iterators = iterators; this.fill = fill; active = iterators.Length;
    }

    internal static PyZipLongest Create(object?[] args, string[] names, object?[] values)
    {
        _ = Type;
        // 只允许 fillvalue 一个关键字。先拒绝未知名字，之后才按顺序触发用户 iter。
        if (names.Length > 1 || (names.Length == 1 && names[0] != "fillvalue"))
            throw PyZipLongestProtocol.Own(new PyTypeError("zip_longest() got an unexpected keyword argument"));
        object? fill = names.Length == 0 ? PyNone.Instance : values[0];
        var iterators = new IPyIterator?[args.Length];
        for (int index = 0; index < args.Length; index++) iterators[index] = PyZipLongestProtocol.Start(args[index]);
        return new PyZipLongest(iterators, fill);
    }

    public override string TypeName => "zip_longest";

    // 稳定类型不随模块导出重绑；不按输入的类型短名猜接受域。
    internal static PyNativeType Type => NativeType.Value;
    public override object? GetAttr(string name) => name switch
    {
        "__class__" => Type,
        "__iter__" or "__next__" => base.GetAttr(name),
        "__reduce__" or "__reduce_ex__" or "__setstate__" => throw PyZipLongestProtocol.Unsupported("pickle state protocol"),
        _ => throw MissingAttribute(name),
    };

    public override void SetAttr(string name, object? value) => RejectMutation(name);
    public override void DelAttr(string name) => RejectMutation(name);
    private static void RejectMutation(string name)
    {
        if (name is "__class__" or "__iter__" or "__next__" or "__reduce__" or "__reduce_ex__" or "__setstate__")
            throw PyZipLongestProtocol.Unsupported("changing iterator protocol attributes");
        throw MissingAttribute(name);
    }
    private static PyBaseException MissingAttribute(string name) => PyZipLongestProtocol.Own(
        new PyAttributeError($"'itertools.zip_longest' object has no attribute '{name}'"));

    public override bool MoveNext()
    {
        current = null;
        if (active == 0) return false;
        // CP 可在无外部引用时复用 tuple；此处不模拟 refcount，永不修改已发布的结果。
        // 局部数组也保证用户 next 重入时，内层不能覆盖外层已经取得的元素。
        var row = new object?[iterators.Length];
        try
        {
            for (int index = 0; index < iterators.Length; index++)
            {
                IPyIterator? iterator = iterators[index];
                if (iterator is null) { row[index] = fill; continue; }
                if (PyZipLongestProtocol.ReadNext(iterator, out object? value)) row[index] = value;
                else
                {
                    active--;
                    if (active == 0) { current = null; return false; }
                    iterators[index] = null;
                    row[index] = fill;
                }
            }
            current = new PyTuple(row);
            return true;
        }
        catch (PyBaseException)
        {
            // 非 Stop 的输入错误已经由协议层保现场；当前行丢弃，后续永不访问输入。
            active = 0; current = null; throw;
        }
    }

    public override object? Current() => current;
}
