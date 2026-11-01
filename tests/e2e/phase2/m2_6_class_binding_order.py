# ryupy: pending
# 后续同名 class 不能覆盖此前 except as 的动态模块绑定；独立登记既有名字解析欠账。
try:
    raise ValueError("earlier")
except ValueError as Later:
    print(isinstance(Later, ValueError))

class Later(Exception):
    pass

print(issubclass(Later, Exception))
