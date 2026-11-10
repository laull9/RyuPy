// 类型级 __index__ 冷槽取得与调用；不负责API的None、结果宽度、警告或缺槽错误。
namespace RyuPy.Runtime;

internal static class PyIndexSlot
{
    // 缺槽返回false，存在但不可调用仍抛错；用户getter和正文异常原样传播。
    internal static bool TryInvoke(object? value, out object? result)
    {
        PyClass? cls = PythonClass(value);
        if (cls is not null)
        {
            cls.GuardInheritedGenericProtocol("__index__");
            if (!cls.TryLookup("__index__", out PyClass owner, out object? raw))
            {
                result = null;
                return false;
            }
            result = InvokeIndexSlot(value!, cls, owner, raw);
            return true;
        }
        // 保留已有native特殊槽域，不把普通实例字段当作类型槽。
        if (value is PyNativeObject && PyObject.TryMethod(value, "__index__", [], out result)) return true;
        result = null;
        return false;
    }

    private static PyClass? PythonClass(object? value) => value switch
    {
        PyInstance instance => instance.Class,
        PyBaseException exception => exception.PythonClass,
        _ => null,
    };

    // __index__按实际类型链取槽，不接受实例遮蔽；上层普通描述符绑定与下层raw Get严格分开。
    private static object? InvokeIndexSlot(object self, PyClass actualClass, PyClass owner, object? raw)
    {
        if (raw is PyFunction) return PyObject.InvokeBound(raw, owner, self, [], [], []);
        if (raw is PyStaticMethod staticMethod)
        {
            CheckCallable(staticMethod.Value);
            return PyObject.InvokeBound(raw, owner, self, [], [], []);
        }
        if (raw is PyClassMethod classMethod)
        {
            CheckCallable(classMethod.Value);
            return PyObject.InvokeBound(raw, owner, self, [], [], []);
        }
        object? callable;
        if (raw is PyProperty property)
        {
            object? getter = property.Getter;
            if (getter is null or PyNone)
                throw Own(new PyAttributeError($"property '__index__' of '{actualClass.Name}' object has no getter"));
            // 普通getter函数保已有super上下文；其它getter是raw callable，显式接收self。
            callable = getter is PyFunction
                ? PyObject.InvokeBound(getter, owner, self, [], [], []) : CallRaw(getter, [self]);
        }
        else if (PythonClass(raw) is { } descriptorClass)
        {
            descriptorClass.GuardInheritedGenericProtocol("__get__");
            if (descriptorClass.TryLookup("__get__", out PyClass getOwner, out object? get))
                // 普通函数仍收到三个参数，并保已有super的owner/self上下文；
                // 其它raw槽绝不再次descriptor绑定，static包装也必须接收完整三参。
                callable = get is PyFunction
                    ? PyObject.InvokeBound(get, getOwner, raw, [self, actualClass], [], [])
                    : CallRaw(get, [raw, self, actualClass]);
            else callable = PyObject.Bind(raw, owner, self);
        }
        else callable = PyObject.Bind(raw, owner, self);
        return CallRaw(callable, []);
    }

    // CallValue旧入口会解包classmethod；此窄帮助器的raw调用明确拒绝它和property，绝不调用getter。
    private static object? CallRaw(object? target, object?[] args)
    {
        target = CheckCallable(target);
        return PyOps.CallValue(target, args, [], []);
    }

    private static object? CheckCallable(object? target)
    {
        while (target is PyStaticMethod method) target = method.Value;
        bool bad = target switch
        {
            PyClassMethod or PyProperty => true,
            PyInstance instance => !instance.Class.TryLookup("__call__", out _, out _),
            IPyNativeCallable or PyFunction or PyBuiltinFunction or PyNativeType or PyClass
                or PyBoundMethod or PyBuiltinMethod or PyType => false,
            _ => true,
        };
        // 旧TypeName将两个wrapper统称内建方法；此处必须保CP的精确classmethod标签。
        if (bad) throw Own(new PyTypeError($"'{(target is PyClassMethod ? "classmethod" : PyRepr.TypeName(target))}' object is not callable"));
        return target;
    }

    // 只准备本层新造错误，不重包用户异常。
    private static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}
