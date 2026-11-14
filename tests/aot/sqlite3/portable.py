# 文件数据库跨目录搬迁探针：参数绑定、提交和关闭后由独立 CPython 读取同一文件。
import sqlite3
import sys

try:
    connection = sqlite3.connect("portable.sqlite3")
except ImportError:
    print("sqlite native dependency missing")
    raise SystemExit(23)

mode = sys.argv[1]
if mode == "seed":
    with connection:
        connection.execute("create table records(id integer primary key, text_value text, payload blob)")
        connection.executemany("insert into records values (?, ?, ?)", [
            (1, "原目录", b"\x00\xff"), (2, "待搬迁", b"abc"),
        ])
elif mode == "append":
    with connection:
        connection.execute("insert into records values (:id, :label, :data)",
                           {"id": 3, "label": "搬迁后", "data": b"\x01\x02"})
print(connection.execute("select * from records order by id").fetchall())
connection.close()
