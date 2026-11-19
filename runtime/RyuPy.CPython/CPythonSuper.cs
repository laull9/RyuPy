// 真实 CPython super 与跨域类关系：完整外部 C3 为唯一查找权威，Mixin 使用已有镜像身份。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    /// <inheritdoc />
    public void SynchronizeMirrorAttribute(PyClass ownerClass, string name, object? value, bool deleting) => PythonCall(() =>
    {
        if (!_mixinMirrors.TryGetValue(ownerClass, out PythonObject? mirror) || mirror.IsDisposed) return 0;
        if (name is "__bases__" or "__mro__")
            throw new PyNotImplementedError("foreign Mixin structural mutation is not supported");
        // 外部操作先成功，再由 Runtime 发布本地值；出错时两端都保留旧命名空间。
        if (deleting) DeleteClassAttribute(mirror, name);
        else SetClassAttribute(mirror, ownerClass, name, value);
        return 0;
    });

    /// <inheritdoc />
    public bool IsSubclass(object? child, object? parent) => PythonCall(() =>
    {
        // 不相关本地类没有镜像时也先验证外部 arg1，非法实例不能被误报为 False。
        if (child is PythonObject candidate && ReferenceEquals(candidate.Host, this) && Native.rp_is_type(candidate.Pointer) == 0)
            throw new PyTypeError("issubclass() arg 1 must be a class");
        nint childPtr = TryExistingTypePointer(child);
        if (childPtr == 0)
        {
            if (parent is PythonObject target && ReferenceEquals(target.Host, this) && Native.rp_is_type(target.Pointer) == 0)
                throw new PyTypeError("issubclass() arg 2 must be a class or tuple of classes");
            return false;
        }
        nint parentPtr = 0;
        try
        {
            parentPtr = TryExistingTypePointer(parent);
            if (parentPtr == 0) return false;
            int result = Native.rp_is_subclass(childPtr, parentPtr);
            if (result < 0) ThrowError();
            return result != 0;
        }
        finally
        {
            Native.rp_decref(childPtr);
            if (parentPtr != 0) Native.rp_decref(parentPtr);
        }
    });

    /// <inheritdoc />
    public bool IsInstance(IPyForeignObject value, PyClass target)
    {
        // 存活检查是CLR宿主合同；先于carrier回调与无关镜像查询，保留原异常类型和副作用顺序。
        if (value is PythonObject candidate && ReferenceEquals(candidate.Host, this)) _ = candidate.Pointer;
        return PythonCall(() =>
        {
            if (value is not PythonObject instance || !ReferenceEquals(instance.Host, this)) return false;
            // 进入域后仍核验当前状态；查询不相关类型也不能消费已失效的别名。
            nint pointer = instance.Pointer;
            nint type = TryExistingTypePointer(target);
            if (type == 0) return false;
            try
            {
                nint result = Check(Native.rp_op(27, pointer, type));
                try { int truth = Native.rp_truth(result); ThrowIfError(); return truth != 0; }
                finally { Native.rp_decref(result); }
            }
            finally { Native.rp_decref(type); }
        });
    }

    /// <inheritdoc />
    public object? GetSuperAttribute(object start, object? receiver, string name) => PythonCall(() =>
    {
        EnsureClassBridgeHelpers();
        nint startPtr = ResolveBasePointer(start);
        nint receiverPtr = 0;
        nint superPtr = 0;
        try
        {
            receiverPtr = receiver is null ? Native.rp_none() : ToPython(receiver);
            superPtr = CallSuperOwned(startPtr, receiverPtr);
            return FromOwned(Native.rp_op(2, superPtr, s: name));
        }
        finally
        {
            Native.rp_decref(startPtr);
            if (receiverPtr != 0) Native.rp_decref(receiverPtr);
            if (superPtr != 0) Native.rp_decref(superPtr);
        }
    });

    /// <inheritdoc />
    public object? CreateSuper(object start, object? receiver) => PythonCall(() =>
    {
        EnsureClassBridgeHelpers();
        nint startPtr = ResolveBasePointer(start);
        nint receiverPtr = 0;
        try
        {
            receiverPtr = receiver is null ? Native.rp_none() : ToPython(receiver);
            return (PySuper)FromOwned(CallSuperOwned(startPtr, receiverPtr))!;
        }
        finally
        {
            Native.rp_decref(startPtr);
            if (receiverPtr != 0) Native.rp_decref(receiverPtr);
        }
    });

    // 零参 super 的词法类与接收者已经由托管 PyMethodContext 冻结。
    // 显式调用真实 PySuper_Type，None 接收者沿旧合同建立单参未绑定代理。
    private nint CallSuperOwned(nint start, nint receiver) => Native.rp_is_none(receiver) != 0
        ? CallRawArgs(_superCreateFn, start)
        : CallRawArgs(_superCreateFn, start, receiver);

    // 关系查询只消费真实类与已建立的镜像；查询不相关普通类不会创建新的外部类型。
    private nint TryExistingTypePointer(object? value) => value switch
    {
        PyClass cls when PyForeignClassBridge.GetForeignClass(cls) is PythonObject actual && ReferenceEquals(actual.Host, this)
            => Native.rp_incref(actual.Pointer),
        PyClass cls when _mixinMirrors.TryGetValue(cls, out PythonObject? mirror) && !mirror.IsDisposed
            => Native.rp_incref(mirror.Pointer),
        PythonObject foreign when ReferenceEquals(foreign.Host, this) => Native.rp_incref(foreign.Pointer),
        PyNativeType native when ReferenceEquals(native, PyLexicalSuper.Type) => Check(Native.rp_super_type()),
        PyType builtin => Check(Native.rp_builtin(builtin.Name)),
        _ => 0,
    };
}
