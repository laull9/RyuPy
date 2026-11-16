// SQLite 游标保留真实流式 reader；重新执行、耗尽、显式关闭都释放 provider 资源。
using System;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace RyuPy.Runtime;

/// <summary>DBAPI 游标：execute 返回自身，fetch 返回 tuple/None，迭代沿用统一协议。</summary>
internal sealed class PySqliteCursor : PyNativeObject, IPyIterator
{
    private readonly PySqliteConnection connection;
    private SqliteCommand? command;
    private SqliteDataReader? reader;
    private bool closed;
    private bool dml;
    private object? description = PyNone.Instance;
    private object? lastRowId = PyNone.Instance;
    private long rowCount = -1;
    private long arraySize = 1;
    private object? current;

    internal PySqliteCursor(PySqliteConnection connection) => this.connection = connection;

    /// <inheritdoc/>
    public override string TypeName => "Cursor";

    private void CheckOpen()
    {
        connection.CheckOpen();
        if (closed) throw PySqlite.Error("ProgrammingError", "Cannot operate on a closed cursor.");
    }

    internal void Release()
    {
        reader?.Dispose(); reader = null;
        command?.Dispose(); command = null;
    }

    /// <summary>关闭游标，重复关闭允许；连接关闭后的游标操作仍抛 ProgrammingError。</summary>
    public void Close()
    {
        if (closed) return;
        connection.CheckOpen(); Release(); closed = true; connection.Forget(this);
    }

    /// <summary>编译单条 SQL、绑定全部参数并执行；异常路径释放本次 reader。</summary>
    public PySqliteCursor Execute(object? sql, object? parameters)
    {
        CheckOpen();
        if (sql is not string text) throw new PyTypeError("execute() argument 1 must be str");
        Release(); description = PyNone.Instance; rowCount = -1;
        return PySqlite.Translate(() =>
        {
            var statement = new PySqliteStatement(connection.Handle, text);
            dml = statement.Dml;
            command = connection.Handle.CreateCommand(); command.CommandText = statement.Sql;
            try
            {
                statement.Bind(command, parameters);
                if (!statement.HasStatement)
                {
                    Release();
                    // 空语句没有执行步骤，保留游标先前的 rowid；新游标仍为 None。
                    return this;
                }
                if (dml) connection.BeginDml();
                reader = command.ExecuteReader();
                if (reader.FieldCount != 0)
                {
                    var columns = new object?[reader.FieldCount];
                    for (int index = 0; index < columns.Length; index++)
                        columns[index] = new PyTuple([reader.GetName(index), PyNone.Instance, PyNone.Instance,
                            PyNone.Instance, PyNone.Instance, PyNone.Instance, PyNone.Instance]);
                    description = new PyTuple(columns);
                    rowCount = dml ? 0 : -1;
                }
                else { rowCount = dml ? raw.sqlite3_changes(connection.Handle.Handle) : -1; Release(); }
                lastRowId = raw.sqlite3_last_insert_rowid(connection.Handle.Handle);
                return this;
            }
            catch { Release(); throw; }
        });
    }

    /// <summary>逐项消费参数迭代器；中途错误保留已执行前缀及事务，调用者可回滚。</summary>
    public PySqliteCursor ExecuteMany(object? sql, object? parameters)
    {
        CheckOpen();
        if (sql is not string text) throw new PyTypeError("executemany() argument 1 must be str");
        Release(); description = PyNone.Instance; rowCount = -1;
        var statement = PySqlite.Translate(() => new PySqliteStatement(connection.Handle, text));
        if (!statement.Dml || statement.HasColumns)
            throw PySqlite.Error("ProgrammingError", "executemany() currently requires DML without RETURNING");
        return PySqlite.Translate(() =>
        {
            IPyIterator iterator = PyIter.Create(parameters);
            long count = 0; rowCount = 0;
            connection.BeginDml();
            using SqliteCommand batch = connection.Handle.CreateCommand(); batch.CommandText = statement.Sql;
            // 同一 command 保留 provider 的 prepared statement 缓存，每项只替换参数，避免重写/重编 SQL。
            try
            {
                while (iterator.MoveNext())
                {
                    batch.Parameters.Clear(); statement.Bind(batch, iterator.Current());
                    batch.ExecuteNonQuery(); count += raw.sqlite3_changes(connection.Handle.Handle); rowCount = count;
                }
            }
            catch { rowCount = -1; throw; }
            return this;
        });
    }

    /// <summary>读取下一行；查询耗尽后持续返回 None。</summary>
    public object FetchOne()
    {
        CheckOpen();
        if (reader is null) return PyNone.Instance;
        return PySqlite.Translate(() =>
        {
            try
            {
                if (!reader.Read())
                {
                    Release(); if (dml) rowCount = raw.sqlite3_changes(connection.Handle.Handle);
                    return (object)PyNone.Instance;
                }
                var values = new object?[reader.FieldCount];
                for (int index = 0; index < values.Length; index++) values[index] = PySqlite.FromDatabase(reader.GetValue(index));
                return new PyTuple(values);
            }
            catch { Release(); throw; }
        });
    }

    /// <summary>读取剩余所有行。</summary>
    public PyList FetchAll() => FetchRows(null);

    private PyList FetchRows(long? size)
    {
        CheckOpen(); var rows = new PyList();
        while (size is null || rows.Count < size)
        {
            object row = FetchOne(); if (row is PyNone) break; rows.Append(row);
        }
        return rows;
    }

    /// <inheritdoc/>
    public bool MoveNext() { current = FetchOne(); return current is not PyNone; }
    /// <inheritdoc/>
    public object? Current() => current;

    /// <inheritdoc/>
    public override void SetAttr(string name, object? value)
    {
        if (name != "arraysize") { base.SetAttr(name, value); return; }
        if (value is not long size) throw new PyTypeError("arraysize must be int");
        arraySize = size;
    }

    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "connection" => connection, "description" => description, "rowcount" => rowCount,
        "lastrowid" => lastRowId, "arraysize" => arraySize,
        "execute" => PySqlite.Method(name, ["sql", "parameters"], [null, new PyTuple([])], 1, args => Execute(args[0], args[1])),
        "executemany" => PySqlite.Method(name, ["sql", "seq_of_parameters"], [null, null], 2, args => ExecuteMany(args[0], args[1])),
        "fetchone" => PySqlite.Method(name, [], [], 0, _ => FetchOne()),
        "fetchall" => PySqlite.Method(name, [], [], 0, _ => FetchAll()),
        "fetchmany" => PySqlite.Method(name, ["size"], [null], 0, args =>
        {
            long size = args[0] is null ? arraySize : args[0] is long number ? number : throw new PyTypeError("size must be int");
            return FetchRows(size <= 0 ? null : size);
        }),
        "close" => PySqlite.Method(name, [], [], 0, _ => { Close(); return null; }),
        "__iter__" => PySqlite.Method(name, [], [], 0, _ => { CheckOpen(); return this; }),
        "__next__" => PySqlite.Method(name, [], [], 0, _ => MoveNext() ? current : throw new PyStopIteration(null)),
        _ => throw Missing(name),
    };
}
