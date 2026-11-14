using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RyuPy.Runtime;

/// <summary>
/// 布尔紧凑存储支持：仅由 Count==1 且元素为 exact bool 的正次重复创建。
/// 元素直存 List&lt;bool&gt;；异构写或公开 Items 访问时物化为标准 object 背板。
/// </summary>
public sealed partial class PyList
{
    private List<bool>? _booleans;

    private PyList(List<bool> booleans) => _booleans = booleans;

    internal static bool TryRepeatBoolean(PyList source, long times, [NotNullWhen(true)] out PyList? result)
    {
        if (times <= 0 || times > Array.MaxLength || source.Count != 1)
        {
            result = null;
            return false;
        }

        bool value;
        if (source._booleans is { } booleans)
        {
            value = booleans[0];
        }
        else if (source._items is { } items)
        {
            if (items[0] is bool flag)
            {
                value = flag;
            }
            else
            {
                result = null;
                return false;
            }
        }
        else
        {
            result = null;
            return false;
        }

        int count = (int)times;
        var list = new List<bool>(count);
        CollectionsMarshal.SetCount(list, count);
        CollectionsMarshal.AsSpan(list).Fill(value);
        result = new PyList(list);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object? ReadIteratorItem(int index)
    {
        if (_booleans is { } booleans)
        {
            return PyOps.BoxBool(booleans[index]);
        }
        return Items[index];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private object? GetItemSlow(long index)
    {
        if (_booleans is { } booleans)
        {
            int count = booleans.Count;
            long position = index < 0 ? index + count : index;
            if ((ulong)position >= (ulong)count) return ListIndexError();
            return PyOps.BoxBool(booleans[(int)position]);
        }
        return ReadNumeric(index);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SetBooleanItem(long index, object? value)
    {
        int position = (int)Normalize(index);
        if (value is bool flag)
        {
            _booleans![position] = flag;
            return;
        }
        MaterializeAll()[(int)position] = value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AppendNonItems(object? value)
    {
        if (_booleans is { } booleans)
        {
            if (value is bool flag) booleans.Add(flag);
            else MaterializeAll().Add(value);
            return;
        }
        AppendNumeric(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object PopBoolean(List<bool> booleans, int position)
    {
        bool val = booleans[position];
        booleans.RemoveAt(position);
        return PyOps.BoxBool(val);
    }
}
