// Mapping与三个live view使用真实_mapping槽和通用协议；字典值比较顺序及解包副作用保持Python语义。
using System;
using System.Collections.Generic;
namespace RyuPy.Runtime;
public static partial class PyAbc
{
    private static readonly object MissingDefault = new();
    private static object? MappingOf(object? view) => PyObject.GetAttr(view, "_mapping");
    private static IEnumerable<object?> ViewKeys(object? view)
    {
        foreach (object? key in Iterate(MappingOf(view))) yield return key;
    }
    private static IEnumerable<object?> ViewItems(object? view, bool items)
    {
        object? mapping = MappingOf(view);
        foreach (object? key in Iterate(mapping))
        {
            object? value = PyOps.GetItem(MappingOf(view), key);
            yield return items ? new PyTuple([key, value]) : value;
        }
    }
    private static PyDict AsDict(object? pairs)
    {
        var dict = new PyDict();
        foreach (object? pair in Iterate(pairs)) { var values = ((PyTuple)PyOps.UnpackSequence(pair, 2)).Items; dict.Set(values[0], values[1]); }
        return dict;
    }
    private static void InstallMapping(PyClass cls)
    {
        if (cls.Name == "Mapping")
        {
            cls.SetAttr("__reversed__", PyNone.Instance);
            Method(cls, "get", ["self", "key", "default"], a => { try { return PyOps.GetItem(a[0], a[1]); } catch (PyKeyError) { return a[2]; } }, [PyNone.Instance]);
            Method(cls, "__contains__", ["self", "key"], a => { try { PyOps.GetItem(a[0], a[1]); return true; } catch (PyKeyError) { return false; } });
            foreach (string name in new[] { "keys", "items", "values" })
                Method(cls, name, ["self"], a => RootType(name == "keys" ? "KeysView" : name == "items" ? "ItemsView" : "ValuesView").Invoke([a[0]], [], []));
            Method(cls, "__eq__", ["self", "other"], a =>
            {
                if (!Accepts(RootType("Mapping"), a[1])) return PyNotImplemented.Instance;
                return PyOps.ValuesEqual(AsDict(Call(a[0], "items")), AsDict(Call(a[1], "items")));
            });
        }
        if (cls.Name == "MutableMapping")
        {
            Method(cls, "pop", ["self", "key", "default"], a =>
            {
                object? value;
                try { value = PyOps.GetItem(a[0], a[1]); }
                catch (PyKeyError) { if (ReferenceEquals(a[2], MissingDefault)) throw; return a[2]; }
                PyOps.DelItem(a[0], a[1]); return value;
            }, [MissingDefault]);
            Method(cls, "popitem", ["self"], a =>
            {
                var iterator = PyIter.Create(a[0]); if (!iterator.MoveNext()) throw new PyKeyError("popitem(): mapping is empty");
                object? key = iterator.Current(), value = PyOps.GetItem(a[0], key); PyOps.DelItem(a[0], key); return new PyTuple([key, value]);
            });
            Method(cls, "clear", ["self"], a => { try { while (true) Call(a[0], "popitem"); } catch (PyKeyError) { return PyNone.Instance; } });
            Method(cls, "setdefault", ["self", "key", "default"], a =>
            {
                try { return PyOps.GetItem(a[0], a[1]); } catch (PyKeyError) { PyOps.SetItem(a[0], a[1], a[2]); return a[2]; }
            }, [PyNone.Instance]);
            Method(cls, "update", ["self", "other", "kwds"], a =>
            {
                object? other = a[1];
                if (Accepts(RootType("Mapping"), other))
                    foreach (object? key in Iterate(other)) PyOps.SetItem(a[0], key, PyOps.GetItem(other, key));
                else
                {
                    object? keys; try { keys = PyObject.GetAttr(other, "keys"); } catch (PyAttributeError) { keys = null; }
                    if (keys is not null)
                        foreach (object? key in Iterate(PyOps.CallValue(keys, [], [], []))) PyOps.SetItem(a[0], key, PyOps.GetItem(other, key));
                    else foreach (object? pair in Iterate(other))
                    { var values = ((PyTuple)PyOps.UnpackSequence(pair, 2)).Items; PyOps.SetItem(a[0], values[0], values[1]); }
                }
                foreach (object? key in Iterate(a[2])) PyOps.SetItem(a[0], key, PyOps.GetItem(a[2], key));
                return PyNone.Instance;
            }, [new PyTuple([]), null], ["posonly", "posonly", "varkw"]);
        }
        if (cls.Name == "MappingView")
        {
            Method(cls, "__init__", ["self", "mapping"], a => { PyObject.SetAttr(a[0], "_mapping", a[1]); return PyNone.Instance; });
            Method(cls, "__len__", ["self"], a => PyOps.Len(MappingOf(a[0])));
            Method(cls, "__repr__", ["self"], a => PyRepr.TypeName(a[0]) + "(" + PyRepr.Repr(MappingOf(a[0])) + ")");
        }
        if (cls.Name is "KeysView" or "ItemsView")
        {
            var factory = Method(cls, "_from_iterable", ["cls", "it"], a => new PySet(Iterate(a[1])));
            cls.SetAttr("_from_iterable", new PyClassMethod(factory));
        }
        if (cls.Name == "KeysView")
        {
            Method(cls, "__iter__", ["self"], a => new PyAbcIterator(ViewKeys(a[0]).GetEnumerator()));
            Method(cls, "__contains__", ["self", "key"], a => PyOps.Contains(MappingOf(a[0]), a[1]));
        }
        if (cls.Name is "ItemsView" or "ValuesView")
        {
            bool items = cls.Name == "ItemsView";
            Method(cls, "__iter__", ["self"], a => new PyAbcIterator(ViewItems(a[0], items).GetEnumerator()));
            Method(cls, "__contains__", ["self", "value"], a =>
            {
                if (!items)
                {
                    foreach (object? key in Iterate(MappingOf(a[0]))) if (PyOps.ValuesEqual(PyOps.GetItem(MappingOf(a[0]), key), a[1])) return true;
                    return false;
                }
                var pair = ((PyTuple)PyOps.UnpackSequence(a[1], 2)).Items;
                object? value; try { value = PyOps.GetItem(MappingOf(a[0]), pair[0]); } catch (PyKeyError) { return false; }
                return PyOps.ValuesEqual(value, pair[1]);
            });
        }
    }
}
