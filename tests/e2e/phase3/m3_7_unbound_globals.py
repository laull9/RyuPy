# 未绑定全局名必须在真正执行读取时抛 NameError；不可假注册 Python 2 名称。
import sys

if sys.version_info >= (3, 0):
    text_type = str
else:
    text_type = unicode
print(text_type('兼容分支'))

try:
    unicode
except NameError as error:
    print(type(error).__name__, str(error))

# 任意普通未知名字与 Python 2 名称使用相同路径。
try:
    absent_global_probe
except NameError as error:
    print(type(error).__name__, str(error))


def read_later():
    return bound_after_read


try:
    read_later()
except NameError:
    print('首次未绑定')
bound_after_read = 42
print(read_later())


def read_missing():
    return absent_inside_function


try:
    read_missing()
except NameError as error:
    print(type(error).__name__, str(error))


# global 后续写入仍使用同一个槽位，None 与未绑定哨兵不同。
def bind_missing():
    global assigned_global
    assigned_global = None


try:
    assigned_global
except NameError:
    print('global 初值缺失')
bind_missing()
print(assigned_global is None)


# 函数内只要存在赋值，读取就不是全局查找，仍为 UnboundLocalError。
local_shadow = '外层'


def read_local():
    print(local_shadow)
    local_shadow = '内层'


try:
    read_local()
except UnboundLocalError:
    print('局部仍按词法绑定')
print(local_shadow)
