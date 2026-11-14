// namedtuple的内部不可变载体：直接共享tuple存储布局，只增加真实Python类引用。
// 尚未连接Python工厂或公共类调用；不能将这些内部对象的C#测试视为公开API验收。
namespace RyuPy.Runtime;

internal sealed class PyNamedTupleInstance : PyTuple
{
    internal PyClass Class { get; }

    // 只能传入构造器独占的槽数组；元素引用保留，槽不能借用外部实参数组。
    internal PyNamedTupleInstance(PyClass cls, object?[] ownedItems) : base(ownedItems) => Class = cls;
}

public sealed partial class PyClass
{
    // tuple布局与异常BuiltinBase分离；普通tuple不增加类字段或虚方法。
    internal PyNamedTupleLayout? NamedTupleLayout { get; private set; }
}
