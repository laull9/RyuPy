// 独立新鲜进程验证AutoUnavailable与公开hooks冷拒绝；不reset单向标志或污染xUnit域。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RyuPy.Runtime;
public static class AutoUnavailableProbe
{
    public static int Main()
    {
        PyDistributionSnapshot.ConfigureAutoUnavailable(typeof(AutoUnavailableProbe), "fixture unavailable");
        PySystem.Initialize("auto-unavailable.py", []);
        PyModule metadata = PyModules.Import("importlib.metadata");
        Console.WriteLine("import-ok");
        bool unavailable = false;
        try { PyOps.CallValue(metadata.GetAttr("entry_points"), [], [], []); }
        catch (PyNotImplementedError error)
        {
            if (!error.Message.Contains("fixture unavailable", StringComparison.Ordinal)) return 2;
            unavailable = true; Console.WriteLine("require-ni");
        }
        if (!unavailable) return 1;
        // 放在该独立host最后，真正公开setter置单向flag，不通过反射恢复权限。
        const string parent = "hooks_candidate", child = "hooks_candidate.child";
        int parentRuns = 0, childRuns = 0;
        string parentPath = Path.GetFullPath(Path.Combine("hooks-fixture", parent, "__init__.py"));
        string childPath = Path.GetFullPath(Path.Combine("hooks-fixture", parent, "child.py"));
        PyModules.RegisterCandidateInstance(parent, parent, "hooks-parent.py", 1, parentPath, true, _ => parentRuns++);
        PyModules.RegisterCandidateInstance(child, parent, "hooks-child.py", 1, childPath, false, _ => childRuns++);
        string payload = JsonSerializer.Serialize(new {
            schema_version = 1, roots = new[] { child },
            dependencies = new Dictionary<string, string[]> { [parent] = [], [child] = [parent] },
            locations = new Dictionary<string, object> {
                [parent] = new { package = parent, file = "hooks-parent.py", path = parentPath, is_package = true },
                [child] = new { package = parent, file = "hooks-child.py", path = childPath, is_package = false } },
            deferred = new Dictionary<string, string>(), outside = Array.Empty<string>(), @protected = Array.Empty<string>() });
        PyModules.ConfigureOptionalSources(payload);
        PyModules.Import("sys").SetAttr("meta_path", new PyList([]));
        if (!Rejects(() => PyModules.Import(child))
            || !Rejects(() => PyOps.CallValue(PyImportlibUtil.CreateAttributes()["find_spec"], [child], [], []))) return 3;
        if (parentRuns != 0 || childRuns != 0
            || PyModules.ModuleForFile("hooks-parent.py") is not null
            || PyModules.ModuleForFile("hooks-child.py") is not null
            || PyOps.Contains(new PyModuleView(), parent) || PyOps.Contains(new PyModuleView(), child)) return 4;
        Console.WriteLine("hooks-ni");
        return 0;
    }
    private static bool Rejects(Action action)
    {
        try { action(); return false; }
        catch (PyNotImplementedError) { return true; }
    }
}
