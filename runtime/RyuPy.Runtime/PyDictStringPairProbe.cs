// records聚合实验基础：探针只含两个精确string引用，命中不构造tuple；尚未接入默认MIR。
// 键域证明属于独立访问器，不给普通PyDict实例增加状态或分派；拒绝时调用者保留原tuple路径。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal readonly struct PyDictStringPair(string first, string second)
{
    internal readonly string First = first;
    internal readonly string Second = second;
}

public sealed partial class PyKeyComparer : IAlternateEqualityComparer<PyDictStringPair, object>
{
    bool IAlternateEqualityComparer<PyDictStringPair, object>.Equals(PyDictStringPair probe, object key) =>
        key.GetType() == typeof(PyTuple) && key is PyTuple { Items: [string first, string second] }
        && string.Equals(first, probe.First, StringComparison.Ordinal)
        && string.Equals(second, probe.Second, StringComparison.Ordinal);

    int IAlternateEqualityComparer<PyDictStringPair, object>.GetHashCode(PyDictStringPair probe)
    {
        long hash = PyHash.TwoStringTupleHash(probe.First, probe.Second);
        return unchecked((int)(hash ^ (hash >> 32)));
    }

    object IAlternateEqualityComparer<PyDictStringPair, object>.Create(PyDictStringPair probe) =>
        new PyTuple([probe.First, probe.Second]);
}

public sealed partial class PyDict
{
    // 访问器仅供有物化映射的执行区使用。失败意味着没有执行查找/写入，不是查找缺失。
    // 不缓存值或槽位；每次真实访问仍由原_index完成，覆盖、墓碑整理和删除顺序完全共享。
    internal sealed class StringPairAccess(PyDict owner)
    {
        // 外部持续修改不能驱使每次探针全表扫描；大表失去证明后保守拒绝。
        private const int MaxDomainScanSlots = 256;
        private long _checkedVersion = -1;
        private bool _safe;

        internal bool CheckDomain()
        {
            if (!ReferenceEquals(owner._index.Comparer, PyKeyComparer.Instance)) return false;
            if (_checkedVersion == owner.Version) return _safe;
            _checkedVersion = owner.Version;
            _safe = false;
            if (owner._keys.Count > MaxDomainScanSlots) return false;
            // Version在任何结构修改后变化；精确tuple的精确字符串成员无用户回调。
            // 不使用PyHash.IsHashable作为无回调证明，tuple内的用户对象也必须拒绝。
            foreach (object? key in owner._keys)
                if (key is not null && (key.GetType() != typeof(PyTuple)
                    || key is not PyTuple { Items: [string, string] })) return false;
            _safe = true;
            return true;
        }

        internal bool TryGet(string first, string second, out bool found, out object? value)
        {
            found = false;
            value = null;
            if (!CheckDomain()) return false;
            found = owner._index.GetAlternateLookup<PyDictStringPair>()
                .TryGetValue(new(first, second), out int slot);
            if (found) value = owner._values[slot];
            return true;
        }

        internal bool TrySet(string first, string second, object? value)
        {
            if (!CheckDomain()) return false;
            if (owner._index.GetAlternateLookup<PyDictStringPair>()
                .TryGetValue(new(first, second), out int slot))
            {
                owner._values[slot] = value;
                return true;
            }
            // 只有真正插入才物化真实tuple，仍通过唯一插入入口维护版本和整理。
            owner.Set(new PyTuple([first, second]), value);
            // 既有域已证明，新键仍是精确string对，整个插入不可能调用用户代码。
            // 承接自身写入的证明（包括墓碑整理），而非下一次扫描增长后的整张表。
            // 外部写入仍使Version不相等，不能通过此处自动接受。
            _checkedVersion = owner.Version;
            return true;
        }

        // 仅实验：逻辑tuple键必须已证明完全不可观察（包括循环退出），否则不得使用。
        // 精确long默认零并加一，单次查找；溢出/非long只拒绝，不写入或调用用户代码。
        internal bool TryIncrementDiscardedKey(string first, string second)
        {
            if (!CheckDomain()) return false;
            if (owner._index.GetAlternateLookup<PyDictStringPair>()
                .TryGetValue(new(first, second), out int slot))
            {
                if (owner._values[slot] is not long current || current == long.MaxValue) return false;
                owner._values[slot] = current + 1L;
                return true;
            }
            owner.Set(new PyTuple([first, second]), 1L);
            _checkedVersion = owner.Version;
            return true;
        }
    }
}
