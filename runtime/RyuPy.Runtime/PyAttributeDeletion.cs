// delattr 的有限公开绑定与删除语法共有守卫：只在删除时检查，不新增属性读写热路或实例字段。
// 复用现有槽清理、property deleter和类版本失效；未知hook/descriptor不能静默退回删实例槽。
namespace RyuPy.Runtime;

internal static class PyAttributeDeletion
{
    internal static object Invoke(object?[] args, string[] names)
    {
        try
        {
            if (names.Length != 0) throw new PyTypeError("delattr() takes no keyword arguments");
            if (args.Length != 2) throw new PyTypeError($"delattr expected 2 arguments, got {args.Length}");
            // 仅已封闭的str子类可剥payload；其自定义eq/hash已由String布局守卫拒绝。
            if (!PyStringLayout.TryGet(args[1], out string name))
                throw new PyTypeError($"attribute name must be string, not '{PyRepr.TypeName(args[1])}'");
            PyObject.DelAttr(args[0], name);
            return PyNone.Instance;
        }
        catch (PyBaseException error)
        {
            // 保用户异常身份与已有帧；仅按现有规则补缺失帧及尚未设置的context。
            PyErrors.PrepareRaise(error);
            throw;
        }
    }

    internal static void Guard(object? value, string name)
    {
        if (value is PyModule module && !PyModuleInstances.IsInstance(module))
            throw Unsupported("module attribute deletion requires global-slot synchronization");
        if (value is PyType type)
            throw Error(new PyTypeError($"cannot set '{name}' attribute of immutable type '{type.Name}'"));
        // __qualname__交给真实函数元数据处理TypeError；typing.IO仍由自身修改守卫拒绝。
        if (value is PyFunction function && name is "__class__" or "__defaults__" or "__kwdefaults__"
            or "__code__" or "__closure__" or "__type_params__")
        {
            // 仅B2实例函数的closure删除进入readonly元数据拒绝；其它删除不加名称比较。
            if (name == "__closure__" && function.Body.Target is PyFunctionOwner) return;
            throw Unsupported($"function {name} deletion is not supported");
        }
        if (value is PyClass cls)
        {
            // 已核原生类及IO派生的守卫仍处理__module__；其它反射不能被删普通字典伪装。
            if (name is "__class__" or "__name__" or "__qualname__" or "__dict__" or "__bases__" or "__base__" or "__mro__" or "__doc__"
                || name == "__module__" && cls.NativeDefinitionModule is null && !cls.IsTypingIoFamily)
                throw Unsupported($"class {name} deletion is not supported");
            return; // 类自身的__delattr__控制其实例，不是该类的元类删除协议。
        }
        PyClass? owner = value is PyInstance instance ? instance.Class
            : value is PyBaseException exception ? exception.PythonClass : null;
        if (owner is null) return;
        if (value is PyInstance ordinary)
        {
            if (owner.IsTypingGenericRoot) PyTypingGenericMetadata.MutateInstance(owner, name, deleting: true);
            // 原删除入口已有的布局/frozen拒绝优先级保持；不让新增NI覆盖FrozenInstanceError。
            PyStringLayout.GuardInstanceMutation(ordinary, name);
            PyListLayout.GuardInstanceMutation(ordinary, name);
            PyDictLayout.GuardInstanceMutation(ordinary, name);
            PyEnum.GuardMemberMutation(ordinary, name);
            owner.CheckDataclassWrite(name, deleting: true);
        }
        if (owner.TryLookup("__delattr__", out _, out _))
            throw Unsupported("custom __delattr__ is not supported");
        owner.TryLookup(name, out _, out object? raw);
        if (raw is PyProperty property)
        {
            if (value is not PyInstance || property.Deleter is not null and not PyFunction)
                throw Unsupported("this property deleter shape is not supported");
            return;
        }
        PyClass? descriptorClass = raw is PyInstance descriptor ? descriptor.Class
            : raw is PyBaseException descriptorException ? descriptorException.PythonClass : null;
        // 已知非数据native包装允许删自身遮蔽槽；tuple字段等真实数据descriptor不能误放行。
        if (raw is PyInstanceSlotDescriptor slot && owner.AllowsSlotDescriptor(name, slot)) return;
        if (raw is IPyNativeDataDescriptor || descriptorClass is not null
            && (descriptorClass.TryLookup("__delete__", out _, out _)
                || descriptorClass.TryLookup("__set__", out _, out _)))
            throw Unsupported("custom deletion descriptors are not supported");
        // dict用户类链的普通同名项允许删除自身遮蔽字段；无类项仍是域外布局变更。
        if (name == "__class__" && value is PyDictInstance && owner.TryLookup(name, out _, out _)) return;
        if (name == "__dict__" && owner.HasSlotLayout) return;
        if (name is "__dict__" or "__class__") throw Unsupported($"instance {name} deletion is not supported");
    }

    private static PyNotImplementedError Unsupported(string message) => Error(new PyNotImplementedError(message));
    private static T Error<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}
