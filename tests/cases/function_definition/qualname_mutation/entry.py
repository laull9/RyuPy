# 函数定义来源与限定名语义验收。
# 限定名是真实可变成员；错误写删和公开字典同名条目不能改变描述符语义。
def target(value):
    return value * 2

assert target.__qualname__ == "target"
assert "__qualname__" not in target.__dict__
target.__dict__["__qualname__"] = "dictionary-spoof"
assert target.__qualname__ == "target"
target.__qualname__ = "Namespace.changed"
assert target.__qualname__ == "Namespace.changed"
assert target.__dict__["__qualname__"] == "dictionary-spoof"
target.__name__ = "short-name"
assert target.__qualname__ == "Namespace.changed"
assert target.__name__ == "short-name" and target(5) == 10

failures = []
for invalid in [None, 8, False, ["name"], {"name": "value"}]:
    try:
        target.__qualname__ = invalid
    except TypeError as error:
        failures.append((type(error).__name__, str(error)))
    else:
        raise AssertionError("非法限定名写入未报错")
    assert target.__qualname__ == "Namespace.changed"

try:
    del target.__qualname__
except TypeError as error:
    failures.append((type(error).__name__, str(error)))
else:
    raise AssertionError("限定名删除未报错")

assert failures == [("TypeError", "__qualname__ must be set to a string object")] * 6
assert target.__qualname__ == "Namespace.changed"
del target.__dict__["__qualname__"]
assert target.__qualname__ == "Namespace.changed"
target.__qualname__ = ""
assert target.__qualname__ == ""
target.__qualname__ = "名字.函数"
assert target.__qualname__ == "名字.函数"
print(target.__name__, target.__qualname__, target(5))
print(target.__dict__)
for failure in failures:
    print(failure)
