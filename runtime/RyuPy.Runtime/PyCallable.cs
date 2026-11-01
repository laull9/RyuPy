using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>
/// 编译产物里的函数对象所使用的调用委托。
/// </summary>
/// <remarks>
/// 生成的入口方法（thunk）签名与此一致：第一个参数是函数对象自身——慢路径
/// （<see cref="PyBinder.Bind"/>）要从实例上取签名、默认值与闭包环境；每次执行
/// `def` 都会创建新的默认值数组（M3.0），所以它们必须跟着实例走而不是静态槽位。
/// thunk 完成绑定后调用真正的静态方法并装箱返回值；闭包函数还会把
/// <see cref="PyFunction.Env"/> 作为额外尾参传给静态方法。
/// </remarks>
public delegate object? PyCall(PyFunction self, object?[] args, string[] kwNames, object?[] kwValues);

/// <summary>参数在调用绑定中的角色，判别值必须与 Rust 侧 <c>mir::ParamKind</c> 一致。</summary>
public enum PyParamKind
{
    /// <summary>`/` 之前：只能按位置传入。</summary>
    PositionalOnly = 0,
    /// <summary>普通参数：位置或关键字都可以。</summary>
    PositionalOrKeyword = 1,
    /// <summary>`*args`：收集多出来的位置实参。</summary>
    VarPositional = 2,
    /// <summary>`*` 之后：只能按关键字传入。</summary>
    KeywordOnly = 3,
    /// <summary>`**kwargs`：收集未知的关键字实参。</summary>
    VarKeyword = 4,
}

/// <summary>一个 Python 函数的参数元数据。</summary>
public sealed class PySignature
{
    /// <summary>函数名（错误消息里使用）。</summary>
    public string Name { get; }
    /// <summary>参数名，顺序与函数签名一致。</summary>
    public string[] Names { get; }
    /// <summary>参数种类，与 <see cref="Names"/> 一一对应。</summary>
    public PyParamKind[] Kinds { get; }
    /// <summary>该位置是否有默认值。</summary>
    public bool[] HasDefault { get; }

    /// <summary>
    /// 是否只有普通位置参数（没有 `*args` / `**kwargs` / 仅关键字参数）。
    /// 这类签名的位置调用可以走无分配快路径。
    /// </summary>
    public bool SimplePositional { get; }

    /// <summary>没有默认值的位置参数个数。</summary>
    public int RequiredCount { get; }

    private PySignature(string name, string[] names, PyParamKind[] kinds, bool[] hasDefault)
    {
        Name = name;
        Names = names;
        Kinds = kinds;
        HasDefault = hasDefault;
        SimplePositional = true;
        RequiredCount = 0;
        for (int i = 0; i < kinds.Length; i++)
        {
            if (kinds[i] is not (PyParamKind.PositionalOnly or PyParamKind.PositionalOrKeyword))
            {
                SimplePositional = false;
            }
            if (!hasDefault[i])
            {
                RequiredCount++;
            }
        }
    }

    /// <summary>由生成的模块初始化代码构造。</summary>
    /// <remarks>
    /// 种类与默认值标志以字符串数组传入：生成代码构造 `string[]` 比构造
    /// `int[]` / `bool[]` 少两种元素存储指令，也避免了对装箱数组元素的依赖。
    /// </remarks>
    public static PySignature Create(
        string name,
        string[] names,
        string[] kinds,
        string[] hasDefault
    )
    {
        if (kinds.Length != names.Length || hasDefault.Length != names.Length)
        {
            throw new ArgumentException("参数元数据长度不一致");
        }
        var converted = new PyParamKind[kinds.Length];
        var defaults = new bool[names.Length];
        for (int i = 0; i < kinds.Length; i++)
        {
            converted[i] = kinds[i] switch
            {
                "posonly" => PyParamKind.PositionalOnly,
                "pos" => PyParamKind.PositionalOrKeyword,
                "varpos" => PyParamKind.VarPositional,
                "kwonly" => PyParamKind.KeywordOnly,
                "varkw" => PyParamKind.VarKeyword,
                _ => throw new ArgumentException($"未知参数种类 {kinds[i]}"),
            };
            defaults[i] = hasDefault[i] == "1";
        }
        return new PySignature(name, names, converted, defaults);
    }
}

/// <summary>
/// 参数绑定：把位置实参、关键字实参与默认值映射到函数的形参槽位。
/// </summary>
/// <remarks>
/// 规则与 CPython 一致：
/// <list type="bullet">
/// <item>位置实参先填 `PositionalOnly` / `PositionalOrKeyword`，多出来的进 `*args`；</item>
/// <item>关键字实参按名字匹配 `PositionalOrKeyword` / `KeywordOnly`，未知的进 `**kwargs`；</item>
/// <item>同一个形参被位置与关键字同时赋值 → <c>TypeError</c>；</item>
/// <item>位置-only 形参被按关键字传入 → <c>TypeError</c>；</item>
/// <item>没有默认值且未赋值 → `TypeError`。</item>
/// </list>
/// 这些错误都是 Python 语义错误，因此抛 <see cref="PyTypeError"/> 而不是 CLR 异常。
/// </remarks>
public static class PyBinder
{
    /// <summary>返回与参数顺序一致的绑定结果数组。</summary>
    public static object?[] Bind(
        PySignature signature,
        object?[] args,
        string[] kwNames,
        object?[] kwValues,
        object?[] defaults
    )
    {
        // 常见路径：只有普通位置参数、没有关键字实参。此时不需要 filled / 位置槽列表，
        // 参数按顺序一一对应；实参正好占满时直接复用调用方数组（只读）。
        if (signature.SimplePositional && kwNames.Length == 0)
        {
            int total = signature.Names.Length;
            if (args.Length > total)
            {
                throw TooManyPositionals(signature, total, args.Length);
            }
            if (args.Length < signature.RequiredCount)
            {
                var missing = new List<string>();
                for (int i = args.Length; i < signature.RequiredCount; i++)
                {
                    missing.Add(signature.Names[i]);
                }
                throw MissingArguments(signature, missing, "positional");
            }
            if (args.Length == total)
            {
                return args;
            }
            var fastBound = new object?[total];
            Array.Copy(args, fastBound, args.Length);
            for (int i = args.Length; i < total; i++)
            {
                fastBound[i] = defaults[i];
            }
            return fastBound;
        }

        int count = signature.Names.Length;
        var bound = new object?[count];
        var filled = new bool[count];

        int varPositional = -1;
        int varKeyword = -1;
        var positionalSlots = new List<int>();
        for (int i = 0; i < count; i++)
        {
            switch (signature.Kinds[i])
            {
                case PyParamKind.PositionalOnly:
                case PyParamKind.PositionalOrKeyword:
                    positionalSlots.Add(i);
                    break;
                case PyParamKind.VarPositional:
                    varPositional = i;
                    break;
                case PyParamKind.VarKeyword:
                    varKeyword = i;
                    break;
            }
        }

        // 1. 位置实参
        var extraPositional = new List<object?>();
        for (int i = 0; i < args.Length; i++)
        {
            if (i < positionalSlots.Count)
            {
                int slot = positionalSlots[i];
                bound[slot] = args[i];
                filled[slot] = true;
            }
            else if (varPositional >= 0)
            {
                extraPositional.Add(args[i]);
            }
            else
            {
                throw TooManyPositionals(signature, positionalSlots.Count, args.Length);
            }
        }
        if (varPositional >= 0)
        {
            bound[varPositional] = new PyTuple([.. extraPositional]);
            filled[varPositional] = true;
        }

        // 2. 关键字实参
        var positionalOnlyPassed = new List<string>();
        var extraKeyword = new List<(string Name, object? Value)>();
        for (int i = 0; i < kwNames.Length; i++)
        {
            string name = kwNames[i];
            object? value = kwValues[i];
            int slot = FindByName(signature, name);
            if (slot >= 0)
            {
                if (filled[slot])
                {
                    throw new PyTypeError(
                        $"{signature.Name}() got multiple values for argument '{name}'"
                    );
                }
                bound[slot] = value;
                filled[slot] = true;
            }
            else if (varKeyword >= 0)
            {
                extraKeyword.Add((name, value));
            }
            else if (IsPositionalOnlyName(signature, name))
            {
                positionalOnlyPassed.Add(name);
            }
            else
            {
                throw new PyTypeError(
                    $"{signature.Name}() got an unexpected keyword argument '{name}'"
                );
            }
        }
        if (positionalOnlyPassed.Count > 0)
        {
            throw new PyTypeError(
                $"{signature.Name}() got some positional-only arguments passed as keyword "
                    + $"arguments: '{string.Join("', '", positionalOnlyPassed)}'"
            );
        }
        if (varKeyword >= 0)
        {
            var dict = new PyDict();
            foreach (var (name, value) in extraKeyword)
            {
                dict.Set(name, value);
            }
            bound[varKeyword] = dict;
            filled[varKeyword] = true;
        }

        // 3. 默认值与缺失检查
        var missingPositional = new List<string>();
        var missingKeywordOnly = new List<string>();
        for (int i = 0; i < count; i++)
        {
            if (filled[i] || signature.Kinds[i] == PyParamKind.VarPositional
                || signature.Kinds[i] == PyParamKind.VarKeyword)
            {
                continue;
            }
            if (i < defaults.Length && signature.HasDefault[i])
            {
                bound[i] = defaults[i];
                continue;
            }
            if (signature.Kinds[i] == PyParamKind.KeywordOnly)
            {
                missingKeywordOnly.Add(signature.Names[i]);
            }
            else
            {
                missingPositional.Add(signature.Names[i]);
            }
        }
        if (missingPositional.Count > 0)
        {
            throw MissingArguments(signature, missingPositional, "positional");
        }
        if (missingKeywordOnly.Count > 0)
        {
            throw MissingArguments(signature, missingKeywordOnly, "keyword-only");
        }
        return bound;
    }

    private static int FindByName(PySignature signature, string name)
    {
        for (int i = 0; i < signature.Names.Length; i++)
        {
            if ((signature.Kinds[i] == PyParamKind.PositionalOrKeyword
                    || signature.Kinds[i] == PyParamKind.KeywordOnly)
                && signature.Names[i] == name)
            {
                return i;
            }
        }
        return -1;
    }

    private static bool IsPositionalOnlyName(PySignature signature, string name)
    {
        for (int i = 0; i < signature.Names.Length; i++)
        {
            if (signature.Kinds[i] == PyParamKind.PositionalOnly && signature.Names[i] == name)
            {
                return true;
            }
        }
        return false;
    }

    private static PyTypeError TooManyPositionals(
        PySignature signature,
        int named,
        int given
    ) =>
        new(
            $"{signature.Name}() takes {named} positional argument"
                + $"{(named == 1 ? "" : "s")} but {given} were given"
        );

    private static PyTypeError MissingArguments(
        PySignature signature,
        List<string> names,
        string kind
    ) =>
        new(
            $"{signature.Name}() missing {names.Count} required {kind} argument"
                + $"{(names.Count == 1 ? "" : "s")}: '{string.Join("', '", names)}'"
        );
}

/// <summary>
/// 一等函数对象：名字、参数元数据、默认值数组与调用入口。
/// </summary>
/// <remarks>
/// 默认值数组在模块初始化时分配、由模块顶层代码填充，因此多次调用共享同一份默认值——
/// 这正是 <c>def f(x=[])</c> 在 CPython 里的行为。
/// </remarks>
public sealed class PyFunction
{
    /// <summary>函数名。</summary>
    public string Name { get; }
    /// <summary>参数元数据。</summary>
    public PySignature Signature { get; }
    /// <summary>调用入口（生成代码里的 thunk）。</summary>
    public PyCall Body { get; }
    /// <summary>默认值数组，与参数下标对齐。</summary>
    public object?[] Defaults { get; }

    /// <summary>函数体是否调用过 `super()`；只有这类函数需要维护方法上下文。</summary>
    public bool UsesSuper { get; }

    /// <summary>
    /// 闭包环境（M3.0）：外层作用域的 cell 数组；非闭包函数为 <c>null</c>。
    /// 同一 cell 对象被多个闭包共享，这正是 late binding 与共享修改的来源。
    /// </summary>
    public object?[]? Env { get; }

    /// <summary>构造函数对象。</summary>
    public PyFunction(
        string name,
        PySignature signature,
        PyCall body,
        object?[] defaults,
        bool usesSuper = true,
        object?[]? env = null
    )
    {
        Name = name;
        Signature = signature;
        Body = body;
        Defaults = defaults;
        UsesSuper = usesSuper;
        Env = env;
    }

    /// <summary>用已展开的实参调用。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object? Invoke(object?[] args, string[] kwNames, object?[] kwValues) =>
        Body(this, args, kwNames, kwValues);

    /// <summary>与 CPython 一致的 <c>&lt;function name&gt;</c> 形式。</summary>
    public override string ToString() => $"<function {Name}>";
}
