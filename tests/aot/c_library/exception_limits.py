# ryupy: pending
"""受限异常类型导出的明确诊断：失败不冻结，成功后两个执行域均拒绝类修改。"""
import ffi_demo
import set_limits


class WithField(ValueError):
    marker = 1


try:
    ffi_demo.consume(WithField)
except NotImplementedError as error:
    assert 'marker' in str(error)
else:
    raise AssertionError('自定义类字段未拒绝')
WithField.marker = 2
assert WithField.marker == 2


class WithString(ValueError):
    def __str__(self):
        return 'custom'


try:
    ffi_demo.consume(WithString)
except NotImplementedError as error:
    assert '__str__' in str(error)
else:
    raise AssertionError('自定义字符串协议未拒绝')


class WithInstanceField(ValueError):
    pass


instance = WithInstanceField('value')
instance.marker = 3
try:
    ffi_demo.consume(instance)
except NotImplementedError as error:
    assert 'instance attributes' in str(error)
else:
    raise AssertionError('实例字段未拒绝')
WithInstanceField.marker = 4
assert WithInstanceField.marker == 4


class Frozen(ValueError):
    pass


ffi_demo.consume(Frozen)
try:
    Frozen.__dict__['marker'] = 5
except TypeError:
    pass
else:
    raise AssertionError('已导出类字典不是只读视图')
try:
    Frozen.marker = 5
except NotImplementedError as error:
    assert 'exported exception type' in str(error)
else:
    raise AssertionError('已导出类的Runtime字段修改未拒绝')
try:
    del Frozen.__module__
except NotImplementedError as error:
    assert 'exported exception type' in str(error)
else:
    raise AssertionError('已导出类的Runtime元数据删除未拒绝')
try:
    ffi_demo.mutate_type(Frozen)
except TypeError as error:
    assert 'immutable type' in str(error)
else:
    raise AssertionError('已导出类的CPython字段修改未拒绝')
print('异常类型边界诊断通过')
