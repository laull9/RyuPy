# ryupy: pending
# M4.2：非 NumPy 扩展的基本调用、约束异常与资源关闭。
import sqlite3

connection = sqlite3.connect(":memory:")
try:
    connection.execute("create table items (value integer unique)")
    connection.execute("insert into items values (?)", (7,))
    print(connection.execute("select value from items").fetchall())
    try:
        connection.execute("insert into items values (?)", (7,))
    except sqlite3.IntegrityError:
        print("constraint error")
finally:
    connection.close()
try:
    connection.execute("select 1")
except sqlite3.ProgrammingError:
    print("closed")
