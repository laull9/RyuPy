// tzinfo是可实例化的无payload协议根，不是ABC；默认虚拟方法抛NI而非抽象构造拒绝。
using System;
namespace RyuPy.Runtime;
internal static class PyDateTimeTzInfo
{
    private static readonly Lazy<PyClass> Canonical = new(Build);
    internal static PyClass Root => Canonical.Value;
    private static PyClass Build()
    {
        var root = new PyClass("tzinfo") { NativeDefinitionModule = "datetime" };
        root.SetAttr("__module__", "datetime");
        root.SetAttr("__slots__", new PyTuple([]));
        foreach (string method in new[] { "utcoffset", "dst", "tzname" })
            Install(root, method, _ => throw new PyNotImplementedError($"a tzinfo subclass must implement {method}()"));
        Install(root, "fromutc", args => PyDateTimeZoneProtocol.DefaultFromUtc(args[0], args[1]));
        root.FinalizeOrdinaryConstruction();
        // 不制造MI来获取反射；普通单链保留既有AOT能力。
        root.FreezeNativeProtocolRoot();
        return root;
    }
    private static void Install(PyClass root, string name, Func<object?[], object?> body)
    {
        var function = PyNativeFunctions.Create("tzinfo." + name, ["self", "dt"], [null, null], 2, args =>
        {
            if (args[0] is not PyDateTimeZone && !(args[0] is PyInstance instance && instance.Class.IsSubclassOf(root)))
                throw new PyTypeError($"descriptor '{name}' requires a 'datetime.tzinfo' object");
            return body(args);
        }, ["posonly", "posonly"]);
        PyOps.FunctionModule(function, "datetime");
        root.SetAttr(name, function);
    }
}
