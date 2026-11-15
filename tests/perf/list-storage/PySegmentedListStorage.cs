// 列表引用背板的隔离候选：分段追加避免搬运历史引用；尚不接入 PyList 默认路径。
// 已有盒始终保留身份，公开可变 List 后永久物化；容量、段大小不构成 Python 语义。
using System;
using System.Collections;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal sealed class PySegmentedListStorage : IReadOnlyList<object?>
{
    internal const int SegmentSize = 1024;
    private readonly List<object?[]> _segments = new();
    private List<object?>? _materialized;
    private int _count;

    public int Count => _materialized?.Count ?? _count;
    internal bool IsSegmented => _materialized is null;

    public object? this[int index]
    {
        get
        {
            CheckIndex(index);
            return _materialized is { } items ? items[index]
                : _segments[index / SegmentSize][index % SegmentSize];
        }
        set
        {
            CheckIndex(index);
            if (_materialized is { } items) items[index] = value;
            else _segments[index / SegmentSize][index % SegmentSize] = value;
        }
    }

    internal object? GetItem(long index) => this[Normalize(index)];
    internal void SetItem(long index, object? value) => this[Normalize(index)] = value;

    internal void Append(object? value)
    {
        if (_materialized is { } items) { items.Add(value); return; }
        int next = checked(_count + 1);
        if (_count % SegmentSize == 0) _segments.Add(new object?[SegmentSize]);
        _segments[_count / SegmentSize][_count % SegmentSize] = value;
        _count = next;
    }

    // 固定入口长度支持 self-extend；各段复制保留引用，不造全量临时数组。
    internal void ExtendExact(PySegmentedListStorage source)
    {
        int remaining = source.Count;
        int offset = 0;
        while (remaining > 0)
        {
            if (_materialized is not null || source._materialized is not null)
            {
                for (; remaining > 0; remaining--, offset++) Append(source[offset]);
                return;
            }
            int next = checked(_count + Math.Min(remaining, SegmentSize - _count % SegmentSize));
            if (_count % SegmentSize == 0) _segments.Add(new object?[SegmentSize]);
            int length = Math.Min(next - _count, SegmentSize - offset % SegmentSize);
            Array.Copy(source._segments[offset / SegmentSize], offset % SegmentSize,
                _segments[_count / SegmentSize], _count % SegmentSize, length);
            _count += length;
            offset += length;
            remaining -= length;
        }
    }

    // 只建模枚举器消费的逐项提交；Python iter/hint/异常协议仍由原 PyListExtendProtocol 负责。
    internal void Extend(IEnumerable<object?> source)
    {
        if (ReferenceEquals(source, this)) { ExtendExact(this); return; }
        foreach (object? value in source) Append(value);
    }

    internal object? Pop(long index = -1)
    {
        int position = Normalize(index);
        if (_materialized is { } items)
        {
            object? result = items[position];
            items.RemoveAt(position);
            return result;
        }
        if (position != _count - 1)
        {
            var materialized = Materialize();
            object? result = materialized[position];
            materialized.RemoveAt(position);
            return result;
        }
        object? value = this[position];
        _segments[position / SegmentSize][position % SegmentSize] = null;
        _count--;
        if (_count % SegmentSize == 0) _segments.RemoveAt(_segments.Count - 1);
        return value;
    }

    internal PySegmentedListStorage Slice(PySlice slice)
    {
        var (start, _, step, count) = slice.Indices(Count);
        if (step == 1) return SliceRange((int)start, (int)count);
        var result = new PySegmentedListStorage();
        long index = start;
        for (long i = 0; i < count; i++, index += step) result.Append(this[(int)index]);
        return result;
    }

    // 归一化后的连续切片：只复制引用，按源/目标段界拆批次，不逐元素调用索引器和Append。
    // 与List.GetRange一致验证位置及长度；此入口本身不执行Python索引协议。
    internal PySegmentedListStorage SliceRange(int start, int count)
    {
        if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (start > Count - count) throw new ArgumentException("切片超出列表范围");
        var result = new PySegmentedListStorage();
        int copied = 0;
        while (copied < count)
        {
            var destination = new object?[SegmentSize];
            int length = Math.Min(SegmentSize, count - copied);
            if (_materialized is { } items)
            {
                items.CopyTo(start + copied, destination, 0, length);
            }
            else
            {
                int written = 0;
                while (written < length)
                {
                    int sourceIndex = start + copied + written;
                    int part = Math.Min(length - written, SegmentSize - sourceIndex % SegmentSize);
                    Array.Copy(_segments[sourceIndex / SegmentSize], sourceIndex % SegmentSize,
                        destination, written, part);
                    written += part;
                }
            }
            result._segments.Add(destination);
            copied += length;
        }
        result._count = count;
        return result;
    }

    internal void Clear()
    {
        if (_materialized is { } items) items.Clear();
        else { _segments.Clear(); _count = 0; }
    }

    // 先完整构建再发布，失败时旧背板仍有效。导出后外部修改长度/引用必须即时可见。
    internal List<object?> Materialize()
    {
        if (_materialized is { } existing) return existing;
        var result = new List<object?>(_count);
        int remaining = _count;
        foreach (object?[] segment in _segments)
        {
            int length = Math.Min(remaining, SegmentSize);
            for (int i = 0; i < length; i++) result.Add(segment[i]);
            remaining -= length;
        }
        _materialized = result;
        _segments.Clear();
        _count = 0;
        return result;
    }

    private int Normalize(long index)
    {
        long position = index < 0 ? index + Count : index;
        if ((ulong)position >= (ulong)Count) throw new PyIndexError("list index out of range");
        return (int)position;
    }

    private void CheckIndex(int index)
    {
        if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
    }

    // 与 Python list 活游标相同：每次恢复读取当前长度；已耗尽的游标不复活。
    public IEnumerator<object?> GetEnumerator()
    {
        for (int index = 0; index < Count; index++) yield return this[index];
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
