# 相对文件数据库必须能持久化、关闭、重开；运行目录由差分框架隔离。
import sqlite3
import os

path = "sqlite3-movable.db"
if os.path.exists(path):
    os.remove(path)
db = sqlite3.connect(path)
db.execute("create table notes(id integer primary key, text text)")
db.executemany("insert into notes(text) values (?)", [("中文",), ("portable",)])
db.commit()
db.close()
db = sqlite3.connect(path)
print(db.execute("select * from notes order by id").fetchall())
db.execute("insert into notes(text) values ('discard')")
db.close()
db = sqlite3.connect(path)
print(db.execute("select count(*) from notes").fetchone())
db.close()
os.remove(path)
