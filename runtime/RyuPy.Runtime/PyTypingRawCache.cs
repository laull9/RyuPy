// 可独立实例化的 typing 原始输入 LRU；每个实例独占 128 项，不复用旧归一化驻留池。
// 只共用 F0 已有限验证的键协议，不改变 Optional/Union 的现有算法与静态池。
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

internal sealed class PyTypingRawCache(Func<PyTypingForm, object?, object> uncached)
{
    private sealed class Entry(PyTypingForm receiver, object? parameter, long hash, object result)
    {
        internal readonly PyTypingForm Receiver = receiver;
        internal readonly object? Parameter = parameter;
        internal readonly long Hash = hash;
        internal readonly object Result = result;
        internal LinkedListNode<Entry>? BucketNode;
        internal LinkedListNode<Entry>? RecentNode;
    }
    private readonly Dictionary<long, LinkedList<Entry>> _buckets = new();
    private readonly LinkedList<Entry> _recent = new();
    private readonly object _sync = new();

    internal object Subscribe(PyTypingForm receiver, object? parameter)
    {
        // _tp_cache 包含 hash、eq 和冷正文的 TypeError；重试不再查表、hash 或写入。
        // NI/ValueError 等必须原样传播，不能用未知协议冒充无副作用的 TypeError。
        try { return Cached(receiver, parameter); }
        catch (PyTypeError) { return uncached(receiver, parameter); }
    }

    private object Cached(PyTypingForm receiver, object? parameter)
    {
        PyTypingSpecialFormCache.RequireKnown(parameter);
        long hash = unchecked(PyHash.Of(parameter) * 1000003 ^ RuntimeHelpers.GetHashCode(receiver));
        lock (_sync)
        {
            if (_buckets.TryGetValue(hash, out LinkedList<Entry>? bucket))
                foreach (Entry entry in bucket)
                    if (ReferenceEquals(receiver, entry.Receiver)
                        && (ReferenceEquals(entry.Parameter, parameter)
                            || PyTypingSpecialFormCache.Equal(entry.Parameter, parameter)))
                    {
                        // 只查同 hash 桶；双向节点使命中触碰与淘汰不扫描整个容量。
                        LinkedListNode<Entry> node = entry.RecentNode!;
                        _recent.Remove(node); _recent.AddLast(node); return entry.Result;
                    }
            // 真 miss 才消费类型参数及全局 helper。失败不驱逐旧项，也不留下成功旁表。
            object result = uncached(receiver, parameter);
            if (_recent.Count == 128)
            {
                Entry victim = _recent.First!.Value;
                LinkedList<Entry> victims = _buckets[victim.Hash];
                victims.Remove(victim.BucketNode!);
                if (victims.Count == 0) _buckets.Remove(victim.Hash);
                _recent.RemoveFirst();
            }
            // 以条目保存的 hash 解除引用，淘汰绝不重新调用旧键的 hash/eq。
            if (!_buckets.TryGetValue(hash, out bucket)) _buckets.Add(hash, bucket = new());
            var added = new Entry(receiver, parameter, hash, result);
            added.BucketNode = bucket.AddLast(added);
            added.RecentNode = _recent.AddLast(added);
            return result;
        }
    }
}
