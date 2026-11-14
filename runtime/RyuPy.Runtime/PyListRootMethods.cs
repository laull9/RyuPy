// list 的十一命名根方法；参数绑定与容器正文分离，用户回调异常不被重新 PrepareRaise。
using System;
using System.Numerics;
namespace RyuPy.Runtime;

internal static class PyListRootMethods
{
    internal static bool Has(string name) => name is "append" or "extend" or "insert" or "remove" or "index"
        or "count" or "clear" or "pop" or "reverse" or "copy" or "sort";
    private static PyTypeError Error(string message) => PyListLayout.Own(new PyTypeError(message));
    internal static object? Invoke(object self, string name, object?[] args, string[] names, object?[] values)
    {
        PyList list = PyListLayout.RequireRaw(self);
        if (name is "clear" or "pop" or "reverse" or "copy")
            return PyOps.CallListCheckedMethod(list, name, args, names);
        if (name == "sort")
        {
            // CP 的参数总数守卫先于 keyword-only 检查。
            if (args.Length + names.Length > 2) throw Error($"sort() takes at most 2 arguments ({args.Length + names.Length} given)");
            if (args.Length != 0) throw Error("sort() takes no positional arguments");
            foreach (string keyword in names)
                if (keyword is not ("key" or "reverse")) throw Error($"'{keyword}' is an invalid keyword argument for sort()");
            return Sort(list, names, values);
        }
        if (names.Length != 0) throw Error($"list.{name}() takes no keyword arguments");
        if (name == "insert")
        {
            if (args.Length != 2) throw Error($"insert expected 2 arguments, got {args.Length}");
        }
        else if (name == "index")
        {
            if (args.Length < 1) throw Error("index expected at least 1 argument, got 0");
            if (args.Length > 3) throw Error($"index expected at most 3 arguments, got {args.Length}");
        }
        else if (args.Length != 1) throw Error($"list.{name}() takes exactly one argument ({args.Length} given)");
        switch (name)
        {
            case "append": list.Append(args[0]); break;
            case "extend": PyOps.ExtendList(list, args[0]); break;
            case "insert":
                long index = Integer(args[0], false);
                // clamp 在窄化之前，避免现有 Insert 对负宽整数的 int 截断。
                list.Items.Insert((int)(index < 0 ? Math.Max(0, index + list.Count) : Math.Min(index, list.Count)), args[1]);
                break;
            case "index":
                long start = args.Length > 1 ? Integer(args[1], true) : 0;
                long stop = args.Length > 2 ? Integer(args[2], true) : long.MaxValue;
                if (start < 0) start = Math.Max(0, start + list.Count);
                if (stop < 0) stop = Math.Max(0, stop + list.Count);
                for (long i = start; i < stop && i < list.Count; i++)
                    if (Equal(list.GetItem(i), args[0])) return i;
                throw PyListLayout.Own(new PyValueError($"{PyRepr.Repr(args[0])} is not in list"));
            case "count":
                long count = 0;
                for (int i = 0; i < list.Count; i++) if (Equal(list.GetItem(i), args[0])) count++;
                return count;
            case "remove":
                for (int i = 0; i < list.Count; i++)
                {
                    if (!Equal(list.GetItem(i), args[0])) continue;
                    // 比较可以缩短原表；CP 删除当前位置的切片，即使已越尾仍成功。
                    if (i < list.Count) list.Items.RemoveAt(i);
                    return PyNone.Instance;
                }
                throw PyListLayout.Own(new PyValueError("list.remove(x): x not in list"));
        }
        return PyNone.Instance;
    }
    private static object Sort(PyList list, string[] names, object?[] values)
    {
        object? key = PyNone.Instance;
        object? reverseValue = false;
        for (int i = 0; i < names.Length; i++)
            if (names[i] == "key") key = values[i]; else reverseValue = values[i];
        // reverse 的真假回调必须先执行；随后检查回调修改后的当前列表。
        bool reverse = PyOps.Truthy(reverseValue);
        if (key is not (null or PyNone))
            throw PyListLayout.Own(new PyNotImplementedError("list root sort with key callbacks is not supported"));
        bool numeric = false, text = false;
        for (int i = 0; i < list.Count; i++)
        {
            object? item = list.GetItem(i);
            if (item is string scalarText)
            {
                // 现有排序器使用 UTF16 顺序；含代理码元时不等于 Python 码点序，明确冷拒。
                foreach (char unit in scalarText)
                    if (char.IsSurrogate(unit))
                        throw PyListLayout.Own(new PyNotImplementedError("list root sort supports only BMP strings without surrogate code units"));
                text = true;
            }
            else if (item is bool or int or long or BigInteger || item is double number && !double.IsNaN(number)) numeric = true;
            else throw PyListLayout.Own(new PyNotImplementedError("list root sort supports only native scalar elements without callbacks"));
        }
        if (numeric && text)
            throw PyListLayout.Own(new PyNotImplementedError("list root sort with mixed text and numeric elements is not supported"));
        // 此范围内不再执行 Python 回调；旧稳定排序器可安全复用。
        PySort.Sort(list.Items, reverse);
        return PyNone.Instance;
    }
    internal static bool Equal(object? left, object? right)
    {
        if (ReferenceEquals(left, right)) return true;
        // 容器判等采用 RichCompareBool：严格子类右槽优先，非bool结果允许真假转换。
        bool rightFirst = left is PyInstance a && right is PyInstance b
            && !ReferenceEquals(a.Class, b.Class) && b.Class.IsSubclassOf(a.Class);
        bool tried = false;
        if (rightFirst && TryEqualSlot(right, left, ref tried, out bool result)) return result;
        if (TryEqualSlot(left, right, ref tried, out result)) return result;
        if (!rightFirst && TryEqualSlot(right, left, ref tried, out result)) return result;
        // 用户槽已经返回 NI，不让全局旧 Equal 再调用一次；身份分支已在入口处理。
        return !tried && PyHash.Equal(left, right);
    }
    private static bool TryEqualSlot(object? self, object? other, ref bool tried, out bool result)
    {
        result = false;
        if (!PyObject.TryMethod1(self, "__eq__", other, out object? value)) return false;
        tried = true;
        if (value is PyNotImplemented) return false;
        result = PyOps.Truthy(value);
        return true;
    }
    internal static long Integer(object? value, bool clip)
    {
        object? integer = value;
        if (!PyInt.IsInt(integer))
        {
            if (!PyObject.TryMethod(value, "__index__", [], out integer))
                throw Error(clip ? "slice indices must be integers or have an __index__ method" : $"'{PyRepr.TypeName(value)}' object cannot be interpreted as an integer");
            if (integer is bool) throw PyListLayout.Own(new PyNotImplementedError("list: __index__ returning bool requires DeprecationWarning support"));
            if (!PyInt.IsInt(integer)) throw Error($"__index__ returned non-int (type {PyRepr.TypeName(integer)})");
        }
        BigInteger wide = PyInt.ToBigInteger(integer!);
        if (wide >= long.MinValue && wide <= long.MaxValue) return (long)wide;
        if (clip) return wide.Sign < 0 ? long.MinValue : long.MaxValue;
        throw PyListLayout.Own(new PyOverflowError("Python int too large to convert to C ssize_t"));
    }
}
