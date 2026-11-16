// OrderedDict 专属流式更新：mapping 每个 next/get/commit 交错，pairs 仅 N+1 解包；保原异常与前缀。
namespace RyuPy.Runtime;

public static partial class PyOps
{
    internal static object OrderedDictUpdate(PyOrderedDict target, object?[] args, string[] names,
        object?[] values, bool initialize = false)
    {
        if (args.Length > 1) throw PyOrderedDict.Error(new PyTypeError(initialize
            ? $"expected at most 1 arguments, got {args.Length}"
            : $"update() takes at most 1 positional argument ({args.Length} given)"));
        if (args.Length == 1) OrderedDictSource(target, args[0]);
        for (int i = 0; i < names.Length; i++) target.Set(names[i], values[i]);
        return PyNone.Instance;
    }
    private static void OrderedDictSource(PyOrderedDict target, object? source)
    {
        if (source is PyInstanceIterator adapter) source = adapter.Instance;
        if (source is PyOrderedDict ordered)
        {
            foreach (var (key, value) in ordered.Entries()) target.Set(key, value);
            return;
        }
        // 精确 dict 是无回调快路；派生 mapping 的 keys/getitem 仍公开调用，不能剥 raw。
        if (source is PyDict exact)
        {
            foreach (var (key, value) in exact.Entries()) target.Set(key, value);
            return;
        }
        bool sequence = source is PyList or PyGenerator || source is PyTuple && source.GetType() == typeof(PyTuple);
        if (!sequence && DictUpdateKeys(source, out object? method))
        {
            IPyIterator keys = ListExtendIterator(DictUpdateMappingCall(method, null, true));
            while (ListExtendNext(keys, out object? key))
            {
                object? value = DictUpdateMappingCall(source, key, false);
                target.Set(key, value);
            }
            return;
        }
        IPyIterator pairs = ListExtendIterator(source);
        while (ListExtendNext(pairs, out object? pair))
        {
            // 不求 len/hint、不物化全部 pair；第三项一出现立即失败，不读第四项。
            IPyIterator inner = ListExtendIterator(pair);
            if (!ListExtendNext(inner, out object? key))
                throw PyOrderedDict.Error(new PyValueError("need more than 0 values to unpack"));
            if (!ListExtendNext(inner, out object? value))
                throw PyOrderedDict.Error(new PyValueError("need more than 1 value to unpack"));
            if (ListExtendNext(inner, out _))
                throw PyOrderedDict.Error(new PyValueError("too many values to unpack (expected 2)"));
            target.Set(key, value);
        }
    }
    internal static PyOrderedDict OrderedDictFromKeys(object? source, object? value)
    {
        var result = new PyOrderedDict();
        IPyIterator iterator = ListExtendIterator(source);
        while (ListExtendNext(iterator, out object? key)) result.Set(key, value);
        return result;
    }
}
