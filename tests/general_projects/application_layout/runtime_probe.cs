// 独立进程的真实SQLite/resolver探针：已有自定义resolver必须保优先，新布局读取同一依赖根。
using RyuPy.Runtime;
using Microsoft.Data.Sqlite;
using System.Runtime.InteropServices;
internal static class Program
{
    private static int customCalls;
    private static string NativeFile => OperatingSystem.IsWindows() ? "e_sqlite3.dll"
        : OperatingSystem.IsMacOS() ? "libe_sqlite3.dylib" : "libe_sqlite3.so";
    private static int Main(string[] args)
    {
        bool custom = args.Contains("custom");
        if (custom)
            NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_e_sqlite3).Assembly,
                (name, owner, search) =>
                {
                    if (name != "e_sqlite3") return IntPtr.Zero;
                    customCalls++;
                    return NativeLibrary.Load(Path.Combine(PyApplicationLayout.DependencyRoot, NativeFile));
                });
        PySystem.InitializeCompiledBase("layout-probe", []);
        using var connection = new SqliteConnection("Data Source=:memory:;Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand(); command.CommandText = "select 40+2";
        if ((long)command.ExecuteScalar()! != 42L) throw new Exception("SQLite实际查询失败");
        if (custom && customCalls == 0) throw new Exception("已有resolver未被实际调用");
        bool canonical = args.Contains("canonical");
        if ((Path.GetFileName(Path.TrimEndingDirectorySeparator(PyApplicationLayout.DependencyRoot)) == "internal") != canonical)
            throw new Exception("实际dependency root与marker不一致");
        string expected = canonical ? "native/v1/probe" : "_internal/native/v1/probe";
        if (PyApplicationLayout.MapLegacyRelative("_internal/native/v1/probe") != expected) throw new Exception("旧逻辑位置映射错误");
        Console.WriteLine($"SQLITE=42 canonical={canonical} custom={custom} called={customCalls > 0}"); return 0;
    }
}
