// SQLite 连接负责线程合同、legacy 隐式事务和确定性关闭；with 仅提交/回滚，不关闭连接。
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace RyuPy.Runtime;

/// <summary>由 Microsoft.Data.Sqlite 承载的 Python 连接。</summary>
internal sealed class PySqliteConnection : PyNativeObject
{
    private static readonly Lazy<bool> Provider = new(() => { Batteries_V2.Init(); return true; });
    private readonly SqliteConnection connection;
    private readonly List<WeakReference<PySqliteCursor>> cursors = new();
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly bool checkThread;
    private string? isolation;
    private bool closed;

    /// <summary>创建连接；未支持的关键字由模块的固定签名立即拒绝。</summary>
    public PySqliteConnection(object? database, object? timeout, object? isolationLevel, object? checkSameThread)
    {
        if (database is not string path) throw new PyTypeError("database must be str");
        if (path.IndexOf('\0') >= 0) throw new PyValueError("embedded null byte");
        if (path.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            throw PySqlite.Error("NotSupportedError", "SQLite URI filenames are not supported; use an ordinary filesystem path");
        double seconds = timeout switch
        {
            long integer => integer, double number => number,
            _ => throw new PyTypeError("timeout must be a number"),
        };
        // provider 的 CommandTimeout 为整数秒，0 表示无限等待；不可悄悄改变 Python 的秒数。
        if (!double.IsFinite(seconds) || seconds < 1 || seconds > int.MaxValue || Math.Truncate(seconds) != seconds)
            throw new PyNotImplementedError("sqlite3 timeout currently requires a positive integral number of seconds");
        if (checkSameThread is not bool sameThread) throw new PyTypeError("check_same_thread must be bool");
        checkThread = sameThread;
        isolation = ValidateIsolation(isolationLevel);
        try { _ = Provider.Value; }
        catch (Exception error) when (error is DllNotFoundException or TypeInitializationException or BadImageFormatException)
        {
            throw new PyImportError("sqlite3 requires the matching SQLitePCLRaw e_sqlite3 native library beside the application: " + error.Message);
        }
        connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path, Pooling = false, DefaultTimeout = (int)seconds,
        }.ToString());
        try { PySqlite.Translate(() => { connection.Open(); return true; }); }
        catch { connection.Dispose(); throw; }
    }

    /// <inheritdoc/>
    public override string TypeName => "Connection";

    internal SqliteConnection Handle { get { CheckOpen(); return connection; } }
    internal bool InTransaction { get { CheckOpen(); return raw.sqlite3_get_autocommit(connection.Handle) == 0; } }

    internal void CheckOpen()
    {
        CheckThread();
        if (closed) throw PySqlite.Error("ProgrammingError", "Cannot operate on a closed database.");
    }

    private void CheckThread()
    {
        if (checkThread && Environment.CurrentManagedThreadId != ownerThread)
            throw PySqlite.Error("ProgrammingError", "SQLite objects created in a thread can only be used in that same thread.");
    }

    internal PySqliteCursor Cursor()
    {
        CheckOpen();
        cursors.RemoveAll(reference => !reference.TryGetTarget(out _));
        var cursor = new PySqliteCursor(this); cursors.Add(new(cursor)); return cursor;
    }

    internal void Forget(PySqliteCursor cursor) => cursors.RemoveAll(reference =>
        !reference.TryGetTarget(out PySqliteCursor? target) || ReferenceEquals(target, cursor));

    internal void BeginDml()
    {
        if (isolation is not null && !InTransaction) Control("BEGIN " + isolation);
    }

    private void Control(string sql) => PySqlite.Translate(() =>
    {
        using SqliteCommand command = Handle.CreateCommand();
        command.CommandText = sql; return command.ExecuteNonQuery();
    });

    /// <summary>提交当前事务；无事务时保持空操作。</summary>
    public void Commit() { CheckOpen(); if (InTransaction) Control("COMMIT"); }

    /// <summary>回滚当前事务；无事务时保持空操作。</summary>
    public void Rollback() { CheckOpen(); if (InTransaction) Control("ROLLBACK"); }

    /// <summary>关闭全部 reader 和连接；SQLite 原生关闭会回滚未提交事务。</summary>
    public void Close()
    {
        CheckThread(); if (closed) return;
        foreach (WeakReference<PySqliteCursor> reference in cursors)
            if (reference.TryGetTarget(out PySqliteCursor? cursor)) cursor.Release();
        PySqlite.Translate(() => { connection.Dispose(); return true; }); closed = true; cursors.Clear();
    }

    private static string? ValidateIsolation(object? value)
    {
        if (value is null or PyNone) return null;
        if (value is not string text) throw new PyTypeError("isolation_level must be str or None");
        string normalized = text.ToUpperInvariant();
        if (normalized is not ("" or "DEFERRED" or "IMMEDIATE" or "EXCLUSIVE"))
            throw new PyValueError("isolation_level string must be '', 'DEFERRED', 'IMMEDIATE', or 'EXCLUSIVE'");
        return normalized;
    }

    /// <inheritdoc/>
    public override void SetAttr(string name, object? value)
    {
        if (name != "isolation_level") { base.SetAttr(name, value); return; }
        CheckOpen(); string? next = ValidateIsolation(value);
        if (next is null) Commit(); isolation = next;
    }

    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "in_transaction" => InTransaction,
        "isolation_level" => ReadIsolation(),
        "total_changes" => (long)raw.sqlite3_total_changes(Handle.Handle),
        "cursor" => PySqlite.Method(name, [], [], 0, _ => Cursor()),
        "execute" => PySqlite.Method(name, ["sql", "parameters"], [null, new PyTuple([])], 1,
            args => Shortcut(args, false)),
        "executemany" => PySqlite.Method(name, ["sql", "parameters"], [null, null], 2,
            args => Shortcut(args, true)),
        "commit" => PySqlite.Method(name, [], [], 0, _ => { Commit(); return null; }),
        "rollback" => PySqlite.Method(name, [], [], 0, _ => { Rollback(); return null; }),
        "close" => PySqlite.Method(name, [], [], 0, _ => { Close(); return null; }),
        "__enter__" => PySqlite.Method(name, [], [], 0, _ => { CheckOpen(); return this; }),
        "__exit__" => PySqlite.Method(name, ["type", "value", "traceback"], [null, null, null], 3, args => Exit(args[0])),
        _ => throw Missing(name),
    };

    private object? ReadIsolation() { CheckOpen(); return (object?)isolation ?? PyNone.Instance; }

    private object Shortcut(object?[] args, bool many)
    {
        PySqliteCursor cursor = Cursor();
        try { return many ? cursor.ExecuteMany(args[0], args[1]) : cursor.Execute(args[0], args[1]); }
        catch { cursor.Close(); throw; }
    }

    private bool Exit(object? errorType)
    {
        CheckOpen();
        if (errorType is not (null or PyNone)) Rollback();
        else
        {
            try { Commit(); }
            catch { Rollback(); throw; }
        }
        return false;
    }
}
