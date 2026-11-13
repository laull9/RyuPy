# ryupy: pending
"""CP3：NumPy 原生 ufunc 实际调用已编译函数，并保留异常实例。"""
import numpy as np

calls = []
marker = ValueError('编译回调错误')


def compiled_transform(value):
    calls.append(value)
    return value * value + 1


def compiled_failure(value):
    if value == 2:
        raise marker
    return value


ufunc = np.frompyfunc(compiled_transform, 1, 1)
result = ufunc(np.array([1, 2, 3]))
assert result.tolist() == [2, 5, 10]
assert calls == [1, 2, 3]
try:
    np.frompyfunc(compiled_failure, 1, 1)(np.array([1, 2, 3]))
except ValueError as error:
    assert error is marker
    assert str(error) == '编译回调错误'
else:
    raise AssertionError('编译回调异常未往返')
# 重复创建和释放真实 ufunc；宿主计数门由验收器另行检查。
for index in range(128):
    transient = np.frompyfunc(compiled_transform, 1, 1)
    assert transient(index) == index * index + 1
    del transient
del ufunc
del result
print('CP3 编译回调及异常实例通过')
