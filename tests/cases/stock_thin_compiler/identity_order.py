# ryupy: pending
# 身份比较按左至右活查名称；条件表达式仅读取选中臂，删除与重绑定即时可见。
left = object()
right = left

def compare(x):
    return left is right

def selected(x):
    return left if x is None else right

def unicode_missing(x):
    return 雪 is x

print(compare(None))
globals()["right"] = object()
print(compare(None))
del right
print(selected(None) is left)
try:
    compare(None)
except NameError as error:
    print(type(error) is NameError, error.name, error.args, str(error))
del left
try:
    compare(None)
except NameError as error:
    print(type(error) is NameError, error.name, error.args, str(error))
try:
    unicode_missing(None)
except NameError as error:
    print(type(error) is NameError, error.name, error.args, str(error))
globals()["left"] = None
globals()["right"] = None
print(compare(None), selected(None) is None)
