# 编译应用只调用通用CPython模块代理；结构、指针与本机回调由ctypes执行。
import ffi_demo
import first_exception
import second_exception
from urllib import parse as first_parse
import urllib.parse as second_parse
import sys
import types

assert first_parse is second_parse
assert first_parse is sys.modules['urllib.parse']
assert ffi_demo.import_hook_is_private()
assert type(first_parse) is types.ModuleType


class CustomError(ValueError):
    pass


class ChildError(CustomError):
    pass


assert ffi_demo.distinct_types(first_exception.SameError, second_exception.SameError)


def custom_failure():
    raise ChildError('compiled custom')


returned = ffi_demo.exception_type(CustomError, ChildError, custom_failure)
assert type(returned) is ChildError
try:
    raise returned
except CustomError as error:
    assert error is returned
    assert str(error) == 'python custom'
else:
    raise AssertionError('跨域异常继承匹配失败')
try:
    ffi_demo.raise_derived(CustomError)
except CustomError as derived:
    assert type(derived) is ffi_demo.derived_type()
    assert isinstance(derived, CustomError)
    assert str(derived) == 'python derived'
else:
    raise AssertionError('CPython派生异常未匹配编译祖先')


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
