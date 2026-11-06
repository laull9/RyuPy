// 精确 dict 的有界更新协议：位置源逐项提交，mapping 先取完 keys，关键字最后写入。
// 复用 list 的局部迭代/提示协议但不物化外层 pairs；不改 dict 存储或全局迭代 ABI。
using System.Numerics;

namespace RyuPy.Runtime;

public static partial class PyOps
{
    internal static object CallDictUpdate(PyDict target, object?[] args, string[] names,
        object?[] values, bool construct = false)
    {
        if (args.Length > 1)
            throw DictUpdateError(new PyTypeError($"{(construct ? "dict" : "update")} expected at most 1 argument, got {args.Length}"));
        if (args.Length == 1) target.Update(args[0]);
        // 新 update keyword 入口也不得让目标已有自定义键触发尚未闭合的 eq 重入。
        // dict 构造原有 keyword 路保持历史能力；无 keyword 时不额外扫描目标。
        if (!construct && names.Length > 0) new DictUpdateTargetGuard(target).Check();
        for (int i = 0; i < names.Length; i++) target.Set(names[i], values[i]);
        return PyNone.Instance;
    }

    internal static void DictUpdateSource(PyDict target, object? source)
    {
        // 已有 proxy 的窄域及错误合同保持独立；不能借此次扩展扩大它的自定义键支持。
        if (source is PyMappingProxy proxy) { PyMappingProxyConsumers.CopyInto(target, proxy); return; }
        PyDict? raw = source as PyDict ?? (source as IPyNativeMapping)?.Dictionary;
        if (raw is not null)
        {
            if (ReferenceEquals(raw, target)) return;
            // 保留历史原生复制路径；存储未暴露已有 hash，因此不宣称自定义键复制/重入已对齐。
            foreach (var (key, value) in raw.Entries()) target.Set(key, value);
            return;
        }
        // 默认 iter 槽的真实派生源与 CP 一样走 raw，不调用业务 keys 覆盖。
        if (source is PyDictInstance wrapper)
        {
            wrapper.Class.RequireDictReady();
            DictLayoutUpdateSource(target, wrapper);
            return;
        }
        // iter(user) 只是运行时适配器，Python 中仍是原对象，不能隐藏它的 keys。
        if (source is PyInstanceIterator adapter) source = adapter.Instance;
        // 精确外层按活下标走，不为每一项读写 traceback/ambient；inner 回调仍完整保护。
        if (source is PyList list)
        {
            var guard = new DictUpdateTargetGuard(target);
            for (int i = 0; i < list.Count; i++) DictUpdatePair(target, list.GetItem(i), i, ref guard);
            return;
        }
        if (source is PyTuple tuple && source.GetType() == typeof(PyTuple))
        {
            var guard = new DictUpdateTargetGuard(target);
            for (int i = 0; i < tuple.Length; i++) DictUpdatePair(target, tuple.Items[i], i, ref guard);
            return;
        }
        // sealed generator 没有实例属性写槽，不能设置 keys；真实派生对象仍做普通属性选择。
        if (source is not PyGenerator && DictUpdateKeys(source, out object? keysMethod))
        {
            DictUpdateMapping(target, source, keysMethod);
            return;
        }
        IPyIterator iterator = ListExtendIterator(source);
        var pairGuard = new DictUpdateTargetGuard(target);
        long index = 0;
        while (ListExtendNext(iterator, out object? item)) DictUpdatePair(target, item, index++, ref pairGuard);
    }

    // 新布局入口单独约束 raw 源，不把 scalar 白名单倒灌到既有 exact/native 更新。
    internal static void DictLayoutUpdateSource(PyDict target, object? source)
    {
        PyDict? raw = source as PyDict ?? (source as IPyNativeMapping)?.Dictionary;
        if (source is PyDictInstance wrapper)
        {
            wrapper.Class.RequireDictReady();
            raw = wrapper.Payload;
        }
        if (raw is not null)
        {
            PyDictProtocols.RequireScalarContents(raw);
            if (ReferenceEquals(raw, target)) return;
            PyDictProtocols.RequireScalarContents(target);
            foreach (var (key, value) in raw.Entries()) target.Set(key, value);
            return;
        }
        // generic 路径已有逐项键/目标守卫与流式前缀；proxy 也有自己的有限守卫。
        DictUpdateSource(target, source);
    }

    // 所有外源共用完整 inner 物化、错误索引、键域与立即提交逻辑，避免快路悄悄改变失败前缀。
    private static void DictUpdatePair(PyDict target, object? item, long index, ref DictUpdateTargetGuard guard)
    {
        object pair;
        try { pair = DictUpdateSequence(item); }
        catch (PyTypeError)
        {
            // CP 用新 TypeError 替换整个 inner 物化的 TypeError，不把被替换异常挂成 context。
            throw DictUpdateError(new PyTypeError($"cannot convert dictionary update sequence element #{index} to a sequence"));
        }
        int count = DictUpdateCount(pair);
        if (count != 2)
            throw DictUpdateError(new PyValueError($"dictionary update sequence element #{index} has length {count}; 2 is required"));
        object? key = DictUpdateAt(pair, 0), value = DictUpdateAt(pair, 1);
        DictUpdateRequireKey(key);
        guard.Check();
        target.Set(key, value);
        guard.Written();
    }

    // PySequence_Fast 的精确 list/tuple 路径保 alias；generic 先 iter，再对游标 list.extend。
    // 第二次 iter 及游标长度提示可观察，必须完整消费后才检查 pair 长度。
    private static object DictUpdateSequence(object? source)
    {
        if (source is PyList || source is PyTuple && source.GetType() == typeof(PyTuple)) return source;
        IPyIterator iterator = ListExtendIterator(source is PyInstanceIterator adapter ? adapter.Instance : source);
        var materialized = new PyList();
        ExtendList(materialized, iterator);
        return materialized;
    }
    private static int DictUpdateCount(object sequence) => sequence is PyList list ? list.Count : ((PyTuple)sequence).Length;
    private static object? DictUpdateAt(object sequence, int index) => sequence is PyList list
        ? list.GetItem(index) : ((PyTuple)sequence).Items[index];

    // mapping 冷路径独立，raw exact/native 的提前返回不创建闭包或用户调用包装。
    private static void DictUpdateMapping(PyDict target, object? source, object? keysMethod)
    {
        object sequence = DictUpdateSequence(DictUpdateMappingCall(keysMethod, null, invoke: true));
        var guard = new DictUpdateTargetGuard(target);
        for (int i = 0; i < DictUpdateCount(sequence); i++)
        {
            object? key = DictUpdateAt(sequence, i);
            object? value = DictUpdateMappingCall(source, key, invoke: false);
            // mapping 先取值再验证键；getitem 的用户异常不能被提前的 unhashable/NI 遮住。
            DictUpdateRequireKey(key);
            guard.Check();
            target.Set(key, value);
            guard.Written();
        }
    }

    private static bool DictUpdateKeys(object? source, out object? method)
    {
        long depth = PyTraceback.CaptureDepth();
        object? context = PyErrors.EnterHandler(null);
        try { method = PyObject.GetAttr(source, "keys"); return true; }
        catch (PyAttributeError) { method = null; return false; }
        catch (PyBaseException error) { PyTraceback.Attach(error); throw; }
        finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
    }
    private static object? DictUpdateMappingCall(object? source, object? key, bool invoke)
    {
        long depth = PyTraceback.CaptureDepth();
        object? context = PyErrors.EnterHandler(null);
        try { return invoke ? CallValue(source, [], [], []) : GetItem(source, key); }
        catch (PyBaseException error) { PyTraceback.Attach(error); throw; }
        finally { PyTraceback.PopTo(depth + 1); PyErrors.LeaveHandler(context); }
    }

    // 新协议域只准精确标量；自定义键可回调修改 CLR 哈希表，本轮不以全表重写掩盖它。
    private static void DictUpdateRequireKey(object? key)
    {
        if (key is null or PyNone or string or long or int or bool or BigInteger or double) return;
        if (!PyHash.IsHashable(key))
            throw DictUpdateError(new PyTypeError($"unhashable type: '{PyRepr.TypeName(key)}'"));
        throw DictUpdateError(new PyNotImplementedError("dict update protocol with custom keys is not supported"));
    }
    // 仅栈上局部守卫，pair helper 按 ref 写回版本；不装箱、不逃逸或保存在目标上。
    private struct DictUpdateTargetGuard(PyDict target)
    {
        private long version = -1;
        internal void Check()
        {
            if (version == target.Version) return;
            // 已有内部槽访问不执行用户协议，也不分配 Keys 的 yield 枚举器。
            for (int slot = 0; slot < target.SlotCount; slot++)
                if (target.TryKeyAt(slot, out object? key)) DictUpdateRequireKey(key);
            version = target.Version;
        }
        internal void Written() => version = target.Version;
    }
    private static PyBaseException DictUpdateError(PyBaseException error) => (PyBaseException)PyErrors.PrepareRaise(error);
}
