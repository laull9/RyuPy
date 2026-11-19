
# 精确 dict 更新的完整异常链、身份及调用帧；不能仅比较最后错误消息。
cause = ValueError('dict-update-cause')
ambient = RuntimeError('dict-update-ambient')
source_error = LookupError('dict-update-source')
target = {'old': 0}

def pairs():
    yield ('prefix', 1)
    raise source_error

def inner():
    return target.update(pairs(), tail=2)

def middle():
    return inner()

def outer():
    return middle()

try:
    raise cause
except ValueError as caught:
    assert caught is cause
try:
    raise ambient from cause
except RuntimeError as caught:
    assert caught is ambient
    assert caught.__cause__ is cause
    assert caught.__suppress_context__ is True
    try:
        outer()
    except LookupError as propagated:
        assert propagated is source_error
        assert propagated.__cause__ is None
        assert propagated.__context__ is ambient
        assert propagated.__suppress_context__ is False
        assert target == {'old': 0, 'prefix': 1}
        print('dict-update.chain-identity', target)
        raise
