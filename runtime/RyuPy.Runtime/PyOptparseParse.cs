// optparse阶段B：真实切片和公开消费列表；保留CP的pop(0)成本与错误时的部分状态。
using System;
using System.Linq;
using System.Collections.Generic;
namespace RyuPy.Runtime;

public sealed partial class PyOptparseParser
{
    private object Parse(object? args, object? supplied)
    {
        bool useArgv = PyOptparse.None(args);
        object? source = useArgv ? PyModules.Import("sys").GetAttr("argv") : args;
        object? sliced = PyOps.GetItem(source, new PySlice(useArgv ? 1L : null, null, null));
        object? values = PyOptparse.None(supplied) ? DefaultValues() : supplied;
        var left = new PyList();
        Data["rargs"] = sliced; Data["largs"] = left; Data["values"] = values;
        if (values is not PyOptparseValues actual) throw PyOptparse.Unsupported("custom parse values");
        // 非列表的合法自定义切片结果属于后续协议，不能泛化为迭代器消费。
        if (sliced is not (PyList or PyTuple or string)) throw PyOptparse.Unsupported("custom argument slice result");
        object right = sliced;
        long depth = PyTraceback.CaptureDepth();
        try { ProcessArguments(left, right, actual); }
        catch (PyBaseException error) when (error is PyBadOptionError or PyOptionValueError)
        {
            PyErrors.PrepareRaise(error); PyTraceback.PopTo(depth + 1);
            object? previous = PyErrors.EnterHandler(error);
            try { Error(PyRepr.Str(error)); }
            finally { PyErrors.LeaveHandler(previous); }
        }
        var remaining = new PyList();
        remaining.Items.AddRange(left.Items);
        if (right is not PyList finalList) throw PyOptparse.Own(new PyTypeError("can only concatenate list (not \"" + PyRepr.TypeName(right) + "\") to list"));
        remaining.Items.AddRange(finalList.Items);
        return new PyTuple([values, remaining]);
    }
    private void ProcessArguments(PyList left, object right, PyOptparseValues values)
    {
        while (PyOps.Truthy(right))
        {
            string arg = PyOptparse.Text(PyOps.GetItem(right, 0L));
            if (arg == "--") { RemoveFirst(right); return; }
            if (arg.StartsWith("--", StringComparison.Ordinal)) ProcessLong(right, values);
            else if (arg.StartsWith('-') && arg.Length > 1) ProcessShort(right, values);
            else if (PyOps.Truthy(Data["allow_interspersed_args"]))
            { left.Items.Add(PyOps.GetItem(right, 0L)); RemoveFirst(right); }
            else return;
        }
    }
    private static object? PopArgument(object args)
    {
        if (args is not PyList list) return PyOps.CallMethod(args, "pop", [0L], [], []);
        object? value = list.Items[0]; list.Items.RemoveAt(0); return value;
    }
    private static void RemoveFirst(object args)
    {
        if (args is PyList list) list.Items.RemoveAt(0);
        else throw PyOptparse.Own(new PyTypeError("'" + PyRepr.TypeName(args) + "' object does not support item deletion"));
    }
    private static void Prepend(object args, object? value)
    {
        if (args is PyList list) list.Items.Insert(0, value);
        else PyOps.CallMethod(args, "insert", [0L, value], [], []);
    }
    private static int CompareOptionNames(string a, string b)
    {
        int i = 0, j = 0;
        while (i < a.Length && j < b.Length)
        {
            int x = a[i++], y = b[j++];
            if (char.IsHighSurrogate((char)x) && i < a.Length && char.IsLowSurrogate(a[i])) x = char.ConvertToUtf32((char)x, a[i++]);
            if (char.IsHighSurrogate((char)y) && j < b.Length && char.IsLowSurrogate(b[j])) y = char.ConvertToUtf32((char)y, b[j++]);
            if (x != y) return x.CompareTo(y);
        }
        return (a.Length - i).CompareTo(b.Length - j);
    }
    private void ProcessLong(object args, PyOptparseValues values)
    {
        string name = PyOptparse.Text(PopArgument(args));
        int equals = name.IndexOf('='); bool explicitValue = equals >= 0;
        if (explicitValue) { Prepend(args, name[(equals + 1)..]); name = name[..equals]; }
        if (!Long.ContainsKey(name))
        {
            string[] matches = Long.Keys.Where(x => x.StartsWith(name, StringComparison.Ordinal)).OrderBy(x => x, Comparer<string>.Create(CompareOptionNames)).ToArray();
            if (matches.Length == 0) throw PyOptparse.Error("BadOptionError", name);
            if (matches.Length > 1) throw PyOptparse.Error("AmbiguousOptionError", name, new PyList(matches.Cast<object?>()));
            name = matches[0];
        }
        PyOptparseOption option = Long[name]; object? value = null;
        if (option.Action == "store")
        {
            if (PyOps.Len(args) == 0) Error(name + " option requires 1 argument");
            value = PopArgument(args);
        }
        else if (explicitValue) Error(name + " option does not take a value");
        Perform(option, name, value, values);
    }
    private void ProcessShort(object args, PyOptparseValues values)
    {
        string cluster = PyOptparse.Text(PopArgument(args));
        int offset = 1;
        while (offset < cluster.Length)
        {
            int width = char.IsHighSurrogate(cluster[offset]) && offset + 1 < cluster.Length && char.IsLowSurrogate(cluster[offset + 1]) ? 2 : 1;
            string name = "-" + cluster.Substring(offset, width); offset += width;
            if (!Short.TryGetValue(name, out PyOptparseOption? option)) throw PyOptparse.Error("BadOptionError", name);
            object? value = null; bool attached = false;
            if (option.Action == "store")
            {
                if (offset < cluster.Length)
                {
                    string tail = cluster[offset..];
                    Prepend(args, tail); attached = true;
                }
                if (PyOps.Len(args) == 0) Error(name + " option requires 1 argument");
                value = PopArgument(args);
            }
            Perform(option, name, value, values);
            if (attached) break;
        }
    }
    private void Perform(PyOptparseOption option, string name, object? value, PyOptparseValues values)
    {
        switch (option.Action)
        {
            case "store": values.SetAttr(PyOptparse.Text(option.Dest), option.CheckValue(name, value)); break;
            case "store_true": values.SetAttr(PyOptparse.Text(option.Dest), true); break;
            case "store_false": values.SetAttr(PyOptparse.Text(option.Dest), false); break;
            case "help": PyOps.CallValue(GetAttr("print_help"), [], [], []); Exit(0L, null); break;
            case "version": PrintVersion(null); Exit(0L, null); break;
            default: throw PyOptparse.Unsupported("mutated option action");
        }
    }
}
