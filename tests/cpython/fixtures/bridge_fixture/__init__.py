"""必须由 CPython provider 执行的包无关对象桥辅助程序。"""
import threading

import_attempts = 0
retained = None
exception_events = []
last_error = None


class UserError(ValueError):
    """真实用户异常子类，继承关系由type对象表达。"""


MisleadingValueError = type('ValueError', (TypeError,), {})


def echo(value):
    return value


def same(left, right):
    return left is right


def make_text():
    return ''.join(['Python', ' 动态字符串 ', str(123456789)])


def raise_user_error():
    raise UserError('用户子类')


def raise_misleading_error():
    raise MisleadingValueError('同名不同继承')


def hold_callback(callback):
    return {'callback': callback}


def clear_holder(holder):
    holder.clear()


def mutate_and_callback(value, callback):
    value['items'].append('Python前')
    callback(value)
    assert value['items'][-1] == '编译回调'
    value['items'].append('Python后')
    return value


def mutate_then_fail(value):
    value['items'].append('异常前写入')
    raise ValueError('写回异常')


def verify_shared_cycle(value):
    assert value['left'] is value['right']
    assert value['left'][0] is value
    value['left'].append('环内Python写入')
    return value


def hash_values():
    # 全部由CPython重新创建，避免echo返回编译侧缓存掩盖hash不一致。
    return [''.join(['跨域', '哈希', str(123456789)]),
            bytes([0, 1, 200, 255]), int('123456789012345678901234567890'),
            tuple([''.join(['元', '组']), int('98765432101234567890')])]


def key_count(left, right):
    mapping = {left: 'first'}
    assert mapping[right] == 'first'
    mapping[right] = 'second'
    assert len(mapping) == 1
    assert len({left, right}) == 1
    return mapping[left]


def retain_object():
    global retained
    retained = {'value': 42}
    return retained


def get_retained():
    return retained


def drop_retained():
    global retained
    retained = None


def replace_math_sqrt(callback):
    import math
    old = math.sqrt
    math.sqrt = callback
    return old


class LazyError(ValueError):
    """异常捕获本身不能触发用户字符串化或属性读取副作用。"""
    def __str__(self):
        exception_events.append('str')
        return '延迟异常展示'

    def __getattribute__(self, name):
        if name in ('args', '__cause__', '__context__'):
            exception_events.append('getattribute:' + name)
        return super().__getattribute__(name)


def raise_lazy():
    global last_error
    last_error = LazyError('参数')
    raise last_error


def append(value, item):
    value.append(item)


def assign(value, key, item):
    value[key] = item


def call(callback, value):
    return callback(value)


def call_noargs(callback):
    return callback()


def chain(callback):
    try:
        callback()
    except Exception as error:
        raise RuntimeError('Python 包装异常') from error


def external_thread(callback, value):
    # join 是受控生命周期，异常返回主线程并保留原实例。
    results = []
    errors = []

    def invoke():
        try:
            results.append(callback(value))
        except BaseException as error:
            errors.append(error)

    worker = threading.Thread(target=invoke)
    worker.start()
    worker.join(timeout=5)
    assert not worker.is_alive(), '外线程回调死锁'
    if errors:
        raise errors[0]
    return results[0]


def capture_exception_state(callback):
    """解释域直接观察编译异常的真实实例和抑制/链状态。"""
    try:
        callback()
    except BaseException as error:
        return error, error.__suppress_context__, error.__context__, error.__cause__, error.args
    raise AssertionError('callback没有抛错')


def rewrite_exception_state(error, cause, context, suppress):
    """同一异常反复变更链后重抛，测试已有代理状态刷新。"""
    error.__cause__ = cause
    error.__context__ = context
    error.__suppress_context__ = suppress
    raise error


def mutate_exception_without_raise(error, cause, context, suppress, arguments):
    """修改已缓存异常后正常返回；下一次导出必须读取最新origin状态。"""
    error.__cause__ = cause
    error.__context__ = context
    error.__suppress_context__ = suppress
    error.args = arguments
