# 精确Runtime字典保存方法与根描述符：活绑定、原值身份、参数检查和键错误均按普通协议。
marker = object()
mapping = {"key": marker}
getter = mapping.__getitem__
root = dict.__getitem__

def retrieve(x):
    return getter(x)

print(getter.__self__ is mapping, getter.__name__, getter.__qualname__)
print(retrieve("key") is marker)
mapping["key"] = 23
print(retrieve("key"), root(mapping, "key"))
for arguments in [(), ("key", "extra")]:
    try:
        getter(*arguments)
    except TypeError as error:
        print(type(error).__name__, error.args, str(error))
try:
    getter(key="key")
except TypeError as error:
    print(type(error).__name__, error.args, str(error))
for receiver in [mapping, 1]:
    try:
        root(receiver)
    except TypeError as error:
        print(type(error).__name__, error.args, str(error))
for key in ["missing", []]:
    try:
        retrieve(key)
    except Exception as error:
        print(type(error).__name__, error.args, str(error))
