# ryupy: pending
# 编译应用只调用通用CPython模块代理；结构、指针与本机回调由ctypes执行。
import ffi_demo


def twice(value):
    return value * 2


def fail(value):
    raise ValueError("callback failure")


print(ffi_demo.structure())
print(ffi_demo.owned_buffer())
print(ffi_demo.callback(twice))
try:
    ffi_demo.callback(fail)
except ValueError as error:
    print(str(error))
