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
