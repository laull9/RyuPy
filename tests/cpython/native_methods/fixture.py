"""C1 多版本原生描述符宿主输入；oracle 与 shim 同次执行同一消费者断言。"""
from bridge_fixture.method_descriptor_contract import verify
import gc
import weakref


class Subject:
    pass


def target(self, x, *, z=1):
    return self.offset + x + z


def test(native):
    value = Subject()
    value.offset = 7
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
