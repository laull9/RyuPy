// sqlite3 的 DBAPI 模块入口与值转换；数据库引擎、SQL 编译和执行复用成熟 provider。
using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace RyuPy.Runtime;

/// <summary>SQLite 原生竖切：固定委托和静态类型调用，适用于裁剪及 NativeAOT。</summary>
internal static class PySqlite
{
    internal static readonly string[] Errors = ["Warning", "Error", "InterfaceError", "DatabaseError",
        "DataError", "OperationalError", "IntegrityError", "InternalError", "ProgrammingError", "NotSupportedError"];

    /// <summary>只初始化托管模块字典；首次连接时才加载 SQLite 原生库。</summary>
    public static Dictionary<string, object?> CreateAttributes()
    {
        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["connect"] = PyNativeFunctions.Create("connect",
                ["database", "timeout", "isolation_level", "check_same_thread"],
                [null, 5.0, "", true], 1,
                args => new PySqliteConnection(args[0], args[1], args[2], args[3]),
                ["pos", "kwonly", "kwonly", "kwonly"]),
            ["Connection"] = new PyNativeType("Connection", "sqlite3", null, value => value is PySqliteConnection),
            ["Cursor"] = new PyNativeType("Cursor", "sqlite3", null, value => value is PySqliteCursor),
            ["apilevel"] = "2.0", ["paramstyle"] = "qmark", ["threadsafety"] = 1L,
        };
        foreach (string name in Errors) attributes[name] = PyType.Get("sqlite3." + name);
        return attributes;
    }

    internal static object ToDatabase(object? value) => value switch
    {
        null or PyNone => DBNull.Value,
        bool boolean => boolean ? 1L : 0L,
        long or double or string => value,
        BigInteger integer when integer >= long.MinValue && integer <= long.MaxValue => (long)integer,
        BigInteger => throw new PyOverflowError("Python int too large to convert to SQLite INTEGER"),
        PyBytes bytes => bytes.Bytes,
        PyByteArray bytes => bytes.Bytes.ToArray(),
        _ => throw Error("ProgrammingError", $"type '{PyRepr.TypeName(value)}' is not supported"),
    };

    internal static object FromDatabase(object value) => value switch
    {
        DBNull => PyNone.Instance,
        byte[] bytes => new PyBytes(bytes),
        long or double or string => value,
        _ => throw Error("DataError", "SQLite 返回了未支持的基础类型"),
    };

    internal static PySqliteError Error(string category, string message) => new(category, message);

    internal static T Translate<T>(Func<T> operation)
    {
        try { return operation(); }
        catch (SqliteException error)
        {
            string category = error.SqliteErrorCode switch
            {
                raw.SQLITE_CONSTRAINT => "IntegrityError",
                raw.SQLITE_TOOBIG or raw.SQLITE_MISMATCH => "DataError",
                raw.SQLITE_MISUSE => "InterfaceError",
                raw.SQLITE_INTERNAL => "InternalError",
                raw.SQLITE_CORRUPT or raw.SQLITE_NOTADB => "DatabaseError",
                _ => "OperationalError",
            };
            throw new PySqliteError(category, error.Message);
        }
    }

    internal static PyFunction Method(string name, string[] names, object?[] defaults, int required,
        Func<object?[], object?> body)
    {
        var kinds = new string[names.Length]; Array.Fill(kinds, "posonly");
        return PyNativeFunctions.Create(name, names, defaults, required, body, kinds);
    }
}

/// <summary>SQLite 异常保留独立 CLR 家族和限定 Python 类型身份。</summary>
internal sealed class PySqliteError : PyException
{
    /// <summary>按 DBAPI 类别构造；捕获匹配通过限定类型和规范父链完成。</summary>
    public PySqliteError(string category, string message) : base("sqlite3." + category, message) { }
}
