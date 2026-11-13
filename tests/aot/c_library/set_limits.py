"""两个容器模式共享同一set/frozenset限制断言，禁止静默返回错误比较值。"""
import ffi_demo

try:
    ffi_demo.make_set() == {1, 2}
except NotImplementedError as error:
    assert 'set/frozenset' in str(error)
else:
    raise AssertionError('set混合比较未明确拒绝')
try:
    ffi_demo.make_frozenset() == frozenset([1, 2])
except NotImplementedError as error:
    assert 'set/frozenset' in str(error)
else:
    raise AssertionError('frozenset混合比较未明确拒绝')
