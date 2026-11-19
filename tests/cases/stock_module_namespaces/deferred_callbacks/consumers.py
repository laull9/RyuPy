"""独立消费者覆盖忽略、条件读取、参数重绑、闭包逃逸及旧 variadic 入口。"""
readers = []


def ignore(payload):
    payload = None
    return payload


def dispatch(enabled, payload):
    if enabled:
        return payload
    return None


def capture(payload):
    readers.append(lambda: payload)
    return payload


def fail_before_read(payload):
    raise ValueError("回调主动失败")


def variadic(*args):
    assert args[0] is args[1]
    return args[0]
