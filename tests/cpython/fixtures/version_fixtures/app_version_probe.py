#!/usr/bin/env python3
"""版本兼容业务输入：固定版本事实、真实NumPy扩展、标准库、异常和默认中文文件IO。"""
import json
from pathlib import Path
import sqlite3
import ssl
import sys
import numpy as np

values = np.array([2, 4, 6], dtype="int64")
view = values[1:]
view[0] = 11
computed = (values * 3).tolist()
connection = sqlite3.connect(":memory:")
connection.execute("create table sample (message text)")
connection.execute("insert into sample values (?)", ("版本适配 中文",))
message = connection.execute("select message from sample").fetchone()[0]
connection.close()
context = ssl.create_default_context()
error_name = ""
try:
    np.array(["invalid number"], dtype="int64")
except ValueError as error:
    error_name = type(error).__name__
result = {
    "python": list(sys.version_info[:3]),
    "implementation": sys.implementation.name,
    "numpy": np.__version__,
    "computed": computed,
    "sqlite": message,
    "ssl_verify": int(context.verify_mode),
    "error": error_name,
}
text = json.dumps(result, ensure_ascii=False, sort_keys=True)
Path("版本 业务副作用.txt").write_text(text)
print(text)
print("版本 stderr 中文", file=sys.stderr)
