# ryupy: pending
# 普通标注全局删除后未绑定，重新赋值仍恢复数值语义；None 不是未绑定标记。
counter: int = 21

def twice() -> int:
    return counter * 2

assert twice() == 42
del counter
try:
    twice()
except NameError as error:
    assert str(error) == "name 'counter' is not defined"
    print('deleted typed global', type(error).__name__, str(error))
else:
    assert False
counter = 8
assert twice() == 16 and type(counter) is int
nullable = None
del nullable
try:
    del nullable
except NameError as error:
    assert str(error) == "name 'nullable' is not defined"
    print('deleted None binding', type(error).__name__, str(error))
else:
    assert False
nullable = None
assert nullable is None
print('module reassignment', twice(), nullable)
