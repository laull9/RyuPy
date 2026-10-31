# ryupy: pending
# M4.1：真实 NumPy 导入、from、子模块与缓存身份。
import numpy as np
import numpy as again
import numpy.linalg as linalg
from numpy import array

print(np is again, linalg is np.linalg)
values = array([1, 2, 3], dtype=np.int64)
print(values.tolist(), str(values.dtype), int(values.sum()))
print(round(float(linalg.det(array([[1.0, 2.0], [3.0, 4.0]]))), 6))
