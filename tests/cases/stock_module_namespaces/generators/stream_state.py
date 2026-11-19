"""暂停点保存真实模块环境；外部改写和finally副作用都可在下一次恢复观察。"""
events = []
marker = 10
original = globals()


def sequence(seed):
    global marker
    events.append(('start', seed, marker))
    try:
        change = yield (seed + marker, globals() is original)
        events.append(('send', change, marker))
        marker += change
        yield marker
        return 'complete'
    finally:
        events.append(('close', marker))


def delegated(seed):
    result = yield from sequence(seed)
    return ('delegated', result)


def lazy():
    return (marker + item for item in [0, 1, 2])


def default_length(value):
    return 70


def captured_builtins():
    saved = globals()['__builtins__']
    globals()['__builtins__'] = {'len': default_length}
    result = (len(value) for value in ([1], [1, 2]))
    globals()['__builtins__'] = saved
    return result


def delayed(globals, consume):
    # 同名参数是真实callee；其结果必须跨暂停保留，恢复时不能重读或重调。
    return consume(globals(), (yield 'ready'))
