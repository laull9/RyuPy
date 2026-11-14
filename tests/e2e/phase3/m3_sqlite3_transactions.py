# 以固定 CPython 3.12.13 检查手动事务、批处理中断、fetchmany、RETURNING 和空 SQL。
import sqlite3

db = sqlite3.connect(":memory:", isolation_level="IMMEDIATE")
cur = db.cursor()
print(isinstance(db, sqlite3.Connection), isinstance(cur, sqlite3.Cursor))
cur.execute("")
print(cur.lastrowid, cur.rowcount, cur.description, cur.fetchall())
cur.execute("create table values_table(value unique)")
cur.executemany("insert into values_table values (?)", ((1,), (2,), (3,)))
print(cur.rowcount, db.in_transaction)
db.isolation_level = None
print(db.in_transaction, db.isolation_level)
db.execute("insert into values_table values (4)")
db.rollback()
print(db.execute("select count(*) from values_table").fetchone())
db.execute("begin")
db.execute("delete from values_table")
db.execute("rollback")
print(db.in_transaction)
db.isolation_level = "deferred"
try:
    cur.executemany("insert into values_table values (?)", [(5,), (6,), (5,)])
except sqlite3.DatabaseError:
    print("batch error", cur.rowcount, db.in_transaction)
print(db.execute("select value from values_table order by value").fetchall())
db.rollback()
cur.execute("select value as x from values_table order by value")
cur.arraysize = 2
print(cur.fetchmany(), cur.fetchone(), list(cur), cur.fetchone())
cur.execute("select 1 union all select 2")
print(cur.fetchmany(0))
cur.execute("insert into values_table values (7) returning value")
print(cur.rowcount, cur.fetchone(), cur.fetchone(), cur.rowcount)
db.rollback()
try:
    cur.execute("create table leaked(x); broken sql")
except sqlite3.ProgrammingError:
    print("single statement")
print(db.execute("select count(*) from sqlite_master where name='leaked'").fetchone())
db.close()
