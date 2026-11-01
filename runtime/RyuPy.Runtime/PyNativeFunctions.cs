using System;

namespace RyuPy.Runtime;

/// <summary>原生标准库函数复用Python参数绑定器，定义时缓存签名；null返回统一变None，避免与未绑定槽混淆。</summary>
public static class PyNativeFunctions
{
    /// <summary>创建固定签名函数；defaults与names等长，必填位置用null占位。</summary>
    public static PyFunction Create(string name, string[] names, object?[] defaults, int required,
        Func<object?[], object?> body, string[]? kinds = null)
    {
        var flags = new string[names.Length];
        for (int index = 0; index < flags.Length; index++) flags[index] = index < required ? "0" : "1";
        if (kinds is null) { kinds = new string[names.Length]; Array.Fill(kinds, "pos"); }
        PySignature signature = PySignature.Create(name, names, kinds, flags);
        return new PyFunction(name, signature,
            (self, args, keywords, values) => body(PyBinder.Bind(signature, args, keywords, values, self.Defaults)) ?? PyNone.Instance,
            defaults, false);
    }

    /// <summary>创建只接受位置参数的变参函数，未知关键字由统一绑定器拒绝。</summary>
    public static PyFunction CreateVariadic(string name, Func<object?[], object?> body)
    {
        PySignature signature = PySignature.Create(name, ["args"], ["varpos"], ["0"]);
        return new PyFunction(name, signature, (self, args, keywords, values) =>
        {
            object?[] bound = PyBinder.Bind(signature, args, keywords, values, self.Defaults);
            return body(((PyTuple)bound[0]!).Items) ?? PyNone.Instance;
        }, [], false);
    }
}
