// Generic 专用 128 项 LRU：保留单值/显式 tuple 输入形状，TypeVar 按真实身份比较。
// 缓存只接收已验证的变量，因此不会省略用户对象的 hash/eq 副作用。
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

internal static class PyTypingGenericCache
{
    private sealed class Key(bool tuple, object?[] arguments) : IEquatable<Key>
    {
        internal readonly bool Tuple = tuple;
        internal readonly object?[] Arguments = (object?[])arguments.Clone();
        public bool Equals(Key? other)
        {
            if (other is null || Tuple != other.Tuple || Arguments.Length != other.Arguments.Length) return false;
            for (int i = 0; i < Arguments.Length; i++)
                if (!ReferenceEquals(Arguments[i], other.Arguments[i])) return false;
            return true;
        }
        public override bool Equals(object? other) => other is Key key && Equals(key);
        public override int GetHashCode()
        {
            var hash = new HashCode(); hash.Add(Tuple);
            foreach (object? argument in Arguments) hash.Add(RuntimeHelpers.GetHashCode(argument!));
            return hash.ToHashCode();
        }
    }
    private sealed record Entry(Key Key, PyTypingGenericAlias Alias);
    private static readonly Dictionary<Key, LinkedListNode<Entry>> Entries = new();
    private static readonly LinkedList<Entry> Recent = new();
    private static readonly object Sync = new();

    internal static PyTypingGenericAlias Intern(bool tuple, object?[] arguments)
    {
        var key = new Key(tuple, arguments);
        lock (Sync)
        {
            if (Entries.TryGetValue(key, out LinkedListNode<Entry>? node))
            {
                Recent.Remove(node); Recent.AddLast(node); return node.Value.Alias;
            }
            // CP 的缓存命中不重读可写 Generic 全局；仅 miss 才实际消费它。
            PyTypingGeneric.GuardExport(PyTyping.CreateAttributes());
            var alias = new PyTypingGenericAlias(arguments);
            if (Entries.Count == 128)
            {
                Entries.Remove(Recent.First!.Value.Key); Recent.RemoveFirst();
            }
            node = Recent.AddLast(new Entry(key, alias)); Entries.Add(key, node);
            return alias;
        }
    }
}
