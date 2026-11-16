// Runtime类定义冷元数据：真实来源只读一次；内部未知来源保持旧构造行为。
namespace RyuPy.Runtime;

internal static class PyClassNamespaceMetadata
{
    internal static bool TryGetModule(out object? module)
    {
        if (!PyTraceback.TryGetClassDefinitionModule(out module)) return false;
        // getter的任意异常原传；只有明确哨兵可判缺名，不按异常类型/消息猜测。
        if (ReferenceEquals(module, PyUnbound.Instance))
            throw PyNamedTupleLayout.Unsupported("class globals name fallback is not supported");
        return true;
    }

    internal static void Capture(PyClass cls)
    {
        if (PyForeignClassConstruction.TryGet(cls, out IPyForeignClassDefinition definition))
        {
            // LOAD_NAME 先执行真实 mapping.__getitem__；Contains/Get 共用同一次冻结结果。
            try
            {
                object? module;
                if (definition.Contains("__name__")) module = definition.Get("__name__");
                else if (!PyTraceback.TryGetClassDefinitionModule(out module)) return;
                if (ReferenceEquals(module, PyUnbound.Instance))
                    module = PyOps.LoadDeletedModuleName(module, "__name__");
                cls.SetAttr("__module__", module ?? PyNone.Instance);
                cls.SetAttr("__qualname__", cls.Name);
            }
            catch { PyForeignClassConstruction.Abort(cls); throw; }
            return;
        }
        if (!TryGetModule(out object? ordinaryModule)) return;
        cls.Attributes["__module__"] = ordinaryModule ?? PyNone.Instance;
        cls.Attributes["__qualname__"] = cls.Name;
    }

    internal static void ValidateQualname(PyClass cls, object? value, bool typed)
    {
        if (value is string) return;
        if (value is PyStringInstance)
            throw PyNamedTupleLayout.Unsupported("class qualname string subclass is not supported");
        string type = PyRepr.TypeName(value);
        string message = typed
            ? $"can only assign string to {cls.Name}.__qualname__, not '{type}'"
            : $"type __qualname__ must be a str, not {type}";
        throw PyNamedTupleLayout.Error(new PyTypeError(message));
    }
}
