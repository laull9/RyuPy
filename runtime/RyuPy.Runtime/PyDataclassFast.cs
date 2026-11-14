using System;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>
/// 数据类的无副作用构造计划：保留 PyClass / PyInstance 身份，仅省去通用绑定与逐字段名字查找。
/// 计划绑定生成的 __init__ 对象；任何类修改后重新检查描述符和方法身份；生成时含 post_init 调用的类不登记计划。
/// </summary>
public sealed partial class PyClass
{
    private PyFunction? _dataclassGeneratedInit;
    private int[]? _dataclassConstructorSlots;
    private long _dataclassConstructorVersion = -1;
    private bool _dataclassConstructorSafe;

    // 首批只接受全部参与初始化且无工厂的字段。复杂签名与副作用继续走原实现。
    internal void RegisterDataclassConstructor(PyFunction generatedInit, PyDataclassField[] fields, bool hasPostInit)
    {
        if (HasSlotLayout || hasPostInit) return;
        foreach (var field in fields)
            if (!field.Init || !ReferenceEquals(field.Factory, PyDataclasses.Missing)) return;
        var slots = new int[fields.Length];
        for (int index = 0; index < fields.Length; index++)
            slots[index] = GetOrAddInstanceSlot(fields[index].Name!);
        _dataclassGeneratedInit = generatedInit;
        _dataclassConstructorSlots = slots;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryConstructDataclass(object?[] args, string[] names, out PyInstance? instance)
    {
        instance = null;
        int[]? slots = _dataclassConstructorSlots;
        // 关键字、缺参、额外实参的错误必须由通用 binder 产生，不能改变错误消息或顺序。
        if (slots is null || names.Length != 0 || args.Length != slots.Length) return false;
        if (_dataclassConstructorVersion != GlobalVersion) RefreshDataclassConstructor();
        if (!_dataclassConstructorSafe) return false;
        instance = new PyInstance(this);
        for (int index = 0; index < slots.Length; index++) instance.SetSlot(slots[index], args[index]);
        return true;
    }

    private void RefreshDataclassConstructor()
    {
        _dataclassConstructorSafe = DataclassActive && !HasProperties
            && TryLookup("__init__", out _, out object? init)
            && ReferenceEquals(init, _dataclassGeneratedInit);
        _dataclassConstructorVersion = GlobalVersion;
    }
}
