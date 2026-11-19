# 已完成Handle的同一实例再次显式raise必须保留旧历史。
def fail():
    raise ValueError("reused")

try:
    fail()
except ValueError as caught:
    saved = caught
try:
    raise saved
except ValueError as caught:
    result = caught
