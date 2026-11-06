// _SpecialForm 共用的原始输入 LRU；不与 Generic、Literal 或旧归一化驻留池混用。
// 桶只按已保存 hash 定位，淘汰不重算 hash；未知回调在真正消费原始键时明确拒绝。
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

internal static class PyTypingSpecialFormCache
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
    private static readonly Dictionary<long, LinkedList<Entry>> Buckets = new();
    private static readonly LinkedList<Entry> Recent = new();
    private static readonly object Sync = new();

    internal static object Subscribe(PyTypingForm receiver, object? parameter)
    {
        // _tp_cache 捕获 TypeError 后重跑未缓存入口。未知用户回调不冒充 TypeError。
        try { return Cached(receiver, parameter); }
        catch (PyTypeError) { return PyTyping.SpecialUncached(receiver, parameter); }
    }

    private static object Cached(PyTypingForm receiver, object? parameter)
    {
        RequireKnown(parameter);
        long hash = unchecked(PyHash.Of(parameter) * 1000003 ^ RuntimeHelpers.GetHashCode(receiver));
        lock (Sync)
        {
            if (Buckets.TryGetValue(hash, out LinkedList<Entry>? bucket))
                foreach (Entry entry in bucket)
                    if (ReferenceEquals(receiver, entry.Receiver)
                        && (ReferenceEquals(entry.Parameter, parameter) || Equal(entry.Parameter, parameter)))
                    {
                        // 每项保留双向链节点，热命中不扫描整个容量。
                        LinkedListNode<Entry> node = entry.RecentNode!;
                        Recent.Remove(node); Recent.AddLast(node); return entry.Result;
                    }
            object result = PyTyping.SpecialUncached(receiver, parameter);
            // Optional 冷 miss 可递归插入 Union，外层随后才更新容量与最近顺序。
            if (Recent.Count == 128)
            {
                Entry victim = Recent.First!.Value;
                LinkedList<Entry> victims = Buckets[victim.Hash];
                victims.Remove(victim.BucketNode!);
                if (victims.Count == 0) Buckets.Remove(victim.Hash);
                Recent.RemoveFirst();
            }
            if (!Buckets.TryGetValue(hash, out bucket)) Buckets.Add(hash, bucket = new());
            var added = new Entry(receiver, parameter, hash, result);
            added.BucketNode = bucket.AddLast(added);
            added.RecentNode = Recent.AddLast(added);
            return result;
        }
    }

    // tuple 比较递归保留元素身份快判；Generic alias 的类级 eq 同样消费分类 helper。
    internal static bool Equal(object? left, object? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is PyTuple a && right is PyTuple b)
        {
            if (a.Items.Length != b.Items.Length) return false;
            for (int i = 0; i < a.Items.Length; i++) if (!Equal(a.Items[i], b.Items[i])) return false;
            return true;
        }
        if (left is PyTypingGenericAlias || right is PyTypingGenericAlias) PyTyping.GuardHelper("_GenericAlias");
        return PyHash.Equal(left, right);
    }

    // 只接受已知无 Python 回调的 hash/eq 域。tuple 与 alias 递归检查而非引用替代结构键。
    // TypeVar 的实例 extra 不覆盖隐式槽；普通 PyClass 无自定义元类，类体方法也不是元类槽。
    internal static void RequireKnown(object? value)
    {
        if (value is PyTuple tuple && tuple.GetType() == typeof(PyTuple))
        { foreach (object? item in tuple.Items) RequireKnown(item); return; }
        if (value is PyTypingAlias alias)
        { foreach (object? item in alias.Arguments) RequireKnown(item); return; }
        if (value is PyTypingGenericAlias generic)
        { foreach (object? item in generic.Arguments.Items) RequireKnown(item); return; }
        if (value is null or PyNone or bool or int or long or BigInteger or double or string
            or PyEllipsis or PyType or PyNativeType or PyClass or PyTypedDictType or PyTypingTypeVar or PyTypingForm) return;
        throw new PyNotImplementedError("typing special-form cache key hash/equality protocol is not supported");
    }
}
