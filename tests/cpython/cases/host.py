"""CP1a：隔离初始化后加载基础扩展，版本必须与同次 oracle 一致。"""
import base64
import json
import sqlite3
import sys
import zlib

assert sys.version_info[:3] == (3, 12, 13)
assert base64.b64decode('5Lit5paH').decode('utf-8') == '中文'
assert json.loads('{"ok": true}')['ok']
assert zlib.decompress(zlib.compress(b'isolated')) == b'isolated'
connection = sqlite3.connect(':memory:')
assert connection.execute('SELECT 41 + 1').fetchone() == (42,)
connection.close()
print('CP1a 基础模块及隔离宿主通过')
