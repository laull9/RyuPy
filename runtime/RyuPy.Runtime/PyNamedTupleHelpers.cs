// 生成 namedtuple 的保护函数/绑定方法；class、static 与普通 descriptor 三种绑定明确分离。
// 仅本 API 的原生对象实现元数据和协议，不给普通函数或 tuple 热路径新增布局/分派。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal static class PyNamedTupleHelpers
{
    internal static void Prepare(PyClass cls, PyNamedTupleLayout layout,
        Dictionary<string, object?> attributes)
    {
        foreach (string name in new[] { "__new__", "_make", "_replace", "__repr__", "_asdict", "__getnewargs__" })
        {
            var method = new Method(cls, layout, name);
            attributes[name] = name switch
            {
                "__new__" => new PyStaticMethod(method),
                "_make" => new ClassDescriptor(method),
                _ => method,
            };
        }
    }

    private abstract class Protected : PyNativeObject
    {
        public override void SetAttr(string name, object? value) => throw PyNamedTupleLayout.Unsupported("changing namedtuple helper metadata is not supported");
        public override void DelAttr(string name) => throw PyNamedTupleLayout.Unsupported("changing namedtuple helper metadata is not supported");
        protected object? Reflection(string name) => name is "__dict__" or "__code__" or "__closure__" or "__annotations__" or "__kwdefaults__" or "__reduce__" or "__reduce_ex__"
            ? throw PyNamedTupleLayout.Unsupported("namedtuple helper reflection is not supported") : throw Missing(name);
    }

    private sealed class Method : Protected, IPyNativeCallable, IPyNativeDescriptor
    {
        internal readonly PyClass Owner;
        internal readonly string Name;
        private readonly PySignature _signature;
        internal readonly string QualifiedName;
        private readonly string _module;
        private readonly string _doc;
        internal Method(PyClass owner, PyNamedTupleLayout layout, string name)
        {
            Owner = owner; Name = name;
            QualifiedName = owner.Name + "." + name;
            _module = name == "__new__" ? "namedtuple_" + owner.Name : "collections";
            _doc = layout.HelperDoc(owner, name);
            string[] parameters = name switch { "_make" => ["cls", "iterable"], "_replace" => ["self", "kwds"], _ => ["self"] };
            string[] kinds = name == "_replace" ? ["posonly", "varkw"] : name == "_make" ? ["pos", "pos"] : ["pos"];
            _signature = PySignature.Create(QualifiedName, parameters, kinds, name is "_make" or "_replace" ? ["0", "0"] : ["0"]);
        }
        public override string TypeName => "function";
        public override object? GetAttr(string name) => name switch
        {
            "__name__" => Name,
            "__qualname__" => QualifiedName,
            "__module__" => _module,
            "__doc__" => _doc,
            "__defaults__" => Name == "__new__" ? Owner.NamedTupleLayout!.PublicDefaults : PyNone.Instance,
            "__annotations__" when Name == "__new__" && Owner.Attributes.ContainsKey("__annotations__") => Owner.Attributes["__annotations__"],
            "__class__" => PyType.Get("function"),
            "__get__" => new FunctionGet(this),
            _ => Reflection(name),
        };
        public object? Bind(PyClass owner, object? self) => self is null or PyNone ? this : new Bound(this, self);
        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            if (Name == "__new__") return PyNamedTupleLayout.ConstructExplicit(Owner, args, names, values);
            object?[] bound = PyNamedTupleBinding.Bind(_signature, args, names, values, []);
            if (Name == "_make")
            {
                if (!ReferenceEquals(bound[0], Owner))
                    throw PyNamedTupleLayout.Unsupported("namedtuple _make with a different explicit class is not supported");
                return Owner.NamedTupleLayout!.Make(Owner, bound[1]);
            }
            if (bound[0] is not PyNamedTupleInstance receiver)
                throw PyNamedTupleLayout.Unsupported("namedtuple helper duck receivers are not supported");
            return Name switch
            {
                "_replace" => Owner.NamedTupleLayout!.Replace(receiver, (PyDict)bound[1]!),
                "_asdict" => PyNamedTupleLayout.AsDictionary(receiver),
                "__getnewargs__" => new PyTuple((object?[])receiver.Items.Clone()),
                "__repr__" => Owner.NamedTupleLayout!.FormatRepr(receiver),
                _ => throw PyNamedTupleLayout.Unsupported("unknown namedtuple helper"),
            };
        }
        public override string PythonRepr() => $"<function {QualifiedName}>";
    }

    private sealed class ClassDescriptor(Method function) : Protected, IPyNativeDescriptor
    {
        public override string TypeName => "classmethod";
        public override object? GetAttr(string name) => name == "__func__" ? function : Reflection(name);
        public object? Bind(PyClass owner, object? self)
        {
            object target = self is PyNamedTupleInstance record ? record.Class : self as PyClass ?? owner;
            return new Bound(function, target);
        }
    }

    private sealed class Bound(Method function, object receiver) : Protected, IPyNativeCallable
    {
        public override string TypeName => "method";
        public override object? GetAttr(string name) => name switch
        {
            "__self__" => receiver,
            "__func__" => function,
            "__class__" => PyType.Get("method"),
            "__eq__" or "__ne__" or "__hash__" => new BoundSlot(this, name),
            _ => function.GetAttr(name),
        };
        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            var withSelf = new object?[args.Length + 1]; withSelf[0] = receiver;
            Array.Copy(args, 0, withSelf, 1, args.Length);
            return function.Invoke(withSelf, names, values);
        }
        internal object Equal(object? other) => other is not Bound method ? PyNotImplemented.Instance
            : ReferenceEquals(function, method.Function) && (ReferenceEquals(receiver, method.Receiver) || PyHash.Equal(receiver, method.Receiver));
        internal long Hash()
        {
            long hash = PyHash.Of(function) ^ PyHash.Of(receiver);
            return hash == -1 ? -2 : hash;
        }
        private Method Function => function;
        private object Receiver => receiver;
        public override string PythonRepr() => $"<bound method {function.QualifiedName} of {PyRepr.Repr(receiver)}>";
    }

    private sealed class BoundSlot(Bound owner, string name) : Protected, IPyNativeCallable
    {
        public override string TypeName => "method-wrapper";
        public override object? GetAttr(string attribute) => attribute switch
        {
            "__name__" => name, "__self__" => owner, "__class__" => PyType.Get(TypeName), _ => Reflection(attribute),
        };
        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            int required = name == "__hash__" ? 0 : 1;
            if (args.Length != required || names.Length != 0)
                throw PyNamedTupleLayout.Error(new PyTypeError($"method.{name}() requires {required} positional arguments"));
            if (name == "__hash__") return owner.Hash();
            object equal = owner.Equal(args[0]);
            return name == "__ne__" && equal is bool boolean ? !boolean : equal;
        }
    }

    private sealed class FunctionGet(Method function) : Protected, IPyNativeCallable
    {
        private static readonly PySignature Signature = PySignature.Create("function.__get__", ["instance", "owner"], ["posonly", "posonly"], ["0", "1"]);
        public override string TypeName => "method-wrapper";
        public override object? GetAttr(string name) => name switch
        {
            "__name__" => "__get__", "__self__" => function, "__class__" => PyType.Get(TypeName), _ => Reflection(name),
        };
        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            object?[] bound = PyNamedTupleBinding.Bind(Signature, args, names, values, [null, PyNone.Instance]);
            if ((bound[0] is null or PyNone) && (bound[1] is null or PyNone))
                throw PyNamedTupleLayout.Error(new PyTypeError("__get__(None, None) is invalid"));
            return bound[0] is null or PyNone ? function : new Bound(function, bound[0]!);
        }
    }
}
