// 字符串子类的默认值协议；只在已验证布局的冷分支使用，不执行用户文本转换。
using System;
using System.Numerics;
namespace RyuPy.Runtime;

internal static class PyStringProtocols
{
    private static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
    internal static bool Contains(object self, object? needle)
    {
        string text = PyStringLayout.RequireRaw(self);
        if (!PyStringLayout.TryGet(needle, out string part))
            throw Own(new PyTypeError($"'in <string>' requires string as left operand, not {PyRepr.TypeName(needle)}"));
        return text.Contains(part, StringComparison.Ordinal);
    }
    internal static object Concat(object self, object? other)
    {
        string text = PyStringLayout.RequireRaw(self);
        if (!PyStringLayout.TryGet(other, out string right))
            throw Own(new PyTypeError($"can only concatenate str (not \"{PyRepr.TypeName(other)}\") to str"));
        return string.Concat(text, right);
    }
    internal static object Repeat(object self, object? count)
    {
        string text = PyStringLayout.RequireRaw(self);
        object? number = count;
        if (number is not (bool or int or long or BigInteger))
        {
            if (!PyObject.TryMethod(number, "__index__", [], out number))
                throw Own(new PyTypeError($"can't multiply sequence by non-int of type '{PyRepr.TypeName(count)}'"));
            if (number is bool) throw Own(new PyNotImplementedError("str repetition: __index__ returning bool requires DeprecationWarning support"));
            if (number is not (int or long or BigInteger))
                throw Own(new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(number)})"));
        }
        try { return PyOps.RepeatRootString(text, number!); }
        catch (PyBaseException error) { throw Own(error); }
    }
    internal static object? Item(object self, object? key)
    {
        string text = PyStringLayout.RequireRaw(self);
        if (key is PySlice slice)
        {
            // 现有切片算法不调用用户槽；仅准备它自身的参数错误。
            try { return PyText.Slice(text, slice); }
            catch (PyBaseException error) { throw Own(error); }
        }
        long index = PyStringIndex.Convert(key);
        try { return PyText.At(text, index); }
        catch (PyBaseException error) { throw Own(error); }
    }
    internal static object Compare(string name, object self, object? other)
    {
        string text = PyStringLayout.RequireRaw(self);
        if (!PyStringLayout.TryGet(other, out string right)) return PyNotImplemented.Instance;
        // 与现精确文本比较保持同一UTF16序，码点排序旧边界单独保留正期望。
        int order = string.CompareOrdinal(text, right);
        return name switch
        {
            "__eq__" => order == 0, "__ne__" => order != 0, "__lt__" => order < 0,
            "__le__" => order <= 0, "__gt__" => order > 0, "__ge__" => order >= 0,
            _ => throw new PyRuntimeError("unknown string comparison slot"),
        };
    }

    internal static object? Invoke(string name, object self, object?[] args) => name switch
    {
        "__str__" => PyStringLayout.RequireRaw(self),
        "__repr__" => PyRepr.Quote(PyStringLayout.RequireRaw(self)),
        "__hash__" => PyHash.Of(PyStringLayout.RequireRaw(self)),
        "__len__" => PyText.Length(PyStringLayout.RequireRaw(self)),
        "__iter__" => PyIter.Create(PyStringLayout.RequireRaw(self)),
        "__getitem__" => Item(self, args[0]),
        "__contains__" => Contains(self, args[0]),
        "__add__" => Concat(self, args[0]),
        "__mul__" or "__rmul__" => Repeat(self, args[0]),
        "__mod__" => PyPercentFormat.Format(PyStringLayout.RequireRaw(self), args[0]),
        "__eq__" or "__ne__" or "__lt__" or "__le__" or "__gt__" or "__ge__" => Compare(name, self, args[0]),
        _ => throw new PyRuntimeError("unknown string root slot"),
    };
}
