// 普通函数直接 locals() 的有限运行时协议：缓存由生成函数持有，只在调用点逐槽同步。
// 不建立解释器帧，不回写 fast locals；映射保原身份、额外键及逐槽可观察副作用。
using System.Collections.Generic;

namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>完整展开调用实参后严格校验；无参直调可由编译器省略本步。</summary>
    public static void FunctionLocalsValidate(object?[] rawArgs, object?[] rawNames, object?[] rawValues)
    {
        try
        {
            // 唯一裸star在kwargs合并之后消费；混合位置star/多段**由MIR明确诊断。
            var (keywords, nonString) = CheckLocalsKeywords(rawNames, rawValues);
            long positional = 0;
            foreach (object? raw in rawArgs)
            {
                if (raw is not StarredArg starred) { positional++; continue; }
                object? source = starred.Value;
                // 精确非iterable值可确定调用器诊断；协议iterator则明确NI。
                if (source is null or PyNone or bool or int or long or double or System.Numerics.BigInteger)
                    throw new PyTypeError($"locals() argument after * must be an iterable, not {PyRepr.TypeName(source)}");
                if (source is not (PyList or PyBytes or PyByteArray or PyRange or string)
                    && source.GetType() != typeof(PyTuple))
                    throw new PyNotImplementedError("locals() star expansion with a protocol iterable is not supported");
                positional += ToSequence(source).Length;
            }
            // 非字符串key检查在CALL边界，晚于唯一star的实际展开。
            if (nonString) throw new PyTypeError("keywords must be strings");
            if (keywords != 0) throw new PyTypeError("locals() takes no keyword arguments");
            if (positional != 0) throw new PyTypeError($"locals() takes no arguments ({positional} given)");
        }
        catch (PyBaseException error)
        {
            PyErrors.PrepareRaise(error);
            throw;
        }
    }

    // 本切只接受已有真实dict/native mapping的关键字展开。重复键先于非string错误；
    // 用户hash键的merge需缓存hash协议，未纳入时明确拒绝，不重放用户hash假装等价。
    private static (int Count, bool NonString) CheckLocalsKeywords(object?[] names, object?[] values)
    {
        if (names.Length == 0) return (0, false);
        var seen = new HashSet<object>(PyKeyComparer.Instance); bool nonString = false;
        void Add(object? key)
        {
            if (key is not (null or PyNone or string or bool or int or long or double or System.Numerics.BigInteger))
                throw new PyNotImplementedError("locals() keyword expansion with protocol keys is not supported");
            if (!seen.Add(key ?? PyNone.Instance))
                throw new PyTypeError($"locals() got multiple values for keyword argument '{PyRepr.Str(key)}'");
            nonString |= key is not string;
        }
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i] is not null and not PyNone) { Add(names[i]); continue; }
            PyDict? mapping = values[i] as PyDict ?? (values[i] as IPyNativeMapping)?.Dictionary;
            if (mapping is null)
            {
                if (values[i] is PyInstance)
                    throw new PyNotImplementedError("locals() keyword expansion with a custom mapping is not supported");
                throw new PyTypeError($"locals() argument after ** must be a mapping, not {PyRepr.TypeName(values[i])}");
            }
            foreach (var (key, _) in mapping.Entries()) Add(key);
        }
        return (seen.Count, nonString);
    }

    /// <summary>仅第一次实际调用分配字典；不同函数调用各自持有独立缓存槽。</summary>
    public static object FunctionLocalsEnsure(object? cache)
    {
        if (cache is not null and not PyNone) return cache;
        var comparer = new FunctionLocalsKeyComparer();
        var dictionary = new PyDict(comparer);
        comparer.Owner = dictionary;
        return dictionary;
    }

    /// <summary>顺序同步一个源码局部；null/未绑定哨兵删除名字，合法None仍保留。</summary>
    public static void FunctionLocalsSync(object cache, string name, object? value)
    {
        try
        {
            var dictionary = (PyDict)cache;
            if (value is null or PyUnbound)
            {
                // CP3.12的未绑定槽删除会压制KeyError，包括用户碰撞键__eq__抛出的KeyError。
                // 单次查找避免重复eq；正常缺键不创建异常，热路无异常控制流。
                try { dictionary.DeleteIfPresent(name); }
                catch (PyKeyError) { }
            }
            else dictionary.Set(name, value);
        }
        catch (PyBaseException error)
        {
            PyErrors.PrepareRaise(error);
            throw;
        }
    }

    // 保存后间接调用没有生成函数的缓存/槽信息；不得返回调用器自身的假局部字典。
    private static object UnsupportedFunctionLocals(object?[] args, string[] names)
    {
        try
        {
            CheckLocalsArguments(args, names);
            throw new PyNotImplementedError("locals() requires a supported direct call in an ordinary function");
        }
        catch (PyBaseException error)
        {
            PyErrors.PrepareRaise(error);
            throw;
        }
    }

    private static void CheckLocalsArguments(object?[] args, string[] names)
    {
        // 保存调用已由共有入口展开；重复关键字比内建不接受关键字更早失败。
        if (names.Length > 1)
        {
            var seen = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (string name in names)
                if (!seen.Add(name)) throw new PyTypeError($"locals() got multiple values for keyword argument '{name}'");
        }
        if (names.Length != 0) throw new PyTypeError("locals() takes no keyword arguments");
        if (args.Length != 0) throw new PyTypeError($"locals() takes no arguments ({args.Length} given)");
    }
}

// 只用于locals生成的字典。用户协议可观察/改写既有值；若改变结构，在返回CLR字典前
// 明确终止外层操作，保回调已完成的副作用，避免失效slot/ref继续写入。
internal sealed class FunctionLocalsKeyComparer : IEqualityComparer<object>
{
    internal PyDict Owner { get; set; } = null!;
    private static readonly IEqualityComparer<object> Ordinary = PyKeyComparer.Instance;
    public new bool Equals(object? left, object? right)
    {
        long version = Owner.Version;
        bool result = Ordinary.Equals(left, right);
        CheckVersion(version);
        return result;
    }
    public int GetHashCode(object value)
    {
        long version = Owner.Version;
        int result = Ordinary.GetHashCode(value);
        CheckVersion(version);
        return result;
    }
    private void CheckVersion(long version)
    {
        if (version != Owner.Version)
            throw new PyNotImplementedError("structural mutation from a locals dictionary key callback is not supported");
    }
}
