// overload只在实际mapping操作处检查有限键域与工厂；真实存储公开可见，不持影子列表或回滚前缀。
using System.Numerics;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

internal static partial class PyTypingOverload
{
    // 只缓存已验证的键集合版本；弱键不延长外部替换registry或其函数值的生命周期。
    private sealed class ScalarValidation { internal long Version = -1; }
    private static readonly ConditionalWeakTable<PyDict, ScalarValidation> Validated = new();
    private static readonly object DefaultDictRoot = PyCollections.CreateAttributes()["defaultdict"]!;

    private static PyDict Mapping(object? value) => value switch
    {
        PyDict dict => dict,
        PyDefaultDict dict => dict.Dictionary,
        _ => throw Unsupported("custom registry mapping"),
    };

    private static object Key(PyDict mapping, object? key)
    {
        RequireKey(key);
        ScalarValidation validation = Validated.GetValue(mapping, static _ => new ScalarValidation());
        if (validation.Version != mapping.Version)
        {
            // 外部结构写入才重新检查；即使本次查找为scalar，也不能执行未知存量键的碰撞eq。
            // 使用槽索引避免扫描枚举分配；失败不记版本，未消费层仍保持未检查。
            for (int slot = 0; slot < mapping.SlotCount; slot++)
                if (mapping.TryKeyAt(slot, out object? stored)) RequireKey(stored);
            validation.Version = mapping.Version;
        }
        return key ?? PyNone.Instance;
    }

    private static void RequireKey(object? value)
    {
        if (value is null or PyNone or string or bool or int or long or BigInteger or double) return;
        if (value is PyList or PyDict or PySet or PyByteArray or PyDefaultDict or PyCounter)
            throw new PyTypeError($"unhashable type: '{PyRepr.TypeName(value)}'");
        throw Unsupported("non-scalar registry key hash or equality");
    }

    private static bool Contains(object? mapping, object? key)
    {
        PyDict dictionary = Mapping(mapping);
        return dictionary.Contains(Key(dictionary, key));
    }

    private static object? Item(object? mapping, object? key)
    {
        PyDict dictionary = Mapping(mapping);
        object actual = Key(dictionary, key);
        // 空字典TryGet可能不调用CLR comparer；Key已在真正下标消费点验证不可哈希键。
        if (dictionary.TryGet(actual, out object? value)) return value;
        if (mapping is not PyDefaultDict defaults) throw PyKeyError.ForKey(actual);
        object? factory = defaults.GetAttr("default_factory");
        if (factory is null or PyNone) throw PyKeyError.ForKey(actual);
        GuardFactory(factory);
        value = PyOps.CallValue(factory, [], [], []);
        dictionary.Set(actual, value);
        MarkValidated(dictionary);
        return value;
    }

    private static void SetItem(object? mapping, object? key, object? value)
    {
        PyDict dictionary = Mapping(mapping);
        dictionary.Set(Key(dictionary, key), value);
        MarkValidated(dictionary);
    }

    private static void ClearRegistry(object? mapping)
    {
        PyDict dictionary = Mapping(mapping);
        // clear不hash旧键、不消费default_factory；空集合可直接登记新版本。
        dictionary.Clear();
        MarkValidated(dictionary);
    }

    private static void MarkValidated(PyDict dictionary) =>
        Validated.GetValue(dictionary, static _ => new ScalarValidation()).Version = dictionary.Version;

    private static void GuardFactory(object factory)
    {
        if (ReferenceEquals(factory, PyType.Get("dict"))) return;
        if (factory is PyPartial partial && ReferenceEquals(partial.GetAttr("func"), DefaultDictRoot)
            && partial.GetAttr("args") is PyTuple { Length: 1 } args
            && ReferenceEquals(args.Items[0], PyType.Get("dict"))
            && partial.GetAttr("keywords") is PyDict { Count: 0 }) return;
        throw Unsupported("changed registry default_factory");
    }
}
