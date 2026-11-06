// NamedTuple真实函数根、功能式转发及有限单父协议；不伪造隐藏metaclass。
// 显式mro_entries返回仍NI；类语法只消费已声明的原根协议。
using System;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

internal static partial class PyTypingNamedTuple
{
    private sealed record RootFunctions(PyFunction Factory, PyFunction MroEntries);
    private static readonly Lazy<RootFunctions> Functions = new(BuildRoot);
    private sealed class Definition(object originalBase)
    {
        internal readonly object OriginalBase = originalBase;
        internal bool Finishing;
        internal bool Failed;
    }
    private static readonly ConditionalWeakTable<PyClass, Definition> Definitions = new();

    internal static PyFunction Root => Functions.Value.Factory;
    // 普通类基值检查不触发根初始化或额外对象分配。
    internal static bool IsRoot(object? value) => Functions.IsValueCreated && ReferenceEquals(value, Root);

    private static RootFunctions BuildRoot()
    {
        var signature = PySignature.Create("NamedTuple", ["typename", "fields", "kwargs"],
            ["posonly", "posonly", "varkw"], ["0", "1", "0"]);
        var root = new PyFunction("NamedTuple", signature, PyTypingNamedTupleFunctional.Invoke,
            [null, PyNone.Instance, null], false);
        var mro = PyNativeFunctions.Create("_namedtuple_mro_entries", ["bases"], [null], 1, args =>
        {
            if (args[0] is not PyTuple { Length: 1 } bases || !ReferenceEquals(bases.Items[0], Root))
                throw PyNamedTupleLayout.Unsupported("NamedTuple mro_entries currently requires its original single base");
            CheckCurrentRoot();
            throw PyNamedTupleLayout.Unsupported("explicit NamedTuple mro_entries result is not supported");
        });
        foreach (PyFunction function in new[] { root, mro })
        {
            function.SetMetadata("__module__", "typing");
            function.SetMetadata("__qualname__", function.Name);
        }
        root.SetMetadata("__mro_entries__", mro);
        return new RootFunctions(root, mro);
    }

    internal static PyClass BeginDefinition(string name, object? originalBase)
    {
        if (!IsRoot(originalBase)) throw PyNamedTupleLayout.Unsupported("NamedTuple definition requires the original root");
        object? mro;
        try { mro = Root.GetMetadata("__mro_entries__"); }
        catch (PyAttributeError) { throw PyNamedTupleLayout.Unsupported("NamedTuple root without its original mro_entries is not supported"); }
        if (!ReferenceEquals(mro, Functions.Value.MroEntries))
            throw PyNamedTupleLayout.Unsupported("replaced NamedTuple mro_entries is not supported");
        CheckCurrentRoot();
        RejectTypingOverride("_NamedTuple");
        if (!PyClassNamespaceMetadata.TryGetModule(out object? module))
            throw PyNamedTupleLayout.Unsupported("NamedTuple class has no supported globals origin");
        PyClass shell = PyTypingNamedTupleShell.Begin(name, module);
        Definitions.Add(shell, new Definition(originalBase!));
        return shell;
    }

    private static void CheckCurrentRoot()
    {
        object? current = PyObject.GetAttr(PyModules.Import("typing"), "NamedTuple");
        if (ReferenceEquals(current, Root)) return;
        // 当前模块替换值若有未知相等协议，不能以identity不等吞CP membership副作用。
        if (current is null or PyNone or PyBaseObject or PyType or PyClass or PyNativeType
            or PyFunction or string or bool or long or int or double)
            throw PyNamedTupleLayout.Error(new PyAssertionError(""));
        throw PyNamedTupleLayout.Unsupported("NamedTuple current root comparison protocol is not supported");
    }

    internal static void RejectTypingOverride(string name)
    {
        if (PyTyping.CreateAttributes().ContainsKey(name))
            throw PyNamedTupleLayout.Unsupported($"NamedTuple after setting typing.{name} is not supported");
    }

    internal static bool TryFinishDefinition(object value)
    {
        if (value is not PyClass shell || !Definitions.TryGetValue(shell, out Definition? definition)) return false;
        if (definition.Finishing || definition.Failed)
            throw PyNamedTupleLayout.Unsupported("NamedTuple definition cannot be finalized again");
        // 独立Finishing不改B1.Pending，唯一Shell.Prepare仍只调用一次。
        definition.Finishing = true;
        try
        {
            FinishDefinition(shell, definition.OriginalBase);
            Definitions.Remove(shell);
            return true;
        }
        catch
        {
            definition.Failed = true;
            PyTypingNamedTupleShell.Fail(shell);
            throw; // B0已准备的Python错误原样传播，不二次包装现场。
        }
    }
}
