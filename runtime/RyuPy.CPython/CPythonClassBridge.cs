// CPython 跨域类继承桥接实现：负责类型校验、真实 PyTypeObject 派生创建、
// 描述符方法包装、object 基础槽委派、真实 super() 查找以及反向实例身份解析。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private bool classBridgeHelpersReady;
    private nint _wrapMethodFn;
    private nint _staticMethodFn;
    private nint _classMethodFn;
    private nint _propertyFn;
    private nint _objectNewFn;
    private nint _objectInitFn;
    private nint _objectSetAttrFn;
    private nint _objectDelAttrFn;
    private nint _superCreateFn;
    private readonly Dictionary<PyClass, PythonObject> _mixinMirrors = new();

    /// <inheritdoc />
    public bool IsType(IPyForeignObject value)
    {
        PythonObject foreign = RequireForeignObject(value);
        return PythonCall(() => Native.rp_is_type(foreign.Pointer) != 0);
    }

    /// <inheritdoc />
    public bool IsPlainType(object? typeObject)
    {
        if (typeObject is PyType builtin)
        {
            return string.Equals(builtin.Name, "type", StringComparison.Ordinal);
        }
        if (typeObject is PythonObject foreign)
        {
            if (!ReferenceEquals(foreign.Host, this)) return false;
            return PythonCall(() => Native.rp_is_plain_type(foreign.Pointer) != 0);
        }
        return false;
    }

    /// <inheritdoc />
    public IPyForeignObject CreateDerivedClass(
        PyClass compiledClass,
        string name,
        object?[] bases,
        IReadOnlyDictionary<string, object?> attributes
    ) => PythonCall(() =>
    {
        return FinishDerivedClass(compiledClass, bases, attributes);
    });

    /// <inheritdoc />
    public void SetClassAttribute(IPyForeignObject foreignClass, PyClass ownerClass, string name, object? value) =>
        PythonCall(() =>
        {
            EnsureClassBridgeHelpers();
            PythonObject target = RequireForeignObject(foreignClass);
            nint converted = ConvertClassMemberOwned(ownerClass, name, value);
            try
            {
                Native.rp_decref(Check(Native.rp_op(3, target.Pointer, converted, s: name)));
            }
            finally
            {
                Native.rp_decref(converted);
            }
            return 0;
        });

    /// <inheritdoc />
    public void DeleteClassAttribute(IPyForeignObject foreignClass, string name) => PythonCall(() =>
    {
        PythonObject target = RequireForeignObject(foreignClass);
        Native.rp_decref(Check(Native.rp_op(4, target.Pointer, s: name)));
        return 0;
    });

    /// <inheritdoc />
    public object? AllocateFromBaseObject(
        IPyForeignObject foreignClass,
        object?[] extraArgs,
        string[] kwNames,
        object?[] kwValues
    ) => PythonCall(() =>
    {
        EnsureClassBridgeHelpers();
        object?[] allArgs = PrependArg(foreignClass, extraArgs);
        return FromOwned(CallHelperRaw(_objectNewFn, allArgs, kwNames, kwValues));
    });

    /// <inheritdoc />
    public void InitializeFromBaseObject(
        IPyForeignObject foreignInstance,
        object?[] extraArgs,
        string[] kwNames,
        object?[] kwValues
    ) => PythonCall(() =>
    {
        EnsureClassBridgeHelpers();
        object?[] allArgs = PrependArg(foreignInstance, extraArgs);
        Native.rp_decref(CallHelperRaw(_objectInitFn, allArgs, kwNames, kwValues));
        return 0;
    });

    /// <inheritdoc />
    public void SetAttrFromBaseObject(IPyForeignObject foreignInstance, string name, object? value) => PythonCall(() =>
    {
        EnsureClassBridgeHelpers();
        Native.rp_decref(CallHelperRaw(_objectSetAttrFn, [foreignInstance, name, value], null, null));
        return 0;
    });

    /// <inheritdoc />
    public void DelAttrFromBaseObject(IPyForeignObject foreignInstance, string name) => PythonCall(() =>
    {
        EnsureClassBridgeHelpers();
        Native.rp_decref(CallHelperRaw(_objectDelAttrFn, [foreignInstance, name], null, null));
        return 0;
    });

    /// <inheritdoc />
    public bool Contains(IPyForeignObject foreignInstance, object? value) => PythonCall(() =>
    {
        PythonObject container = RequireForeignObject(foreignInstance);
        nint itemPtr = ToPython(value);
        try
        {
            int result = Native.rp_contains(container.Pointer, itemPtr);
            if (result < 0) ThrowError();
            return result != 0;
        }
        finally
        {
            Native.rp_decref(itemPtr);
        }
    });

    /// <inheritdoc />
    public object? Dir(IPyForeignObject foreignObject) => PythonCall(() =>
    {
        PythonObject target = RequireForeignObject(foreignObject);
        return FromOwned(Native.rp_dir(target.Pointer));
    });

    private bool TryResolveForeignDerivedInstance(nint pointer, out PyInstance? derived)
    {
        if (Native.rp_is_type(pointer) == 0)
        {
            nint typePtr = Native.rp_borrowed_type(pointer);
            if (typePtr != 0
                && objects.TryGetValue(typePtr, out WeakReference<object>? typeWeak)
                && typeWeak.TryGetTarget(out object? typeObj)
                && typeObj is PyClass ownerClass
                && PyForeignClassBridge.GetForeignClass(ownerClass) is PythonObject { IsDisposed: false })
            {
                var fi = new PythonObject(this, pointer);
                PyInstance instance = PyForeignClassBridge.CreateForeignInstance(ownerClass, fi);
                objects[pointer] = new WeakReference<object>(instance);
                derived = instance;
                return true;
            }
        }
        derived = null;
        return false;
    }

    private PythonObject RequireForeignObject(IPyForeignObject value)
    {
        if (value is not PythonObject foreign)
        {
            throw new PyTypeError("expected CPython foreign object");
        }
        if (!ReferenceEquals(foreign.Host, this))
        {
            throw new PyTypeError("objects belong to different interpreters");
        }
        return foreign;
    }

    private nint ResolveBasePointer(object? value) => value switch
    {
        PyClass cls => ResolveForeignTypePointer(cls),
        _ => ToPython(value),
    };

    private nint ResolveForeignTypePointer(PyClass cls)
    {
        if (PyForeignClassBridge.GetForeignClass(cls) is PythonObject existing)
        {
            if (!ReferenceEquals(existing.Host, this))
            {
                throw new PyTypeError("objects belong to different interpreters");
            }
            return Native.rp_incref(existing.Pointer);
        }
        if (_mixinMirrors.TryGetValue(cls, out PythonObject? cached) && !cached.IsDisposed)
        {
            return Native.rp_incref(cached.Pointer);
        }

        // 纯 RyuPy 基类作为外部派生类的 MI mixin 时，为其在 CPython 生成真实镜像类型参与 MRO。
        EnsureClassBridgeHelpers();
        var mirror = (PythonObject)FinishDerivedClass(cls, PyForeignClassBridge.GetDirectBases(cls), PyForeignClassBridge.GetClassAttributes(cls));
        _mixinMirrors[cls] = mirror;
        return Native.rp_incref(mirror.Pointer);
    }

    private nint ConvertClassMemberOwned(PyClass ownerClass, string name, object? value)
    {
        switch (value)
        {
            case PyFunction fn:
                return WrapCallableOwned(ownerClass, fn, bindContext: true, staticBinding: string.Equals(name, "__new__", StringComparison.Ordinal));
            case PyStaticMethod sm:
            {
                nint inner = WrapCallableOwned(ownerClass, sm.Value, bindContext: string.Equals(name, "__new__", StringComparison.Ordinal));
                try
                {
                    return CallRawArgs(_staticMethodFn, inner);
                }
                finally
                {
                    Native.rp_decref(inner);
                }
            }
            case PyClassMethod cm:
            {
                nint inner = WrapCallableOwned(ownerClass, cm.Value, bindContext: true);
                try
                {
                    return CallRawArgs(_classMethodFn, inner);
                }
                finally
                {
                    Native.rp_decref(inner);
                }
            }
            case PyProperty prop:
            {
                nint fget = WrapCallableOwned(ownerClass, prop.Getter, bindContext: true);
                nint fset = 0;
                nint fdel = 0;
                try
                {
                    fset = WrapCallableOwned(ownerClass, prop.Setter, bindContext: true);
                    fdel = WrapCallableOwned(ownerClass, prop.Deleter, bindContext: true);
                    return CallRawArgs(_propertyFn, fget, fset, fdel);
                }
                finally
                {
                    Native.rp_decref(fget);
                    if (fset != 0) Native.rp_decref(fset);
                    if (fdel != 0) Native.rp_decref(fdel);
                }
            }
            default:
                return ToPython(value);
        }
    }

    private nint WrapCallableOwned(PyClass ownerClass, object? value, bool bindContext, bool staticBinding = false)
    {
        if (value is null or PyNone) return Native.rp_none();
        if (value is PyFunction fn)
        {
            string qualname = TryGetQualname(fn, $"{ownerClass.Name}.{fn.Name}");
            var target = new ForeignMethodTarget(ownerClass, fn, bindContext);
            nint callable = nativeMethodsEnabled ? CreateNativeMethodOwned(target, fn.Name, qualname, staticBinding)
                : CallHelperRaw(_wrapMethodFn, [target, fn.Name, qualname], null, null);
            try { target.CallableReference = new PythonObject(this, Check(Native.rp_callable_reference(callable))); return callable; }
            catch { Native.rp_decref(callable); throw; }
        }
        return ToPython(value);
    }

    private static string TryGetQualname(PyFunction fn, string fallback)
    {
        try
        {
            return PyObject.GetAttr(fn, "__qualname__") as string ?? fallback;
        }
        catch (PyNotImplementedError)
        {
            return fallback;
        }
    }

    private nint CallHelperRaw(nint helper, object?[] args, string[]? kwNames, object?[]? kwValues)
    {
        nint tuple = MakeTuple(args);
        nint keywords = 0;
        try
        {
            if (kwNames is { Length: > 0 } && kwValues is not null)
            {
                keywords = Check(Native.rp_op(16));
                for (int i = 0; i < kwNames.Length; i++)
                {
                    nint kwVal = ToPython(kwValues[i]);
                    try
                    {
                        Native.rp_decref(Check(Native.rp_op(17, keywords, kwVal, s: kwNames[i])));
                    }
                    finally
                    {
                        Native.rp_decref(kwVal);
                    }
                }
            }
            return Check(Native.rp_op(5, helper, tuple, keywords));
        }
        finally
        {
            Native.rp_decref(tuple);
            if (keywords != 0) Native.rp_decref(keywords);
        }
    }

    private nint CallRawArgs(nint callable, nint arg0)
    {
        nint tuple = Check(Native.rp_op(14, n: 1));
        try
        {
            Native.rp_decref(Check(Native.rp_op(15, tuple, arg0, n: 0)));
            return Check(Native.rp_op(5, callable, tuple));
        }
        finally
        {
            Native.rp_decref(tuple);
        }
    }

    private nint CallRawArgs(nint callable, nint arg0, nint arg1)
    {
        nint tuple = Check(Native.rp_op(14, n: 2));
        try
        {
            Native.rp_decref(Check(Native.rp_op(15, tuple, arg0, n: 0)));
            Native.rp_decref(Check(Native.rp_op(15, tuple, arg1, n: 1)));
            return Check(Native.rp_op(5, callable, tuple));
        }
        finally
        {
            Native.rp_decref(tuple);
        }
    }

    private nint CallRawArgs(nint callable, nint arg0, nint arg1, nint arg2)
    {
        nint tuple = Check(Native.rp_op(14, n: 3));
        try
        {
            Native.rp_decref(Check(Native.rp_op(15, tuple, arg0, n: 0)));
            Native.rp_decref(Check(Native.rp_op(15, tuple, arg1, n: 1)));
            Native.rp_decref(Check(Native.rp_op(15, tuple, arg2, n: 2)));
            return Check(Native.rp_op(5, callable, tuple));
        }
        finally
        {
            Native.rp_decref(tuple);
        }
    }

    private void EnsureClassBridgeHelpers()
    {
        if (classBridgeHelpersReady) return;
        nint module = Check(Native.rp_op(1, s: "_ryupy_imports"));
        try
        {
            _wrapMethodFn = nativeMethodsEnabled ? 0 : Check(Native.rp_op(2, module, s: "_rp_wrap_method"));
            _staticMethodFn = Check(Native.rp_descriptor_type(2));
            _classMethodFn = Check(Native.rp_descriptor_type(3));
            _propertyFn = Check(Native.rp_descriptor_type(1));
            // 缓存真实 object 原生描述符；参数绑定和错误仍由 CPython 槽位处理，
            // 四条基础操作不再建立转发用 Python 函数帧。
            nint objectType = Check(Native.rp_builtin("object"));
            try
            {
                _objectNewFn = Check(Native.rp_op(2, objectType, s: "__new__"));
                _objectInitFn = Check(Native.rp_op(2, objectType, s: "__init__"));
                _objectSetAttrFn = Check(Native.rp_op(2, objectType, s: "__setattr__"));
                _objectDelAttrFn = Check(Native.rp_op(2, objectType, s: "__delattr__"));
            }
            finally { Native.rp_decref(objectType); }
            _superCreateFn = Check(Native.rp_super_type());
            classBridgeHelpersReady = true;
        }
        finally
        {
            Native.rp_decref(module);
        }
    }

    private void ReleaseClassBridgeResources()
    {
        static void ReleaseRef(ref nint ptr)
        {
            if (ptr != 0)
            {
                Native.rp_decref(ptr);
                ptr = 0;
            }
        }
        foreach (ClassDefinition definition in classDefinitions.Values.ToArray()) definition.Dispose();
        ReleaseRef(ref _wrapMethodFn);
        ReleaseRef(ref _staticMethodFn);
        ReleaseRef(ref _classMethodFn);
        ReleaseRef(ref _propertyFn);
        ReleaseRef(ref _objectNewFn);
        ReleaseRef(ref _objectInitFn);
        ReleaseRef(ref _objectSetAttrFn);
        ReleaseRef(ref _objectDelAttrFn);
        ReleaseRef(ref _superCreateFn);
        foreach (PythonObject mirror in _mixinMirrors.Values)
        {
            if (!mirror.IsDisposed) objects.Remove(mirror.Pointer);
            mirror.Dispose();
        }
        _mixinMirrors.Clear();
    }

    private static object?[] PrependArg(object? first, object?[] rest)
    {
        var all = new object?[rest.Length + 1];
        all[0] = first;
        if (rest.Length > 0) Array.Copy(rest, 0, all, 1, rest.Length);
        return all;
    }

    /// <summary>在 CPython 侧触发 RyuPy 方法体时恢复 <see cref="PyMethodContext"/> 以支持零参 <c>super()</c>。</summary>
    private sealed class ForeignMethodTarget(PyClass owner, PyFunction function, bool bindContext) : IPyNativeCallable
    {
        internal PyClass Owner => owner;
        internal PyFunction Function => function;
        // owned weakref：调用活跃时解析真实 wrapper，不强持其 referent。
        internal PythonObject? CallableReference { get; set; }
        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            if (bindContext && args.Length > 0)
            {
                object?[] rest = args.Length == 1 ? [] : args[1..];
                return PyObject.InvokeBound(function, owner, args[0], rest, names, values);
            }
            return function.Invoke(args, names, values);
        }
    }
}
