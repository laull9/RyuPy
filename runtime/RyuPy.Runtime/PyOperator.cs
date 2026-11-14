// operator 的有限公开函数；算术和增量赋值复用语言入口，富比较共享原始值协议。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>常用二元 operator API；未实现的名称不导出。</summary>
internal static class PyOperator
{
    private static readonly Lazy<Dictionary<string, object?>> Attributes = new(Build);
    /// <summary>驻留模块属性，函数签名只构造一次。</summary>
    internal static Dictionary<string, object?> CreateAttributes() => Attributes.Value;
    private static Dictionary<string, object?> Build()
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        Add("add", args => PyOps.Binary(PyOps.Add, args[0]!, args[1]!));
        Add("iadd", args => PyOps.InPlaceBinary(PyOps.Add, args[0]!, args[1]!));
        foreach (var (name, op) in new[] { ("eq", PyOps.Eq), ("ne", PyOps.Ne),
            ("lt", PyOps.Lt), ("le", PyOps.Le), ("gt", PyOps.Gt), ("ge", PyOps.Ge) })
            Add(name, args => PyOrdinaryRichComparison.TryCompare(op, args[0]!, args[1]!, out object? value)
                ? value : PyOps.BoxBool(PyOps.Compare(op, args[0]!, args[1]!)));
        return result;
        void Add(string name, Func<object?[], object?> body) => result[name] = PyNativeFunctions.Create(
            name, ["a", "b"], [null, null], 2, body, ["posonly", "posonly"]);
    }
}
