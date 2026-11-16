// Sequence及MutableSequence的共享Python协议实现；不穿透用户getitem/insert/pop覆盖。
using System;
namespace RyuPy.Runtime;
public static partial class PyAbc
{
    private static void InstallSequence(PyClass cls)
    {
        if (cls.Name == "Sequence")
        {
            Method(cls, "__iter__", ["self"], a => new SequenceIterator(a[0], false));
            Method(cls, "__reversed__", ["self"], a => new SequenceIterator(a[0], true));
            Method(cls, "__contains__", ["self", "value"], a =>
            {
                var iterator = PyIter.Create(a[0]);
                while (iterator.MoveNext()) if (PyOps.ValuesEqual(iterator.Current(), a[1])) return true;
                return false;
            });
            Method(cls, "count", ["self", "value"], a =>
            {
                long count = 0; var iterator = PyIter.Create(a[0]);
                while (iterator.MoveNext()) if (PyOps.ValuesEqual(iterator.Current(), a[1])) count++;
                return count;
            });
            Method(cls, "index", ["self", "value", "start", "stop"], a =>
            {
                object i = a[2]!; object? stop = a[3];
                if (PyOps.Compare(PyOps.Lt, i, 0L)) { i = PyOps.Binary(PyOps.Add, PyOps.Len(a[0]), i); if (PyOps.Compare(PyOps.Lt, i, 0L)) i = 0L; }
                if (stop is not null and not PyNone && PyOps.Compare(PyOps.Lt, stop, 0L)) stop = PyOps.Binary(PyOps.Add, PyOps.Len(a[0]), stop);
                while (stop is null or PyNone || PyOps.Compare(PyOps.Lt, i, stop))
                {
                    object? value; try { value = PyOps.GetItem(a[0], i); } catch (PyIndexError) { break; }
                    if (PyOps.ValuesEqual(value, a[1])) return i;
                    i = PyOps.Binary(PyOps.Add, i, 1L);
                }
                throw EmptyError(new PyValueError(""));
            }, [0L, PyNone.Instance]);
        }
        if (cls.Name != "MutableSequence") return;
        Method(cls, "append", ["self", "value"], a => { Call(a[0], "insert", PyOps.Len(a[0]), a[1]); return PyNone.Instance; });
        Method(cls, "clear", ["self"], a => { try { while (true) Call(a[0], "pop"); } catch (PyIndexError) { return PyNone.Instance; } });
        Method(cls, "reverse", ["self"], a =>
        {
            long length = PyOps.Len(a[0]);
            for (long i = 0; i < length / 2; i++)
            {
                object? left = PyOps.GetItem(a[0], i), right = PyOps.GetItem(a[0], length - i - 1);
                PyOps.SetItem(a[0], i, right); PyOps.SetItem(a[0], length - i - 1, left);
            }
            return PyNone.Instance;
        });
        Method(cls, "extend", ["self", "values"], a =>
        {
            object? values = ReferenceEquals(a[0], a[1]) ? new PyList(PyOps.ToSequence(a[1])) : a[1];
            var iterator = PyIter.Create(values); while (iterator.MoveNext()) Call(a[0], "append", iterator.Current());
            return PyNone.Instance;
        });
        Method(cls, "pop", ["self", "index"], a => { object? value = PyOps.GetItem(a[0], a[1]); PyOps.DelItem(a[0], a[1]); return value; }, [-1L]);
        Method(cls, "remove", ["self", "value"], a => { PyOps.DelItem(a[0], Call(a[0], "index", a[1])); return PyNone.Instance; });
        Method(cls, "__iadd__", ["self", "values"], a => { Call(a[0], "extend", a[1]); return a[0]; });
    }
}
internal sealed class SequenceIterator(object? source, bool reverse) : PyToolIterator
{
    private bool started, ended;
    private long index;
    private object? current;
    public override string TypeName => "generator";
    public override object? GetAttr(string name) => name is "send" or "throw" or "close"
        ? throw PyAbcIteration.UnsupportedControl(name) : base.GetAttr(name);
    public override bool MoveNext()
    {
        if (ended) return false;
        try
        {
            if (!started) { started = true; index = reverse ? PyOps.Len(source) - 1 : 0; }
            if (reverse && index < 0) { ended = true; return false; }
            try { current = PyOps.GetItem(source, index); }
            catch (PyIndexError) when (!reverse) { ended = true; return false; }
            index += reverse ? -1 : 1; return true;
        }
        catch (PyStopIteration error) { ended = true; throw PyAbcIteration.EscapingStop(error); }
        catch { ended = true; throw; }
    }
    public override object? Current() => current;
}
