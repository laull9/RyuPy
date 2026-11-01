using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>
/// 调用的运行时入口：一等函数调用、实参展开与绑定失败的错误构造。
/// </summary>
/// <remarks>
/// 静态直调由编译器直接生成，不经过这里；本文件只服务三条无法在编译期完成的路径：
/// <list type="bullet">
/// <item>调用可能为任意值的可调用对象（函数对象变量、参数传进来的函数）；</item>
/// <item>带 <c>*iterable</c> / <c>**mapping</c> 的调用（长度只有运行时知道）；</item>
/// <item>绑定会失败的调用（需要 Python 语义的 <c>TypeError</c>）。</item>
/// </list>
/// </remarks>
public static partial class PyOps
{
    /// <summary>共享的空实参数组；`CallValue` / `CallMethod` 没有关键字时复用。</summary>
    private static readonly object?[] EmptyObjects = [];

    /// <summary>返回共享空数组（调用方只读）。</summary>
    public static object?[] EmptyArray() => EmptyObjects;

    /// <summary>`*iterable` 实参的标记。</summary>
    private sealed class StarredArg
    {
        public object? Value { get; }

        public StarredArg(object? value) => Value = value;
    }

    /// <summary>给 `*iterable` 实参打标记；展开发生在 <see cref="CallValue"/>。</summary>
    public static object StarArg(object? value) => new StarredArg(value);

    /// <summary>写数组元素。</summary>
    public static void ArraySet(object array, long index, object? value) =>
        ((object?[])array)[index] = value;

    /// <summary>读数组元素。</summary>
    public static object? ArrayGet(object array, long index) => ((object?[])array)[index];

    /// <summary>内建构造名作为值使用时的类型对象（供 <c>isinstance</c> 等）。</summary>
    public static object TypeObject(string name) => PyType.Get(name);

    /// <summary>绑定失败：抛出带给定消息的 <c>TypeError</c>。</summary>
    /// <remarks>
    /// 返回值只是为了让 MIR 把它当成普通表达式；函数永远抛异常。
    /// <paramref name="kind"/> 保留错误种类，便于将来做更细的映射。
    /// </remarks>
    public static object? ArgError(int kind, string message) =>
        throw new PyTypeError(message);

    /// <summary>调用任意可调用值：<c>f(args, *rest, **kwargs)</c>。</summary>
    public static object? CallValue(
        object? callable,
        object?[] rawArgs,
        object?[] rawNames,
        object?[] rawValues
    )
    {
        var (args, names, values) = FlattenArgs(rawArgs, rawNames, rawValues);
        return callable switch
        {
            PyFunction function => function.Invoke(args, names, values),
            PyBuiltinFunction function => function.Invoke(args, names, values),
            PyClass cls => cls.Invoke(args, names, values),
            PyBoundMethod method => method.Invoke(args, names, values),
            PyBuiltinMethod method => method.Invoke(args, names, values),
            PyStaticMethod staticMethod => CallValue(staticMethod.Value, args, names, values),
            PyClassMethod classMethod => CallValue(classMethod.Value, args, names, values),
            PyInstance instance => CallInstance(instance, args, names, values),
            _ => throw new PyTypeError(
                $"'{PyRepr.TypeName(callable)}' object is not callable"
            ),
        };
    }

    /// <summary>位置实参里是否含 <c>*iterable</c>。</summary>
    private static bool HasStarred(object?[] rawArgs)
    {
        foreach (object? arg in rawArgs)
        {
            if (arg is StarredArg)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>实参是否需要展开（含 <c>*</c> / <c>**</c>）。</summary>
    internal static bool NeedsFlatten(object?[] rawArgs, object?[] rawNames) =>
        HasStarred(rawArgs) || Array.IndexOf(rawNames, null) >= 0 || Array.IndexOf(rawNames, PyNone.Instance) >= 0;

    /// <summary>
    /// 展开 `*` / `**` 实参：位置数组里的 <see cref="StarredArg"/> 摊平，
    /// 名字为 <c>None</c> 的关键字项按映射展开。
    /// </summary>
    private static readonly string[] EmptyNames = [];

    internal static (object?[] Args, string[] Names, object?[] Values) FlattenArgs(
        object?[] rawArgs,
        object?[] rawNames,
        object?[] rawValues
    )
    {
        // 没有关键字时数组已经是展开形态，直接复用，避免每次调用分配。
        if (rawNames.Length == 0 && !HasStarred(rawArgs))
        {
            return (rawArgs, EmptyNames, rawValues);
        }
        // 有关键字但没有 `*` / `**`：位置实参可以直接复用，只转换关键字名。
        if (!NeedsFlatten(rawArgs, rawNames))
        {
            var plainNames = new string[rawNames.Length];
            for (int i = 0; i < rawNames.Length; i++)
            {
                plainNames[i] = rawNames[i] as string ?? PyRepr.Str(rawNames[i]);
            }
            return (rawArgs, plainNames, rawValues);
        }
        var args = new List<object?>(rawArgs.Length);
        foreach (object? arg in rawArgs)
        {
            if (arg is StarredArg starred)
            {
                foreach (object? item in ToSequence(starred.Value))
                {
                    args.Add(item);
                }
            }
            else
            {
                args.Add(arg);
            }
        }

        var names = new List<string>(rawNames.Length);
        var values = new List<object?>(rawValues.Length);
        for (int i = 0; i < rawNames.Length; i++)
        {
            object? name = rawNames[i];
            object? value = rawValues[i];
            if (name is null or PyNone)
            {
                // `**mapping`：只接受字典（Python 也允许任意 mapping，当前子集先支持 dict）
                if (value is not PyDict mapping)
                {
                    throw new PyTypeError(
                        $"argument after ** must be a mapping, not "
                            + $"'{PyRepr.TypeName(value)}'"
                    );
                }
                foreach (var (key, item) in mapping.Entries())
                {
                    if (key is not string text)
                    {
                        throw new PyTypeError(
                            $"keywords must be strings, not '{PyRepr.TypeName(key)}'"
                        );
                    }
                    names.Add(text);
                    values.Add(item);
                }
            }
            else
            {
                names.Add(name as string ?? PyRepr.Str(name));
                values.Add(value);
            }
        }
        return ([.. args], [.. names], [.. values]);
    }
    /// <summary>调用实例的 `__call__`。</summary>
    private static object? CallInstance(
        PyInstance instance,
        object?[] args,
        string[] names,
        object?[] values
    )
    {
        if (instance.Class.TryLookup("__call__", out PyClass owner, out object? call))
        {
            return PyObject.InvokeBound(call, owner, instance, args, names, values);
        }
        throw new PyTypeError($"'{instance.Class.Name}' object is not callable");
    }

}
