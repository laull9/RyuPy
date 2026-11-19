# SQLite DBAPI 竖切：参数、类型、游标耗尽、事务与错误都由 CPython 差分验收。
import sqlite3

db = sqlite3.connect(":memory:")
cur = db.cursor()
print(cur.connection is db)
print(cur.execute("create table records(id integer primary key, label text unique, value real, data blob, absent)") is cur)
print(db.in_transaction)
cur.execute("insert into records values (?, ?, ?, ?, ?)", (1, "中文 '?':x", 2.5, b"\x00\xff", None))
print(cur.rowcount, cur.lastrowid, db.in_transaction)
cur.executemany("insert into records(id,label,value) values (:id,:label,:value)", [
    {"id": 2, "label": "two", "value": True},
    {"id": 3, "label": "three", "value": 3.0},
])
print(cur.rowcount, cur.lastrowid)
db.commit()
print(db.in_transaction)
cur.execute("select id,label,value,data,absent from records order by id")
print(cur.rowcount)
print(cur.fetchone())
print(cur.fetchall())
print(cur.fetchone(), cur.fetchall())
print([column[0] for column in cur.description])
print(db.execute("select :x, :x, @y, $z", {"x": 7, "y": "yes", "z": None}).fetchone())
print(db.execute("select '?' /* ? */ , ? -- ?\n", (9,)).fetchone())
with db:
    db.execute("insert into records(id,label) values (4,'four')")
print(db.execute("select count(*) from records").fetchone())
try:
    with db:
        db.execute("delete from records")
        raise ValueError("rollback")
except ValueError:
    print("rolled back")
print(db.execute("select count(*) from records").fetchone())
db.execute("delete from records")
db.rollback()
print(db.execute("select count(*) from records").fetchone())
try:
    db.execute("insert into records(id,label) values (5,'two')")
except sqlite3.IntegrityError as error:
    print(isinstance(error, sqlite3.DatabaseError), isinstance(error, sqlite3.Error))
db.rollback()
for sql, values in [("select ?", ()), ("select :missing", {}), ("select 1; select 2", ())]:
    try:
        db.execute(sql, values)
    except sqlite3.ProgrammingError:
        print("programming error")
try:
    db.execute("select * from absent_table")
except sqlite3.OperationalError:
    print("operational error")
cur.close()
cur.close()
try:
    cur.fetchone()
except sqlite3.ProgrammingError:
    print("cursor closed")
db.close()
db.close()
try:
    db.commit()
except sqlite3.ProgrammingError:
    print("connection closed")
