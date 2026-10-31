# ryupy: pending
# M3.5：JSON 保留大整数和 Unicode，数学错误采用 Python 异常。
import json
import math
import time

value = {"猫": [10**30, True, None], "later": 2}
encoded = json.dumps(value, ensure_ascii=False, separators=(",", ":"))
print(encoded, json.loads(encoded) == value)
try:
    json.loads('{"a":}')
except json.JSONDecodeError as error:
    print(error.lineno, error.colno)
print(math.sqrt(9), math.floor(-1.2), math.isnan(float("nan")))
try:
    math.sqrt(-1)
except ValueError:
    print("domain error")
print(time.monotonic() <= time.monotonic())
