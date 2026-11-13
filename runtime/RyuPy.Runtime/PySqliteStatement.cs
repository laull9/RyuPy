// SQL 参数适配只改占位符：词法状态保护字符串、标识符和注释，真实语句边界交给 SQLite prepare。
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace RyuPy.Runtime;

internal sealed class PySqliteStatement
{
    internal readonly string Sql;
    internal readonly List<string> Parameters = new();
    internal readonly bool Dml;
    internal readonly bool HasColumns;
    internal readonly bool HasStatement;
    private readonly bool positional;

    internal PySqliteStatement(SqliteConnection connection, string sql)
    {
        if (sql.IndexOf('\0') >= 0) throw PySqlite.Error("ProgrammingError", "the query contains a null character");
        var rewritten = new StringBuilder();
        string first = "";
        for (int index = 0; index < sql.Length;)
        {
            char current = sql[index]; int start = index++;
            if (current is '\'' or '"' or '`' or '[')
            {
                char end = current == '[' ? ']' : current;
                while (index < sql.Length)
                {
                    if (sql[index++] != end) continue;
                    if (end != ']' && index < sql.Length && sql[index] == end) { index++; continue; }
                    break;
                }
            }
            else if (current == '-' && index < sql.Length && sql[index] == '-')
            {
                while (index < sql.Length && sql[index] != '\n') index++;
            }
            else if (current == '/' && index < sql.Length && sql[index] == '*')
            {
                index++;
                while (index < sql.Length && !(sql[index - 1] == '*' && sql[index] == '/')) index++;
                if (index < sql.Length) index++;
            }
            else if (current == '?')
            {
                if (index < sql.Length && char.IsAsciiDigit(sql[index]))
                    throw PySqlite.Error("NotSupportedError", "numbered ?NNN parameters are not supported");
                if (Parameters.Count != 0 && !positional)
                    throw PySqlite.Error("ProgrammingError", "mixed named and qmark parameters are not supported");
                positional = true;
                string parameter = "$__ryupy_sqlite_" + Parameters.Count;
                Parameters.Add(parameter); rewritten.Append(parameter); continue;
            }
            else if (current is ':' or '@' or '$' && index < sql.Length && IsName(sql[index]))
            {
                if (positional) throw PySqlite.Error("ProgrammingError", "mixed named and qmark parameters are not supported");
                while (index < sql.Length && IsName(sql[index])) index++;
                string parameter = sql[start..index];
                if (!Parameters.Contains(parameter)) Parameters.Add(parameter);
            }
            else if (first.Length == 0 && char.IsAsciiLetter(current))
            {
                while (index < sql.Length && char.IsAsciiLetter(sql[index])) index++;
                first = sql[start..index].ToUpperInvariant();
            }
            rewritten.Append(sql.AsSpan(start, index - start));
        }
        Sql = rewritten.ToString();
        Dml = first is "INSERT" or "UPDATE" or "DELETE" or "REPLACE";
        // prepare 不执行 SQL；用引擎返回的 tail 拒绝第二条语句，包括尾部注释后的语句。
        int result = raw.sqlite3_prepare_v2(connection.Handle, Sql, out sqlite3_stmt statement, out string tail);
        using (statement)
        {
            SqliteException.ThrowExceptionForRC(result, connection.Handle);
            HasStatement = statement is not null && !statement.IsInvalid;
            HasColumns = HasStatement && raw.sqlite3_column_count(statement) != 0;
            if (HasStatement && raw.sqlite3_bind_parameter_count(statement) != Parameters.Count)
                throw PySqlite.Error("NotSupportedError", "extended SQLite parameter names are not supported");
            for (int index = 0; HasStatement && index < Parameters.Count; index++)
                if (raw.sqlite3_bind_parameter_name(statement, index + 1).utf8_to_string() != Parameters[index])
                    throw PySqlite.Error("NotSupportedError", "extended SQLite parameter names are not supported");
        }
        // tail 只允许空白和注释；不要编译第二条 SQL，否则其语法错误会抢先改变 DBAPI 错误类别。
        for (int index = 0; index < tail.Length;)
        {
            if (char.IsWhiteSpace(tail[index])) { index++; continue; }
            if (tail[index..].StartsWith("--", StringComparison.Ordinal))
            {
                int end = tail.IndexOf('\n', index + 2); index = end < 0 ? tail.Length : end + 1; continue;
            }
            if (tail[index..].StartsWith("/*", StringComparison.Ordinal))
            {
                int end = tail.IndexOf("*/", index + 2, StringComparison.Ordinal);
                index = end < 0 ? tail.Length : end + 2; continue;
            }
            throw PySqlite.Error("ProgrammingError", "You can only execute one statement at a time.");
        }
    }

    private static bool IsName(char character) => char.IsLetterOrDigit(character) || character is '_' || character >= 128;

    internal void Bind(SqliteCommand command, object? values)
    {
        if (values is PyDict dictionary)
        {
            if (positional) throw PySqlite.Error("ProgrammingError", "Binding has no name, but you supplied a dictionary");
            foreach (string name in Parameters)
            {
                if (!dictionary.TryGet(name[1..], out object? value))
                    throw PySqlite.Error("ProgrammingError", $"You did not supply a value for binding parameter {name}.");
                command.Parameters.AddWithValue(name, PySqlite.ToDatabase(value));
            }
            return;
        }
        object?[] items = values switch
        {
            PyTuple tuple => tuple.Items,
            PyList list => list.Items.ToArray(),
            _ => throw PySqlite.Error("ProgrammingError", "parameters must be a tuple, list or dict"),
        };
        if (items.Length != Parameters.Count)
            throw PySqlite.Error("ProgrammingError", $"Incorrect number of bindings supplied. The current statement uses {Parameters.Count}, and there are {items.Length} supplied.");
        for (int index = 0; index < items.Length; index++)
            command.Parameters.AddWithValue(Parameters[index], PySqlite.ToDatabase(items[index]));
    }
}
