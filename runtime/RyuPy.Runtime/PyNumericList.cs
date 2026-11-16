using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>仅编译器新建的数值列表启用16B槽；普通列表维持object槽，公开Items永久退化。</summary>
public sealed partial class PyList
{
    // 普通热路直接访问typed背板，代价是每个包装对象多一个引用字段。
    // 两个字段不能同时作为可变有效背板；物化完整成功后清除数值背板。
    private List<object?>? _items;
    private List<PyNumericSlot>? _numeric;

    private PyList(bool numeric) => _numeric = new List<PyNumericSlot>();
    internal static PyList CreateNumeric() => new(true);

    // 仅GetSlice归一化后的真子集进入；每个被选槽在源中缓存原盒，结果为独立object背板。
    // 近全/累计观察仍可保留16B槽及盒；不以比例阈值改变表示，完整Copy/Items仍统一物化。
    private PyList GetNumericSlice(long start, long step, long count)
    {
        var result = new PyList((int)count);
        long index = start;
        for (long i = 0; i < count; i++, index += step)
            result._items!.Add(GetItem(index));
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private List<object?> MaterializeAll()
    {
        if (_booleans is { } booleans)
        {
            var boolResult = new List<object?>(booleans.Count);
            for (int index = 0; index < booleans.Count; index++)
            {
                boolResult.Add(PyOps.BoxBool(booleans[index]));
            }
            _items = boolResult;
            _booleans = null;
            return boolResult;
        }
        var slots = _numeric!;
        var result = new List<object?>(slots.Count);
        for (int index = 0; index < slots.Count; index++)
        {
            PyNumericSlot slot = slots[index];
            result.Add(slot.Materialize());
            slots[index] = slot;
        }
        _items = result;
        _numeric = null;
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private object? ReadNumeric(long index)
    {
        int position = (int)Normalize(index);
        var slots = _numeric!;
        PyNumericSlot slot = slots[position];
        object value = slot.Materialize();
        slots[position] = slot;
        return value;
    }

    // 范围已由Pop校验；先物化再删除，失败时不改变列表，未观察槽不装箱。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private object PopNumeric(int position)
    {
        var slots = _numeric!;
        PyNumericSlot slot = slots[position];
        object value = slot.Materialize();
        slots.RemoveAt(position);
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object ListIndexError() => throw new PyIndexError("list index out of range");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AppendNumeric(object? value)
    {
        if (value is double number)
        {
            // 已存在的盒必须保存，不能把两次append(x)改成各自新fresh值。
            _numeric!.Add(PyNumericSlot.Existing(value, number));
            return;
        }
        MaterializeAll().Add(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AppendFreshF64(double value)
    {
        if (_numeric is { } slots) slots.Add(PyNumericSlot.Fresh(value));
        else if (_booleans is not null) MaterializeAll().Add(value);
        else _items!.Add(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryNumericPosition(long index, out int position)
    {
        int count = Count;
        long normalized = index < 0 ? index + count : index;
        if ((ulong)normalized >= (ulong)count) { position = 0; return false; }
        position = (int)normalized;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PyList? PeekChildList(int position) => _items is { } items ? items[position] as PyList : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryReadF64(int position, out double value)
    {
        if (_numeric is { } slots) { value = slots[position].Number; return true; }
        if (_items is { } items && items[position] is double number) { value = number; return true; }
        value = 0;
        return false;
    }

    // 仅供编译器must分析证明的同一list/index守卫真边调用；中间不得写入或回调。
    // 不重复显式范围条件，仍保留List索引器本身的托管边界检查。
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal double ReadCheckedNumeric(long index)
    {
        long position = index < 0 ? index + Count : index;
        if (_numeric is { } slots) return slots[(int)position].Number;
        return (double)_items![(int)position]!;
    }
}
