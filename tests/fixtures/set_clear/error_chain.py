# 保存 clear 的绑定错误保全部用户帧/context；finally 证实失败没有修改原集合。
def inner(values):
    method = values.clear
    return method(0, bad=1)


def outer(values):
    return inner(values)


values = {10, 20}
try:
    raise ValueError('outer context')
except ValueError:
    try:
        outer(values)
    finally:
        print('after failure', sorted(values))
