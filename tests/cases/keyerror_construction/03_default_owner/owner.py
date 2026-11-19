# 真实InstanceGlobals暴露共享默认字典，并保每个函数定义时捕获的custom owner。
shared = __builtins__
original = KeyError


def first_error(value):
    return ['owner-first', value]


def second_error(value):
    return ['owner-second', value]


first = {'KeyError': first_error}
second = {'KeyError': second_error}
__builtins__ = first


def old():
    return KeyError('owner')


__builtins__ = second


def fresh():
    return KeyError('owner')


__builtins__ = shared
