// 冷BoundArguments的活arguments存储：CPython替换字段后，下次原生读取仍取同一真实字典。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private sealed class InspectBoundStorage(CPythonHost host, PythonObject owner) : IPyInspectBoundStorage
    {
        public PyDict Read() => host.PythonCall(() =>
        {
            nint mapping = host.Check(Native.rp_op(2, owner.Pointer, s: "arguments"));
            try
            {
                if (Native.rp_container_kind(mapping) != 2) throw new PyTypeError("BoundArguments.arguments must be an exact dictionary");
                return host.ImportInspectDictionary(mapping);
            }
            finally { Native.rp_decref(mapping); }
        });
        public void Write(PyDict value) => host.PythonCall(() =>
        {
            nint mapping = host.ToPython(value);
            try { Native.rp_decref(host.Check(Native.rp_op(3, owner.Pointer, mapping, s: "arguments"))); }
            finally { Native.rp_decref(mapping); }
            return 0;
        });
    }
}
