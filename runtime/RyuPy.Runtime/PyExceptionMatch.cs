// 动态except的纯类型匹配入口：先验证全部tuple成员，再按真实类身份及祖先匹配。
using System;

namespace RyuPy.Runtime;

public static partial class PyErrors
{
    /// <summary>匹配已捕获的同一异常；调用者须先建立原异常的处理上下文。</summary>
    public static bool MatchDynamicException(object original, object? typeValue)
    {
        // 原异常来自catch槽；不把CLR异常或普通值默认为Python异常。
        if (original is not PyBaseException exception)
            throw new ArgumentException("dynamic exception matching requires a captured Python exception", nameof(original));
        return PyExceptionMatch.Match(exception, typeValue);
    }
}

internal static partial class PyExceptionMatch
{
    internal static bool Match(PyBaseException original, object? target)
    {
        if (target is PyTuple tuple)
        {
            // Python要求全成员验证先于匹配；首项命中也不能隐藏非法后项。
            // PyNamedTupleInstance继承PyTuple，直接读取真实存储，不调用用户迭代器。
            foreach (object? member in tuple.Items) Validate(member);
            foreach (object? member in tuple.Items)
                if (MatchesValidated(original, member!)) return true;
            return false;
        }
        Validate(target);
        return MatchesValidated(original, target!);
    }

    private static void Validate(object? target)
    {
        if (target is IPyForeignObject { IsExceptionClass: true }) return;
        if (target is PyType type && NativeTargets.ContainsKey(type)) return;
        if (target is PyClass cls && HasBuiltinAncestor(cls, "BaseException")) return;
        // 嵌套tuple、异常实例和普通同名类均非法。只有新错误需准备帧及ambient。
        throw (PyTypeError)PyErrors.PrepareRaise(new PyTypeError(
            "catching classes that do not inherit from BaseException is not allowed"));
    }

    private static bool MatchesValidated(PyBaseException original, object target)
    {
        if (target is IPyForeignObject foreign) return foreign.MatchesException(original);
        if (target is PyClass cls)
            return original.PythonClass is { } actual && HasClassAncestor(actual, cls);
        NativeTarget native = NativeTargets[(PyType)target];
        // 绑定了PythonClass的异常以真实Python祖先为准，不以CLR布局或显示名替代。
        return original.PythonClass is { } pythonClass
            ? HasBuiltinAncestor(pythonClass, native.Name)
            : native.Accepts(original);
    }

    private static bool HasClassAncestor(PyClass actual, PyClass target)
    {
        PyClass? slow = actual, fast = actual;
        for (PyClass? current = actual; current is not null; current = current.BaseClass)
        {
            if (ReferenceEquals(current, target)) return true;
            // 冷路径也避免每次分配集合；快慢指针防御损坏的内部继承环。
            slow = slow?.BaseClass;
            fast = fast?.BaseClass?.BaseClass;
            if (fast is not null && ReferenceEquals(slow, fast)) return false;
        }
        return false;
    }

    private static bool HasBuiltinAncestor(PyClass actual, string target)
    {
        PyClass? slow = actual, fast = actual;
        for (PyClass? current = actual; current is not null; current = current.BaseClass)
        {
            // BuiltinBase是已构建类的内部祖先标识，不读取可变Name/__module__等属性。
            if (current.BuiltinBase is { } builtin && PyExceptionHierarchy.IsKnown(builtin)
                && PyExceptionHierarchy.IsSubclass(builtin, target)) return true;
            slow = slow?.BaseClass;
            fast = fast?.BaseClass?.BaseClass;
            if (fast is not null && ReferenceEquals(slow, fast)) return false;
        }
        return false;
    }
}
