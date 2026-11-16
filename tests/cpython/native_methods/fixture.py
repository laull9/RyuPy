"""C1 多版本原生描述符宿主输入；oracle 与 shim 同次执行同一消费者断言。"""
from bridge_fixture.method_descriptor_contract import verify
import gc
import weakref


class Subject:
    pass


def target(self, x, *, z=1):
    if x < 0:
        raise ValueError('native method exception')
    return self.offset + x + z


def class_target(cls):
    return cls.__name__


def static_target(value):
    return value * 2


def property_target(self):
    return self.offset


def test(native):
    value = Subject()
    value.offset = 7
    assert Subject.kind() == 'Subject'
    assert value.twice(3) == 6
    assert value.observed_offset == 7
    try:
        value.sample(-1)
    except ValueError as error:
        assert error.args == ('native method exception',)
    else:
        raise AssertionError('native method lost original exception')
    assert verify(Subject, value, native)
    descriptor = Subject.sample
    reference = weakref.ref(descriptor)
    assert reference() is descriptor
    # CPython GC 确实能看见方法普通属性中的自环；弱引用不能把它钉住。
    descriptor.cycle = descriptor
    del descriptor.tag
    del Subject.sample
    del descriptor
    gc.collect()
    if native:
        assert reference() is None
    print('native method contract passed')
