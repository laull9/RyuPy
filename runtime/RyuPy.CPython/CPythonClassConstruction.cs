// C1 prepared namespace 的托管所有权与两阶段事务；普通类不增加字段，原生类型成功后才发布。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private readonly Dictionary<PyClass, ClassDefinition> classDefinitions = new();

    /// <inheritdoc />
    public IPyForeignClassDefinition PrepareDerivedClass(PyClass owner, object?[] bases) => PythonCall(() =>
    {
        EnsureClassBridgeHelpers();
        if (classDefinitions.ContainsKey(owner)) throw new PyRuntimeError("class definition is already active");
        nint name = ToPython(owner.Name), tuple = Check(Native.rp_op(14, n: bases.Length));
        try
        {
            for (int i = 0; i < bases.Length; i++)
            {
                nint parent = ResolveBasePointer(bases[i]);
                try { Native.rp_decref(Check(Native.rp_op(15, tuple, parent, n: i))); }
                finally { Native.rp_decref(parent); }
            }
            var definition = new ClassDefinition(this, owner, new PythonObject(this, Check(Native.rp_class_prepare(name, tuple))));
            classDefinitions.Add(owner, definition);
            return (IPyForeignClassDefinition)definition;
        }
        finally { Native.rp_decref(name); Native.rp_decref(tuple); }
    });

    private IPyForeignObject FinishDerivedClass(PyClass owner, object?[] bases, IReadOnlyDictionary<string, object?> attributes)
    {
        if (classDefinitions.TryGetValue(owner, out ClassDefinition? existing)) return existing.Finish();
        // 已完成普通 Mixin 的镜像没有新的 CIL 类体，仍走同一原生 prepare/finish 合同。
        var definition = (ClassDefinition)PrepareDerivedClass(owner, bases);
        try
        {
            foreach (var pair in attributes) definition.Set(pair.Key, pair.Value);
            return definition.Finish();
        }
        catch { definition.Dispose(); throw; }
    }

    private void PrepareConstructionCallback(object target, nint arguments)
    {
        if (target is not ForeignMethodTarget method || !classDefinitions.TryGetValue(method.Owner, out ClassDefinition? definition)) return;
        definition.AssociateReceiver(method.Function, arguments);
    }

    private sealed class ClassDefinition(CPythonHost host, PyClass owner, PythonObject session) : IPyForeignClassDefinition
    {
        private sealed record Stored(object? Value, PythonObject Native);
        private readonly Dictionary<string, Stored> written = new(StringComparer.Ordinal);
        private readonly Dictionary<string, object?> reads = new(StringComparer.Ordinal);
        private PythonObject? provisional;
        private bool closed, building;

        public bool Contains(string name) => host.PythonCall(() =>
        {
            RequireOpen();
            int status = Native.rp_class_lookup(session.Pointer, name, out nint value);
            if (status < 0) host.ThrowError();
            reads.Remove(name);
            if (status == 0) return false;
            // prepared mapping 仍执行一次真实 __getitem__；同一导出值回流时保留 CIL 原值身份。
            if (written.TryGetValue(name, out Stored? stored) && stored.Native.Pointer == value)
            { Native.rp_decref(value); reads[name] = stored.Value; }
            else reads[name] = host.FromOwned(value);
            return true;
        });

        public object? Get(string name)
        {
            RequireOpen();
            if (reads.Remove(name, out object? value)) return value;
            if (Contains(name) && reads.Remove(name, out value)) return value;
            throw new PyNameError($"name '{name}' is not defined");
        }

        public void Set(string name, object? value) => host.PythonCall(() =>
        {
            RequireOpen();
            var converted = new PythonObject(host, host.ConvertClassMemberOwned(owner, name, value));
            try
            {
                if (Native.rp_class_set(session.Pointer, name, converted.Pointer) < 0) host.ThrowError();
                if (written.Remove(name, out Stored? previous)) previous.Native.Dispose();
                written[name] = new Stored(value, converted); reads.Remove(name);
            }
            catch { converted.Dispose(); throw; }
            return 0;
        });

        public bool Delete(string name) => host.PythonCall(() =>
        {
            RequireOpen();
            // 删除协议直接执行一次，不为探测键存在而额外调用 __getitem__。
            if (Native.rp_class_delete(session.Pointer, name) < 0)
            {
                nint error = Native.rp_take_error();
                if (error == 0) host.ThrowError();
                object? converted = host.FromOwned(error);
                if (converted is PyKeyError) return false;
                throw (PyBaseException)converted!;
            }
            if (written.Remove(name, out Stored? previous)) previous.Native.Dispose();
            reads.Remove(name); return true;
        });

        internal void AssociateReceiver(PyFunction function, nint arguments)
        {
            if (!building || provisional is not null || Native.rp_length(arguments) == 0) return;
            nint receiver = host.Check(Native.rp_op(18, arguments, n: 0));
            try
            {
                foreach (var pair in written)
                {
                    object? callable = pair.Value.Value switch
                    {
                        PyClassMethod method => method.Value,
                        PyStaticMethod method => method.Value,
                        PyProperty property => property.Getter,
                        var value => value,
                    };
                    if (!ReferenceEquals(callable, function)) continue;
                    nint type = Native.rp_class_receiver(session.Pointer, receiver, pair.Key, pair.Value.Native.Pointer);
                    if (type == 0) { host.ThrowIfError(); continue; }
                    if (host.objects.TryGetValue(type, out WeakReference<object>? weak) && weak.TryGetTarget(out object? current) && !ReferenceEquals(current, owner))
                    { Native.rp_decref(type); throw new PyNotImplementedError("construction callback receiver already has another type identity"); }
                    provisional = new PythonObject(host, type);
                    PyForeignClassBridge.SetConstructingClass(owner, provisional);
                    host.objects[type] = new(owner);
                    return;
                }
            }
            finally { Native.rp_decref(receiver); }
        }

        internal IPyForeignObject Finish() => host.PythonCall(() =>
        {
            RequireOpen(); building = true;
            try
            {
                nint created = host.Check(Native.rp_class_finish(session.Pointer));
                PythonObject result;
                if (provisional is not null)
                {
                    if (created != provisional.Pointer)
                    { Native.rp_decref(created); throw new PyNotImplementedError("metaclass changed the provisional class identity"); }
                    Native.rp_decref(created); result = provisional; provisional = null;
                }
                else
                {
                    if (host.objects.TryGetValue(created, out WeakReference<object>? weak) && weak.TryGetTarget(out _))
                    { Native.rp_decref(created); throw new PyNotImplementedError("metaclass must create a new type identity"); }
                    result = new PythonObject(host, created);
                }
                host.objects[result.Pointer] = new(owner);
                Close(success: true);
                return (IPyForeignObject)result;
            }
            catch { Close(success: false); throw; }
            finally { building = false; }
        });

        public void Dispose()
        {
            if (closed) return;
            host.PythonCall(() => { Close(success: false); return 0; });
        }
        private void RequireOpen()
        {
            if (closed) throw new PyRuntimeError("class definition is no longer active");
        }
        private void Close(bool success)
        {
            if (closed) return;
            closed = true;
            host.classDefinitions.Remove(owner); PyForeignClassBridge.ForgetClassDefinition(owner);
            if (!success)
            {
                PyForeignClassBridge.SetConstructingClass(owner, null);
                if (provisional is not null) { host.objects.Remove(provisional.Pointer); provisional.Dispose(); provisional = null; }
            }
            foreach (Stored value in written.Values) value.Native.Dispose();
            written.Clear(); reads.Clear(); session.Dispose();
        }
    }
}
