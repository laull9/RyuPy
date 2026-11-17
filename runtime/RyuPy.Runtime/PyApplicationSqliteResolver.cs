// 标准库sqlite3既有静态后端的冷加载根；只在显式新布局早装，用户已安装的程序集resolver保持优先。
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

namespace RyuPy.Runtime;

public static partial class PyApplicationLayout
{
    private static int sqliteResolverAttempted;
    private static readonly object SqliteGate = new();
    private static IntPtr sqliteHandle;
    internal static void InstallSqliteResolver()
    {
        if (!Current.Value.Canonical || Interlocked.Exchange(ref sqliteResolverAttempted, 1) != 0) return;
        try
        {
            NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_e_sqlite3).Assembly, ResolveSqlite);
        }
        catch (InvalidOperationException)
        {
            // CLR没有公开查询resolver的API；已有显式宿主resolver由CLR拒绝覆盖，保其原权威。
        }
    }
    private static IntPtr ResolveSqlite(string requested, Assembly owner, DllImportSearchPath? search)
    {
        if (requested != "e_sqlite3") return IntPtr.Zero;
        lock (SqliteGate)
        {
            // 绝对路径失败直接传播，不回退cwd/PATH；已解析的静态函数地址随进程保持。
            if (sqliteHandle == IntPtr.Zero) sqliteHandle = NativeLibrary.Load(Path.Combine(DependencyRoot, SqliteNativeFile()));
            return sqliteHandle;
        }
    }
    internal static string SqliteNativeFile() => OperatingSystem.IsWindows() ? "e_sqlite3.dll"
        : OperatingSystem.IsMacOS() ? "libe_sqlite3.dylib" : "libe_sqlite3.so";
}
