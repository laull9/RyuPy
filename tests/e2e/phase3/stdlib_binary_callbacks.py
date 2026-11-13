# 参数回调必须观察活动 buffer；C 入口持导出租约，Python base64 层遵循各自物化时点。
import base64
import binascii


class Change:
    def __init__(self, data):
        self.data = data

    def __index__(self):
        self.data[0] = 98
        return 0

    def __bool__(self):
        self.data[0] = 98
        return True


for operation in [lambda data, flag: binascii.hexlify(data, b":", flag),
                  lambda data, flag: binascii.crc32(data, flag),
                  lambda data, flag: binascii.b2a_base64(data, newline=flag),
                  lambda data, flag: binascii.a2b_base64(data, strict_mode=flag),
                  lambda data, flag: base64.b64decode(data, validate=flag),
                  lambda data, flag: base64.b16decode(data, casefold=flag),
                  lambda data, flag: base64.b32decode(data, casefold=flag)]:
    data = bytearray(b"AAAAAAAA")
    print(operation(data, Change(data)))


class Release:
    def __init__(self, view):
        self.view = view

    def __bool__(self):
        self.view.release()
        return True


for operation in [lambda data, flag: binascii.b2a_base64(data, newline=flag),
                  lambda data, flag: binascii.a2b_base64(data, strict_mode=flag),
                  lambda data, flag: base64.b64decode(data, validate=flag)]:
    view = memoryview(b"AAAA")
    try:
        print(operation(view, Release(view)))
    except BufferError as error:
        print(type(error).__name__, str(error))
    view.release()


class Resize:
    def __init__(self, data):
        self.data = data

    def __index__(self):
        self.data.append(65)
        return 0

    def __bool__(self):
        self.data.append(65)
        return True


for operation in [lambda data, flag: binascii.hexlify(data, b":", flag),
                  lambda data, flag: binascii.crc32(data, flag),
                  lambda data, flag: binascii.b2a_base64(data, newline=flag),
                  lambda data, flag: binascii.a2b_base64(data, strict_mode=flag),
                  lambda data, flag: base64.b64decode(data, validate=flag)]:
    data = bytearray(b"AAAA")
    try:
        operation(data, Resize(data))
    except BufferError as error:
        print(type(error).__name__, str(error))
    data.append(66)
    print(data)
