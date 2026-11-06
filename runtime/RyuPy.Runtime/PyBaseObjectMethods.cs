// 裸 object 的有限槽方法；派生分配属于后续构造协议，不能静默创建假实例。
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

internal static class PyBaseObjectMethods
{
    private static readonly ConcurrentDictionary<string, Method> Methods = new();
    internal static bool IsMethod(string name) => name is "__new__" or "__init__"
        or "__eq__" or "__ne__" or "__repr__" or "__str__" or "__hash__";
    // list继承的object.__str__与object根共享同一描述符；repr用户回调不进入旧广catch。
    internal static object Unbound(string name) => name == "__str__" ? PyListDescriptors.Unbound(name)
        : Methods.GetOrAdd(name, static n => new Method(n, null));
    internal static object Bind(PyBaseObject value, string name) => new Method(name, value);
    internal static object BindConstruction(object value, string name) => new Method(name, value);
    internal static bool IsRootNew(object? value) => ReferenceEquals(value, Unbound("__new__"));
    internal static bool IsRootInit(object? value) => ReferenceEquals(value, Unbound("__init__"));
    internal static bool IsRootHash(object? value) =>
        value is Method && ReferenceEquals(value, Unbound("__hash__"));

    // 只给已核object hash描述符绑定self，不把其它native callable泛化为描述符。
    internal static object? BindRootHash(object? value, object? self) =>
        self is not null && IsRootHash(value) ? BindConstruction(self, "__hash__") : value;

    // 独立原生可调用槽，不冒 PyFunction，也不走普通方法 PIC 的可变实例字典。
    private sealed class Method(string name, object? receiver) : PyNativeObject, IPyNativeCallable
    {
        public override string TypeName => receiver is not null ? "method-wrapper"
            : name == "__new__" ? "builtin_function_or_method" : "wrapper_descriptor";
        public override object? GetAttr(string attribute) => attribute switch
        {
            "__name__" => name,
            "__qualname__" => "object." + name,
            "__objclass__" when name != "__new__" => PyBaseObject.Type,
            "__self__" when receiver is not null => receiver,
            "__class__" => PyType.Get(TypeName),
            _ => throw Missing(attribute),
        };
        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            try { return InvokeCore(args, names); }
            catch (PyBaseException error) { throw (PyBaseException)PyErrors.PrepareRaise(error); }
        }
        private object? InvokeCore(object?[] args, string[] names)
        {
            if (name == "__new__") return New(args, names);
            int offset = receiver is null ? 1 : 0;
            if (args.Length < offset)
                throw new PyTypeError($"descriptor '{name}' of 'object' object needs an argument");
            object? self = receiver ?? args[0];
            int extra = args.Length - offset;
            if (name == "__init__")
            {
                // str沿真正object根init身份；仅其实际布局走专用参数规则，普通对象原路不变。
                if (PyStringLayout.TryGet(self, out _))
                    return PyStringLayout.Init(self, offset == 0 ? args : args[1..], names, []);
                if (extra == 0 && names.Length == 0) return PyNone.Instance;
                if (self is PyInstance instance)
                {
                    instance.Class.CheckRootInitArguments(extra, names.Length);
                    return PyNone.Instance;
                }
                throw new PyTypeError("object.__init__() takes exactly one argument (the instance to initialize)");
            }
            int required = name is "__eq__" or "__ne__" ? 1 : 0;
            if (extra != required || names.Length != 0)
                throw new PyTypeError($"object.{name}() requires {required + 1} positional arguments");
            if (name == "__eq__") return ReferenceEquals(self, args[offset]) ? true : PyNotImplemented.Instance;
            if (name == "__ne__")
            {
                // 直接 object.__ne__ 仍尊重 self 的真实 __eq__，NI 不得提前变 True。
                if (PyObject.TryMethod1(self, "__eq__", args[offset], out object? equal))
                    return equal is PyNotImplemented ? equal : !PyOps.Truthy(equal);
                return ReferenceEquals(self, args[offset]) ? false : PyNotImplemented.Instance;
            }
            if (name == "__hash__") return (long)RuntimeHelpers.GetHashCode(self ?? PyNone.Instance);
            if (self is not PyBaseObject bare)
                throw new PyNotImplementedError("explicit object repr/str for derived or other builtins is not supported yet");
            return bare.PythonRepr();
        }
        private static object New(object?[] args, string[] names)
        {
            if (args.Length == 0) throw new PyTypeError("object.__new__(): not enough arguments");
            object? cls = args[0];
            if (cls is PyClass userClass)
                return userClass.AllocateFromObject(args.Length - 1, names.Length);
            if (!ReferenceEquals(cls, PyBaseObject.Type))
            {
                if (cls is PyType or PyNativeType)
                    throw new PyTypeError("object.__new__ cannot allocate this builtin layout");
                throw new PyTypeError($"object.__new__(X): X is not a type object ({PyRepr.TypeName(cls)})");
            }
            if (args.Length != 1 || names.Length != 0) throw new PyTypeError("object() takes no arguments");
            return PyBaseObject.Create([], []);
        }
    }
}
