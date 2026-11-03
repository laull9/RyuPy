using System;

namespace RyuPy.Runtime;

/// <summary>原生标准库函数复用Python参数绑定器，定义时缓存签名；null返回统一变None，避免与未绑定槽混淆。</summary>
public static class PyNativeFunctions
{
    // 只有本工厂拥有的固定必填位置一参函数可跳过数组；普通Body没有逃逸假设。
    internal static PyFunction CreateUnary(string name, Func<object?, object?> operation) =>
        new UnaryHolder(name, operation).Owner;

    internal static bool TryInvokeUnary(object? target, object? argument, out object? result)
    {
        if (target is PyFunction function && function.Body.Target is UnaryHolder holder
            && ReferenceEquals(function, holder.Owner) && ReferenceEquals(function.Body, holder.Body))
        {
            result = holder.InvokeOne(argument);
            return true;
        }
        result = null;
        return false;
    }

    // owner/body初始化完成后才发布；内部环可由GC回收，不新增全局根或所有函数字段。
    private sealed class UnaryHolder
    {
        private readonly Func<object?, object?> operation;
        private readonly PySignature signature;
        internal readonly PyCall Body;
        internal readonly PyFunction Owner;

        internal UnaryHolder(string name, Func<object?, object?> operation)
        {
            this.operation = operation;
            signature = PySignature.Create(name, ["x"], ["posonly"], ["0"]);
            Body = InvokeArray;
            Owner = new PyFunction(name, signature, Body, [null], false);
        }

        internal object? InvokeOne(object? argument) => operation(argument) ?? PyNone.Instance;

        private object? InvokeArray(PyFunction self, object?[] args, string[] names, object?[] values) =>
            InvokeOne(PyBinder.Bind(signature, args, names, values, self.Defaults)[0]);
    }

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
