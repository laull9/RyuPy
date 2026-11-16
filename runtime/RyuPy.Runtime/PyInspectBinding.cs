// inspect绑定独立于执行绑定器：省略参数不填默认，partial允许缺失，位置专用名可进入**kwargs。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal static class PyInspectBinding
{
    internal static PyInspectBoundArguments Bind(PyInspectSignature signature, object?[] args, string[] names,
        object?[] values, bool partial)
    {
        var keywords = new PyDict();
        for (int i = 0; i < names.Length; i++)
        {
            if (keywords.TryGet(names[i], out _)) throw new PyTypeError($"multiple values for keyword argument '{names[i]}'");
            keywords.Set(names[i], values[i]);
        }
        var supplied = new PyDict(); int position = 0; PyInspectParameter? rest = null;
        foreach (var p in signature.Parameters)
        {
            int kind = p.Kind.Value;
            if (kind is 0 or 1)
            {
                if (position < args.Length)
                {
                    if (kind == 1 && keywords.TryGet(p.Name, out _)) throw new PyTypeError($"multiple values for argument '{p.Name}'");
                    supplied.Set(p.Name, args[position++]);
                }
                else if (kind == 0 && ReferenceEquals(p.Default, PyInspectModule.Empty) && keywords.TryGet(p.Name, out _))
                    throw new PyTypeError($"'{p.Name}' parameter is positional only, but was passed as a keyword");
                else if (kind == 1 && keywords.TryGet(p.Name, out object? value))
                { supplied.Set(p.Name, value); keywords.Delete(p.Name); }
            }
            else if (kind == 2)
            {
                if (position < args.Length) supplied.Set(p.Name, new PyTuple(args[position..]));
                position = args.Length;
            }
            else if (kind == 3 && keywords.TryGet(p.Name, out object? value))
            { supplied.Set(p.Name, value); keywords.Delete(p.Name); }
            else if (kind == 4) rest = p;
        }
        if (position < args.Length) throw new PyTypeError("too many positional arguments");
        if (keywords.Count > 0)
        {
            if (rest is null)
            {
                foreach (var p in signature.Parameters)
                    if (p.Kind.Value == 0 && keywords.TryGet(p.Name, out _))
                        throw new PyTypeError($"'{p.Name}' parameter is positional only, but was passed as a keyword");
                foreach (var key in keywords.Keys) throw new PyTypeError($"got an unexpected keyword argument '{key}'");
            }
            else supplied.Set(rest.Name, keywords);
        }
        if (!partial)
            foreach (var p in signature.Parameters)
                if (p.Kind.Value is not (2 or 4) && ReferenceEquals(p.Default, PyInspectModule.Empty) && !supplied.TryGet(p.Name, out _))
                    throw new PyTypeError($"missing a required argument: '{p.Name}'");
        return new PyInspectBoundArguments(signature, supplied);
    }
}
