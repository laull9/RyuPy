// count 的局部构造绑定与 exact 数值边界：先总实参数，再名字绑定，最后检查数值资格。
// 不调用用户转换、descriptor 或试算加法；未知数值协议明确拒绝，不伪造 CP 的非数值判断。
using System.Numerics;

namespace RyuPy.Runtime;

internal static class PyCountBinding
{
    internal static object Create(object?[] args, string[] names, object?[] values)
    {
        int total = args.Length + names.Length;
        if (total > 2)
        {
            // CP 区分纯关键字超量与含位置参数的超量；两者都先于绑定和数值资格检查。
            string kind = args.Length == 0 ? "keyword arguments" : "arguments";
            throw Error(new PyTypeError($"count() takes at most 2 {kind} ({total} given)"));
        }
        object? start = args.Length > 0 ? args[0] : PyInt.Box(0);
        object? step = args.Length > 1 ? args[1] : PyInt.Box(1);
        bool boundStart = args.Length > 0, boundStep = args.Length > 1;
        for (int index = 0; index < names.Length; index++)
        {
            string name = names[index];
            if (name is not ("start" or "step"))
                throw Error(new PyTypeError($"'{name}' is an invalid keyword argument for count()"));
            bool bound = name == "start" ? boundStart : boundStep;
            if (bound)
                throw Error(new PyTypeError($"argument for count() given by name ('{name}') and position ({(name == "start" ? 1 : 2)})"));
            if (name == "start") { start = values[index]; boundStart = true; }
            else { step = values[index]; boundStep = true; }
        }
        RequireNumber(start);
        RequireNumber(step);
        return new PyCount(start!, step!);
    }

    private static void RequireNumber(object? value)
    {
        if (PyInt.IsInt(value) || value is double) return;
        // PyTuple 可有用户派生载体，不能凭基类就断定它没有数值槽。
        if (value is null or PyNone or string or PyBaseObject or PyBytes or PyByteArray
            or PyList or PyDict or PySet or PyFrozenSet or PySlice or PyRange or PyDictView or PyType
            || value?.GetType() == typeof(PyTuple))
            throw Error(new PyTypeError("a number is required"));
        throw Unsupported("custom numeric protocol");
    }

    internal static bool IsIntegerOne(object value) => PyInt.IsInt(value)
        && (PyInt.TryI64(value, out long small) ? small == 1 : (BigInteger)value == BigInteger.One);

    internal static bool TryFastStart(object value, out long result)
    {
        if (PyInt.TryI64(value, out result)) return true;
        if (value is BigInteger big && big >= long.MinValue && big <= long.MaxValue)
        { result = (long)big; return true; }
        return false;
    }

    internal static PyBaseException Error(PyBaseException error) => (PyBaseException)PyErrors.PrepareRaise(error);
    internal static PyBaseException Unsupported(string operation) => Error(
        new PyNotImplementedError($"count: {operation} is not supported"));
}
