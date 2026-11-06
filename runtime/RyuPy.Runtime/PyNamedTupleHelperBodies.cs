// namedtuple helper 正文：定义处字段闭包与实际 receiver 类型/内容分开，先保持消费副作用再报错。
using System;
using System.Text;

namespace RyuPy.Runtime;

internal sealed partial class PyNamedTupleLayout
{
    internal PyNamedTupleInstance Make(PyClass cls, object? source)
    {
        PyTuple values = PyNamedTupleSequence.Materialize(source);
        var result = new PyNamedTupleInstance(cls, (object?[])values.Items.Clone());
        if (result.Length != FieldCount)
            throw Error(new PyTypeError($"Expected {FieldCount} arguments, got {result.Length}"));
        return result;
    }

    internal object? Replace(PyNamedTupleInstance receiver, PyDict keywords)
    {
        // 与 self._make(map(...)) 同序：先固定真实 receiver 的方法，再惰性消费定义方字段。
        object? make = PyObject.GetAttr(receiver, "_make");
        object? result = PyOps.CallValue(make, [new Replacement(this, receiver, keywords)], [], []);
        if (keywords.Count != 0)
            throw Error(new PyValueError("Got unexpected field names: " + PyRepr.Repr(new PyList([.. keywords.Keys]))));
        return result;
    }

    internal static PyDict AsDictionary(PyNamedTupleInstance receiver)
    {
        PyTuple fields = (PyTuple)PyObject.GetAttr(receiver, "_fields")!;
        var result = new PyDict();
        for (int index = 0; index < Math.Min(fields.Length, receiver.Length); index++)
            result.Set(fields.Items[index], receiver.Items[index]);
        return result;
    }

    internal string FormatRepr(PyNamedTupleInstance receiver)
    {
        var output = new StringBuilder(receiver.Class.Name).Append('(');
        for (int index = 0; index < FieldCount; index++)
        {
            if (index >= receiver.Length) throw Error(new PyTypeError("not enough arguments for format string"));
            if (index != 0) output.Append(", ");
            output.Append(_fields[index]).Append('=').Append(PyRepr.Repr(receiver.Items[index]));
        }
        // 不能在逐值 repr 前检查总长度；异常和副作用都必须按百分号消费次序保留。
        if (receiver.Length > FieldCount) throw Error(new PyTypeError("not all arguments converted during string formatting"));
        return output.Append(')').ToString();
    }

    internal string HelperDoc(PyClass owner, string name)
    {
        string arguments = string.Join(", ", _fields) + (FieldCount == 1 ? "," : "");
        return name switch
        {
            "__new__" => $"Create new instance of {owner.Name}({arguments})",
            "_make" => $"Make a new {owner.Name} object from a sequence or iterable",
            "_replace" => $"Return a new {owner.Name} object replacing specified fields with new values",
            "_asdict" => "Return a new dict which maps field names to their values.",
            "__getnewargs__" => "Return self as a plain tuple.  Used by copy and pickle.",
            "__repr__" => "Return a nicely formatted representation string",
            _ => "",
        };
    }

    private sealed class Replacement(PyNamedTupleLayout layout, PyNamedTupleInstance receiver, PyDict keywords) : IPyIterator
    {
        private int _index;
        private object? _current;
        public bool MoveNext()
        {
            if (_index >= layout.FieldCount || _index >= receiver.Length) return false;
            _current = keywords.Pop(layout._fields[_index], true, receiver.Items[_index]);
            _index++; return true;
        }
        public object? Current() => _current;
    }
}
