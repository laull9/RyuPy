# ryupy: pending
# M4.1：扩展对象别名、视图修改、基础运算与异常传播。
import numpy as np

values = np.array([[1, 2], [3, 4]], dtype=np.int64)
alias = values
view = values[:, 0]
view[0] = 9
print(alias is values, values.tolist(), list(values.shape))
print((values + 1).tolist(), int(values[0, 0]))
try:
    values.reshape(3)
except ValueError:
    print("shape error")
try:
    print(values[10])
except IndexError:
    print("numpy index error")
