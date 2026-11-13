# 同一应用贯通 HTTP、编码、压缩、JSON、包资源与 SQLite 参数化事务。
import base64
import gzip
import json
import sqlite3
import sys
from importlib.resources import as_file, files
from urllib.parse import urlencode
from urllib.request import urlopen
import appdata

url = sys.argv[1] + "?" + urlencode({"format": "gzip-base64", "label": "集成"})
with urlopen(url, timeout=5) as response:
    assert response.status == 200
    payload = response.read()
rows = json.loads(gzip.decompress(base64.b64decode(payload, validate=True)))
resource = files(appdata).joinpath("schema.sql")
with as_file(resource) as path:
    schema = path.read_text(encoding="utf-8")
connection = sqlite3.connect(sys.argv[2])
connection.execute(schema)
with connection:
    for row in rows:
        connection.execute("INSERT INTO records(id, name) VALUES (?, ?)", (row["id"], row["name"]))
print(connection.execute("SELECT id, name FROM records ORDER BY id").fetchall())
try:
    with connection:
        connection.execute("INSERT INTO records VALUES (?, ?)", (99, "rolled back"))
        raise ValueError("rollback")
except ValueError:
    pass
print(connection.execute("SELECT COUNT(*) FROM records").fetchone())
connection.close()
print(resource.is_file())
