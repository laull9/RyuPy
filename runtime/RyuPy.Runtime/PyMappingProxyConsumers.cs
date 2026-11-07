using System;
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>
/// mappingproxy 新增消费者的有限冷路径：复制按 keys/下标，Counter 按 items，关键字按合并顺序。
/// 不扩大普通 Mapping 准入，不改变无 proxy/OrderedDict 路径；自定义键的 hash/eq 次数尚未闭合，消费时明确拒绝。
/// </summary>
internal static class PyMappingProxyConsumers
{
    internal static bool NeedsKeywordMerge(object?[] names, object?[] values)
    {
        for (int i = 0; i < names.Length; i++)
            if ((names[i] is null or PyNone) && values[i] is PyMappingProxy or PyOrderedDict) return true;
        return false;
    }

    // 白名单只含精确运行时标量；不能通过用户 repr/转换/哈希判断是否安全。
    private static void RequireKey(object? key)
    {
        if (key is null or PyNone or string or long or int or bool or BigInteger or double) return;
        throw PyMappingProxy.Error(new PyNotImplementedError(
            "mappingproxy consumers with custom keys are not supported"));
    }

    /// <summary>目标回调可能增添自定义键；每次访问前按版本复核，不对已有键调用 hash/eq。</summary>
    private sealed class TargetGuard(PyDict target)
    {
        private long checkedVersion = -1;
        internal void Check()
        {
            if (checkedVersion == target.Version) return;
            foreach (object? key in target.Keys) RequireKey(key);
            checkedVersion = target.Version;
        }
        internal void Written() => checkedVersion = target.Version;
    }

    /// <summary>只物化 keys，不预取 values。exact list 保持原引用，以看到下标回调中的修改。</summary>
    private static PyList Keys(PyMappingProxy source)
    {
        object? keys = PyMappingProxyProtocol.Method(source.Mapping, "keys");
        if (keys is PyList list) return list;
        var materialized = new PyList();
        IPyIterator iterator = PyMappingProxyIteration.Start(keys);
        while (PyMappingProxyIteration.ReadNext(iterator, out object? key)) materialized.Items.Add(key);
        return materialized;
    }

    internal static void CopyInto(PyDict target, PyMappingProxy source)
    {
        PyList keys = Keys(source);
        var guard = new TargetGuard(target);
        for (int index = 0; index < keys.Count; index++)
        {
            object? key = keys.GetItem(index);
            RequireKey(key);
            object? value = PyMappingProxyProtocol.Item(source.Mapping, key);
            guard.Check();
            target.Set(key, value);
            guard.Written();
        }
    }

    /// <summary>空 Counter.update 与 dict 一样复制；其他情况逐个 items pair 累加，不转换整个来源。</summary>
    internal static void Accumulate(PyDict target, PyMappingProxy source, bool subtract)
    {
        if (!subtract && target.Count == 0) { CopyInto(target, source); return; }
        object? items = PyMappingProxyProtocol.Method(source.Mapping, "items");
        IPyIterator iterator = PyMappingProxyIteration.Start(items);
        var guard = new TargetGuard(target);
        while (PyMappingProxyIteration.ReadNext(iterator, out object? item))
        {
            // 共用已验收的二项解包：exact tuple 不分配，通用游标只读取 N+1 项。
            var pair = (PyTuple)PyOps.UnpackSequence(item, 2);
            object? key = pair.Items[0], value = pair.Items[1];
            RequireKey(key);
            guard.Check();
            object? old = target.GetOrDefault(key ?? PyNone.Instance, 0L);
            object? result = subtract ? PyOps.Binary(PyOps.Sub, old!, value!)
                : PyOps.Binary(PyOps.Add, value!, old!);
            // 数值运算也可回调用户代码，不能沿用运算前的已验版本。
            guard.Check();
            target.Set(key, result);
            guard.Written();
        }
    }

    // Counter 的空 update 沿 dict 根复制；非空 update/subtract 按公开 items。
    // 目标键域在用户 items/数值回调之后复核，不借新来源开放旧 custom-key 重入域。
    internal static void AccumulateSubclass(PyDict target, PyDictInstance source, bool subtract)
    {
        if (!subtract && target.Count == 0)
        {
            PyDictProtocols.CallUpdate(target, [source], [], []);
            return;
        }
        object? items = PyObject.InvokeMethod(source, "items", [], [], []);
        IPyIterator iterator = PyMappingProxyIteration.Start(items);
        var guard = new TargetGuard(target);
        while (PyMappingProxyIteration.ReadNext(iterator, out object? item))
        {
            var pair = (PyTuple)PyOps.UnpackSequence(item, 2);
            object? key = pair.Items[0], value = pair.Items[1];
            RequireKey(key); guard.Check();
            object? old = target.GetOrDefault(key ?? PyNone.Instance, 0L);
            object? result = subtract ? PyOps.Binary(PyOps.Sub, old!, value!)
                : PyOps.Binary(PyOps.Add, value!, old!);
            guard.Check(); target.Set(key, result); guard.Written();
        }
    }

    /// <summary>proxy 或 OrderedDict 参与时合并所有关键字组；保 object 键到末尾，再检查字符串要求。</summary>
    internal static (string[] Names, object?[] Values) Keywords(object?[] names, object?[] values)
    {
        var merged = new PyDict();
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i] is not (null or PyNone))
            {
                // 编译器提供字面字符串名称；保旧显式名称的内部转换契约。
                object key = names[i] as string ?? PyRepr.Str(names[i]);
                Add(merged, key, values[i]);
                continue;
            }
            if (values[i] is PyMappingProxy proxy)
            {
                PyList keys = Keys(proxy);
                for (int index = 0; index < keys.Count; index++)
                {
                    object? sourceKey = keys.GetItem(index);
                    object key = CheckAbsent(merged, sourceKey);
                    // 重复键必须在读取该 value 之前失败，底层仍接收原键对象。
                    object? value = PyMappingProxyProtocol.Item(proxy.Mapping, sourceKey);
                    merged.Set(key, value);
                }
                continue;
            }
            if (values[i] is PyOrderedDict ordered)
            {
                foreach (var (key, value) in ordered.Entries()) Add(merged, key, value);
                continue;
            }
            PyDict? dictionary = values[i] as PyDict ?? (values[i] as PyDictInstance)?.Payload
                ?? (values[i] as IPyNativeMapping)?.Dictionary;
            if (dictionary is null)
                throw PyMappingProxy.Error(new PyTypeError(
                    $"argument after ** must be a mapping, not '{PyRepr.TypeName(values[i])}'"));
            foreach (var (key, value) in dictionary.Entries()) Add(merged, key, value);
        }
        var resultNames = new List<string>(merged.Count);
        var resultValues = new List<object?>(merged.Count);
        foreach (var (key, value) in merged.Entries())
        {
            if (key is not string text)
                throw PyMappingProxy.Error(new PyTypeError("keywords must be strings"));
            resultNames.Add(text); resultValues.Add(value);
        }
        return ([.. resultNames], [.. resultValues]);
    }

    private static object CheckAbsent(PyDict target, object? sourceKey)
    {
        RequireKey(sourceKey);
        // Set 把 null 存成 PyNone，Contains(null) 则不会归一；局部合并必须一致。
        object key = sourceKey ?? PyNone.Instance;
        if (target.Contains(key))
            throw PyMappingProxy.Error(new PyTypeError(
                $"got multiple values for keyword argument '{PyRepr.Str(key)}'"));
        return key;
    }
    private static void Add(PyDict target, object? key, object? value) =>
        target.Set(CheckAbsent(target, key), value);
}
