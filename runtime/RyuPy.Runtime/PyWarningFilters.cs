using System;
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>有限warnings过滤器：六种action、类别/行匹配、重复抑制；修改过滤器即清除旧版本缓存。</summary>
internal static class PyWarningFilters
{
    internal sealed record Rule(string Action, object Category, BigInteger Line, string? Module = null);
    private static List<Rule> _rules = Defaults();
    private static readonly HashSet<object> Registry = new(PyKeyComparer.Instance);

    private static List<Rule> Defaults() =>
    [
        new("default", PyType.Get("DeprecationWarning"), 0, "__main__"),
        new("ignore", PyType.Get("DeprecationWarning"), 0),
        new("ignore", PyType.Get("PendingDeprecationWarning"), 0),
        new("ignore", PyType.Get("ImportWarning"), 0),
        new("ignore", PyType.Get("ResourceWarning"), 0),
    ];

    internal static bool IsCategory(object? category) => category switch
    {
        PyType type => PyExceptionHierarchy.IsSubclass(type.Name, "Warning"),
        PyClass cls => cls.IsSubclassOfBuiltin("Warning"),
        _ => false,
    };

    internal static bool Matches(object actual, object expected) => (actual, expected) switch
    {
        (PyType a, PyType b) => PyExceptionHierarchy.IsSubclass(a.Name, b.Name),
        (PyClass a, PyType b) => a.IsSubclassOfBuiltin(b.Name),
        (PyClass a, PyClass b) => a.IsSubclassOf(b),
        _ => false,
    };

    internal static void Simple(object? action, object? category, object? line, object? append)
    {
        if (action is not string text || text is not ("default" or "error" or "ignore" or "always" or "module" or "once"))
            throw new PyAssertionError("invalid action: " + PyRepr.Repr(action));
        if (!IsCategory(category)) throw new PyAssertionError("category must be a Warning subclass");
        // simplefilter的lineno要求int，而非任意__index__对象。
        if (line is not (long or int or bool or BigInteger))
            throw new PyAssertionError("lineno must be an int >= 0");
        BigInteger lineNumber = PyInt.ToBigInteger(line!);
        if (lineNumber.Sign < 0) throw new PyAssertionError("lineno must be an int >= 0");
        var rule = new Rule(text, category!, lineNumber);
        if (PyOps.Truthy(append))
        {
            if (!_rules.Contains(rule)) _rules.Add(rule);
        }
        else
        {
            _rules.Remove(rule);
            _rules.Insert(0, rule);
        }
        Registry.Clear();
    }

    // catch_warnings恢复规则列表，不恢复warning registry的旧版本。
    internal static List<Rule> Enter()
    {
        List<Rule> previous = _rules;
        _rules = new List<Rule>(_rules);
        Registry.Clear();
        return previous;
    }
    internal static void Restore(List<Rule> previous)
    {
        _rules = previous;
        Registry.Clear();
    }

    internal static string Action(object? text, object category, string module, int line)
    {
        string action = "default";
        foreach (Rule rule in _rules)
            if (Matches(category, rule.Category) && (rule.Line == 0 || rule.Line == line)
                && (rule.Module is null || rule.Module == module))
            { action = rule.Action; break; }
        if (action is "ignore" or "error" or "always") return action;
        // 非Warning message本身就是去重键，不先str；仅抑制动作才需要Python哈希/相等。
        // 固定CPython 3.12的warn以模块registry处理once；warn_explicit的全局once不在本实现范围。
        var key = new PyTuple([action, text, category, module,
            (long)(action == "default" ? line : 0)]);
        return Registry.Add(key) ? "always" : "ignore";
    }
}
