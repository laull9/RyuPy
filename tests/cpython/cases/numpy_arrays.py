"""CP3：真实 ndarray/dtype、视图别名和独立向量运算；禁止 list 替代。"""
import numpy as np

assert np.__version__ == '2.3.5'
array = np.arange(12, dtype='int64').reshape(3, 4)
assert isinstance(array, np.ndarray)
assert array.dtype == np.dtype('int64')
assert array.shape == (3, 4)
view = array[:, 1:3]
assert np.shares_memory(array, view)
view[0, 0] = 99
assert array[0, 1] == 99
# 删除局部 owner 后视图仍拥有有效 backing storage。
del array
assert int(view[0, 0]) == 99
left = np.arange(8, dtype='float64')
right = left * 2 + 1
assert float(np.dot(left, right)) == 308.0
assert np.array_equal(right, np.array([1, 3, 5, 7, 9, 11, 13, 15]))
try:
    np.arange(6).reshape(4, 2)
except ValueError as error:
    assert 'reshape' in str(error)
else:
    raise AssertionError('NumPy ValueError 未跨桥传播')
print('CP3 ndarray、视图、向量计算通过')
