// SQLite provider 的真实数据库测试：绑定、事务、锁、线程合同与动态异常分派；所有连接确定性关闭。
using System.Diagnostics;
using System.Numerics;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PySqliteTests : IDisposable
{
    private readonly List<PySqliteConnection> connections = [];
    private readonly string directory = Path.Combine(Path.GetTempPath(), "ryupy-sqlite-" + Guid.NewGuid());

    private PySqliteConnection Open(string path = ":memory:", string? isolation = "", bool checkThread = true)
    {
        var connection = new PySqliteConnection(path, 1L, isolation, checkThread);
        connections.Add(connection); return connection;
    }

    private static object? Call(PyNativeObject target, string name, params object?[] arguments) =>
        PyOps.CallValue(target.GetAttr(name), arguments, [], []);

    private static PySqliteCursor Execute(PySqliteConnection connection, string sql, params object?[] values) =>
        Assert.IsType<PySqliteCursor>(Call(connection, "execute", sql, new PyTuple(values)));

    private static object?[] Row(PySqliteCursor cursor) => Assert.IsType<PyTuple>(cursor.FetchOne()).Items;
    private static PyDict Named(params (string Key, object? Value)[] values)
    {
        var result = new PyDict(); foreach (var (key, value) in values) result.Set(key, value); return result;
    }
    private static void Error(string category, Action action)
    {
        PySqliteError error = Assert.Throws<PySqliteError>(action);
        Assert.True(PyErrors.MatchDynamicException(error, PyType.Get("sqlite3." + category)));
    }

    /// <summary>临时文件只在自己的 TMPDIR 内创建；测试退出关闭连接后删除数据库及 journal。</summary>
    public void Dispose()
    {
        foreach (PySqliteConnection connection in connections) connection.Close();
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    [Fact]
    public void BaseValuesAndEmptyBlobsRoundTripWithoutProviderCoercion()
    {
        var connection = Open();
        object?[] values = [long.MinValue, long.MaxValue, 2.5, true, PyNone.Instance,
            "中文\0text", new PyBytes([0, 255]), new PyBytes([]), new BigInteger(42)];
        object?[] row = Row(Execute(connection, "select ?,?,?,?,?,?,?,?,?", values));
        Assert.Equal(values[0], row[0]); Assert.Equal(values[1], row[1]); Assert.Equal(2.5, row[2]);
        Assert.Equal(1L, row[3]); Assert.IsType<PyNone>(row[4]); Assert.Equal(values[5], row[5]);
        Assert.Equal(new byte[] { 0, 255 }, Assert.IsType<PyBytes>(row[6]).Bytes);
        Assert.Empty(Assert.IsType<PyBytes>(row[7]).Bytes); Assert.Equal(42L, row[8]);
        Assert.Throws<PyOverflowError>(() => Execute(connection, "select ?", BigInteger.One << 80));
        Error("ProgrammingError", () => Execute(connection, "select ?", new PyDict()));
    }

    [Fact]
    public void ParametersProtectQuotedSqlAndSupportRepeatedPrefixNames()
    {
        var connection = Open();
        Assert.Equal(new object?[] { "? :x", 9L }, Row(Execute(connection, "select '? :x', ? /* :x ? */ -- ?\n", 9L)));
        var cursor = Assert.IsType<PySqliteCursor>(Call(connection, "execute", "select :x,:x,@x,$other",
            Named(("x", "';drop table t;--"), ("other", 7L), ("ignored", 99L))));
        Assert.Equal(new object?[] { "';drop table t;--", "';drop table t;--", "';drop table t;--", 7L }, Row(cursor));
        Assert.Equal(new object?[] { 3L }, Row(Execute(connection, "select ? as [?:x]", 3L)));
        Error("ProgrammingError", () => Execute(connection, "select ?"));
        Error("ProgrammingError", () => Call(connection, "execute", "select ?", Named(("x", 1L))));
        Error("ProgrammingError", () => Call(connection, "execute", "select :x", Named(("y", 1L))));
        Error("NotSupportedError", () => Execute(connection, "select ?2", 1L));
        Error("NotSupportedError", () => Execute(connection, "select $x::suffix", 1L));
    }

    [Fact]
    public void MultipleStatementsRejectBeforeFirstEffectAndEmptySqlIsNoOp()
    {
        var connection = Open();
        Error("ProgrammingError", () => Execute(connection, "create table leaked(x); invalid sql"));
        Assert.Equal(0L, Row(Execute(connection, "select count(*) from sqlite_master where name='leaked'"))[0]);
        foreach (string sql in new[] { "", "-- comment", "/* comment */", ";" })
        {
            var cursor = Execute(connection, sql);
            Assert.IsType<PyNone>(cursor.FetchOne()); Assert.Equal(-1L, cursor.GetAttr("rowcount"));
            Assert.IsType<PyNone>(cursor.GetAttr("lastrowid"));
            Call(cursor, "execute", "create table if not exists rowids(value)");
            Call(cursor, "execute", "insert into rowids values (1)");
            object? inserted = cursor.GetAttr("lastrowid");
            Call(cursor, "execute", sql); Assert.Equal(inserted, cursor.GetAttr("lastrowid"));
        }
        Assert.Equal(1L, Row(Execute(connection, "select 1; /* trailing comment */ -- ok"))[0]);
        Error("OperationalError", () => Execute(connection, "select * from missing_table"));
    }

    [Fact]
    public void StreamingCursorDescriptionFetchManyAndIterationSharePosition()
    {
        var connection = Open(); var cursor = Execute(connection, "select 1 as value union all select 2 union all select 3");
        Assert.Equal(-1L, cursor.GetAttr("rowcount"));
        var column = Assert.IsType<PyTuple>(Assert.IsType<PyTuple>(cursor.GetAttr("description")).Items[0]);
        Assert.Equal(7, column.Items.Length); Assert.Equal("value", column.Items[0]);
        cursor.SetAttr("arraysize", 2L);
        Assert.Equal(2, Assert.IsType<PyList>(Call(cursor, "fetchmany")).Count);
        Assert.True(cursor.MoveNext()); Assert.Equal(3L, Assert.IsType<PyTuple>(cursor.Current()).Items[0]);
        Assert.False(cursor.MoveNext()); Assert.IsType<PyNone>(cursor.FetchOne()); Assert.Empty(cursor.FetchAll().Items);
        Assert.Same(cursor, Call(cursor, "execute", "select 4")); Assert.Equal(4L, Row(cursor)[0]);
        cursor.Close(); cursor.Close(); Error("ProgrammingError", () => cursor.FetchOne());
    }

    [Fact]
    public void ExecuteManyKeepsPrefixOnFailureAndRollbackRestoresDatabase()
    {
        var connection = Open(); var cursor = Execute(connection, "create table items(value unique)");
        var values = new PyList(); values.Append(new PyTuple([1L])); values.Append(new PyTuple([2L]));
        Assert.Same(cursor, Call(cursor, "executemany", "insert into items values (?)", values));
        Assert.Equal(2L, cursor.GetAttr("rowcount")); Assert.Equal(0L, cursor.GetAttr("lastrowid"));
        connection.Commit(); values.Append(new PyTuple([1L]));
        Error("IntegrityError", () => Call(cursor, "executemany", "insert into items values (?+10)", values));
        Assert.Equal(-1L, cursor.GetAttr("rowcount")); Assert.True((bool)connection.GetAttr("in_transaction")!);
        Assert.Equal(4L, Row(Execute(connection, "select count(*) from items"))[0]);
        connection.Rollback(); Assert.Equal(2L, Row(Execute(connection, "select count(*) from items"))[0]);
        Error("ProgrammingError", () => Call(cursor, "executemany", "insert into items values (?) returning value", values));
        Error("ProgrammingError", () => Call(cursor, "executemany", "select ?", values));
    }

    [Fact]
    public void TransactionModesExplicitSqlAndIsolationSetterStaySynchronized()
    {
        var connection = Open(isolation: "immediate"); Execute(connection, "create table items(value)");
        Assert.False((bool)connection.GetAttr("in_transaction")!);
        Execute(connection, "insert into items values (1)"); Assert.True((bool)connection.GetAttr("in_transaction")!);
        connection.SetAttr("isolation_level", PyNone.Instance); Assert.False((bool)connection.GetAttr("in_transaction")!);
        Execute(connection, "insert into items values (2)"); connection.Rollback();
        Assert.Equal(2L, Row(Execute(connection, "select count(*) from items"))[0]);
        Execute(connection, "begin"); Execute(connection, "delete from items"); Execute(connection, "rollback");
        Assert.False((bool)connection.GetAttr("in_transaction")!);
        Assert.Equal(2L, Row(Execute(connection, "select count(*) from items"))[0]);
        connection.SetAttr("isolation_level", "exclusive"); Assert.Equal("EXCLUSIVE", connection.GetAttr("isolation_level"));
    }

    [Fact]
    public void ContextManagerRollsBackCommitFailureAndLeavesConnectionOpen()
    {
        var connection = Open(); Execute(connection, "pragma foreign_keys=on");
        Execute(connection, "create table parent(id primary key)");
        Execute(connection, "create table child(parent references parent(id) deferrable initially deferred)");
        Assert.Same(connection, Call(connection, "__enter__"));
        Execute(connection, "insert into child values (42)");
        Error("IntegrityError", () => Call(connection, "__exit__", PyNone.Instance, PyNone.Instance, PyNone.Instance));
        Assert.False((bool)connection.GetAttr("in_transaction")!);
        Assert.Equal(0L, Row(Execute(connection, "select count(*) from child"))[0]);
        Execute(connection, "insert into parent values (42)");
        Assert.False((bool)Call(connection, "__exit__", PyNone.Instance, PyNone.Instance, PyNone.Instance)!);
        Execute(connection, "delete from parent"); Call(connection, "__exit__", PyType.Get("ValueError"), null, null);
        Assert.Equal(1L, Row(Execute(connection, "select count(*) from parent"))[0]);
    }

    [Fact]
    public void FilesPersistAndCloseRollsBackWithOutstandingReader()
    {
        Directory.CreateDirectory(directory); string path = Path.Combine(directory, "中文.db");
        var connection = Open(path); Execute(connection, "create table items(value)");
        Execute(connection, "insert into items values (1)"); connection.Commit();
        Execute(connection, "insert into items values (2)"); var reader = Execute(connection, "select * from items");
        connection.Close(); connection.Close(); Error("ProgrammingError", () => reader.FetchOne());
        Assert.Equal(1L, Row(Execute(Open(path), "select count(*) from items"))[0]);
    }

    [Fact]
    public void LockTimeoutAndThreadContractProducePythonErrors()
    {
        Directory.CreateDirectory(directory); string path = Path.Combine(directory, "locked.db");
        var first = Open(path); var second = Open(path); Execute(first, "create table items(value)");
        Execute(first, "insert into items values (1)"); var watch = Stopwatch.StartNew();
        Error("OperationalError", () => Execute(second, "insert into items values (2)"));
        Assert.InRange(watch.Elapsed.TotalSeconds, 0.8, 10); first.Rollback();
        var shared = Open(checkThread: false);
        Exception? failure = null;
        var worker = new Thread(() =>
        {
            try
            {
                Error("ProgrammingError", () => Call(first, "cursor"));
                Assert.Equal(7L, Row(Execute(shared, "select 7"))[0]);
            }
            catch (Exception error) { failure = error; }
        });
        worker.Start(); worker.Join(); Assert.Null(failure);
    }

    [Fact]
    public void UnsupportedOptionsFailBeforeOpeningAndExceptionFamiliesAreDistinct()
    {
        object? connect = PySqlite.CreateAttributes()["connect"];
        foreach (string keyword in new[] { "uri", "detect_types", "factory", "cached_statements", "autocommit" })
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(connect, [":memory:"], [keyword], [true]));
        Assert.Throws<PyNotImplementedError>(() => new PySqliteConnection(":memory:", 0.5, "", true));
        Error("NotSupportedError", () => new PySqliteConnection("file:db?mode=memory", 1L, "", true));
        Assert.Throws<PyValueError>(() => new PySqliteConnection(":memory:", 1L, "invalid", true));
        PySqliteError error = Assert.Throws<PySqliteError>(() => Execute(Open(), "select * from nonexistent"));
        Assert.True(PyErrors.MatchDynamicException(error, PyType.Get("sqlite3.DatabaseError")));
        Assert.True(PyErrors.MatchDynamicException(error, PyType.Get("Exception")));
        Assert.False(PyErrors.MatchDynamicException(error, PyType.Get("sqlite3.IntegrityError")));
    }
}
