"""必须由 CPython provider 执行的包无关对象桥辅助程序。"""
import threading

import_attempts = 0
retained = None


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


def append(value, item):
    value.append(item)


def assign(value, key, item):
    value[key] = item


def call(callback, value):
    return callback(value)


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
