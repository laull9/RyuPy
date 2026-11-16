// namedtuple 专用冷绑定校准；仍复用统一绑定器，不扩大其它函数的错误语义或热路径。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal static class PyNamedTupleBinding
{
    internal static object?[] Bind(PySignature signature, object?[] args, string[] names,
        object?[] values, object?[] defaults)
    {
        int positional = 0, required = 0;
        foreach (PyParamKind kind in signature.Kinds)
            if (kind is PyParamKind.PositionalOnly or PyParamKind.PositionalOrKeyword) positional++;
        for (int index = 0; index < positional; index++)
            if (!signature.HasDefault[index]) required++;
        try
        {
            if (args.Length > positional)
            {
                // 本 API 无必填 kwonly/varargs：先验证所有关键字，再拒绝原超量位置参数。
                // 不能把截断的临时绑定结果当作调用成功。
                PyBinder.Bind(signature, args[..positional], names, values, defaults);
                int keywordOnly = 0;
                foreach (string name in names)
                {
                    int index = Array.IndexOf(signature.Names, name);
                    if (index >= 0 && signature.Kinds[index] == PyParamKind.KeywordOnly) keywordOnly++;
                }
                string expected = required == positional ? positional.ToString() : $"from {required} to {positional}";
                string suffix = keywordOnly == 0 ? "" : $" positional arguments (and {keywordOnly} keyword-only argument{(keywordOnly == 1 ? "" : "s")})";
                throw new PyTypeError($"{signature.Name}() takes {expected} positional argument{(positional == 1 && required == positional ? "" : "s")} but {args.Length}{suffix} {(args.Length == 1 && keywordOnly == 0 ? "was" : "were")} given");
            }
            return PyBinder.Bind(signature, args, names, values, defaults);
        }
        catch (PyTypeError error)
        {
            var missing = new List<string>();
            for (int index = args.Length; index < positional; index++)
                if (!signature.HasDefault[index] && Array.IndexOf(names, signature.Names[index]) < 0)
                    missing.Add(signature.Names[index]);
            string old = $"{signature.Name}() missing {missing.Count} required positional arguments: '{string.Join("', '", missing)}'";
            if (missing.Count >= 2 && error.PyMessage == old)
            {
                string listed = missing.Count == 2 ? $"'{missing[0]}' and '{missing[1]}'"
                    : "'" + string.Join("', '", missing.GetRange(0, missing.Count - 1)) + "', and '" + missing[^1] + "'";
                error = new PyTypeError($"{signature.Name}() missing {missing.Count} required positional arguments: {listed}");
            }
            throw PyNamedTupleLayout.Error(error);
        }
    }
}
