"""显式 ctypes 适配：布局由头文件合同固定，native-owned 内存总在 finally 中配对释放。"""
import ctypes
from pathlib import Path
import sys


class Pair(ctypes.Structure):
    _fields_ = [('x', ctypes.c_int32), ('y', ctypes.c_double)]


_library = ctypes.CDLL(str(Path(__file__).with_name('complex.' +
    ('dylib' if sys.platform == 'darwin' else 'so'))))
_library.pair_transform.argtypes = [Pair]
_library.pair_transform.restype = Pair
_library.buffer_create.argtypes = [ctypes.c_void_p, ctypes.c_size_t]
_library.buffer_create.restype = ctypes.c_void_p
_library.buffer_release.argtypes = [ctypes.c_void_p]
_library.buffer_release.restype = None
_library.active_buffers.argtypes = []
_library.active_buffers.restype = ctypes.c_int32
_callback = ctypes.CFUNCTYPE(ctypes.c_int32, ctypes.c_int32)
_library.callback_apply.argtypes = [_callback, ctypes.c_int32]
_library.callback_apply.restype = ctypes.c_int32


def structure():
    result = _library.pair_transform(Pair(5, 1.25))
    return result.x == 12 and result.y == 2.5


def owned_buffer():
    value = b'a\x00\xffz'
    source = ctypes.create_string_buffer(value)
    for _ in range(128):
        pointer = _library.buffer_create(source, len(value))
        if not pointer:
            raise MemoryError('buffer_create')
        try:
            assert ctypes.string_at(pointer, len(value)) == value
            assert _library.active_buffers() == 1
        finally:
            _library.buffer_release(pointer)
    return _library.active_buffers() == 0


def callback(callable):
    # 回调的Python错误先记录，在C正常返回后重抛，禁止异常穿越C栈帧。
    failures = []
    def invoke(value):
        try:
            return callable(value)
        except BaseException as error:
            failures.append(error)
            return 0
    rooted = _callback(invoke)
    result = _library.callback_apply(rooted, 20)
    if failures:
        raise failures[0]
    return result


def import_hook_is_private():
    import builtins
    return all(not hasattr(builtins, name) for name in
        ('_rp_has', '_rp_load', '_rp_package', '_rp_sys', '_RyuPyLoader', '_RyuPyFinder'))


def exception_type(parent, child, invoke):
    assert isinstance(parent, type) and issubclass(parent, ValueError)
    assert child.__base__ is parent
    assert parent.__module__ == child.__module__ == '__main__'
    assert parent.__name__ == parent.__qualname__ == 'CustomError'
    try:
        invoke()
    except child as error:
        assert type(error) is child
        assert isinstance(error, parent)
        assert str(error) == 'compiled custom'
    else:
        raise AssertionError('编译异常未以真实类型传播')
    return child('python custom')


def distinct_types(first, second):
    return first is not second and first.__name__ == second.__name__ == 'SameError'


def raise_derived(parent):
    global DerivedError
    class DerivedError(parent):
        pass
    raise DerivedError('python derived')


def derived_type():
    return DerivedError


def consume(value):
    pass


def mutate_type(value):
    value.marker = 1


def nested_list():
    return [1, {'x': [2]}]


def nested_dict():
    return {'a': [1]}


def equal_pair(first, second):
    return first == second


class MutatingValue:
    def __init__(self, action):
        self.action = action
    def __eq__(self, other):
        self.action()
        return True


def mutating_list(action):
    return [MutatingValue(action), 99]


def mutating_single(action):
    return [MutatingValue(action)]


def mutating_dict(action):
    return {'a': MutatingValue(action), 'b': 2}


_order_token = object()


class OrderingValue:
    def __eq__(self, other):
        return False
    def __lt__(self, other):
        return _order_token
    def __gt__(self, other):
        return _order_token


def ordering_list():
    return [OrderingValue()]


def order_token():
    return _order_token


def cycle():
    value = []
    value.append(value)
    return value


class CustomKey:
    pass


def custom_key_dict():
    return {CustomKey(): 2}


def make_set():
    return {1, 2}


def make_frozenset():
    return frozenset([1, 2])
