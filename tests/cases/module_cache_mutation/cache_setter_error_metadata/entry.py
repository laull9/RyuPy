# ryupy: pending
# 独立精确CP合同：类型、str与args原样比较，不以通用binder旧措辞替换oracle。
import sys
import mc_state
import mc_subject
subject = mc_subject
importer = subject.read.__builtins__['__import__']
cache = sys.modules
setter = cache.__setitem__
saved = [setter]
message = 'wrapper __setitem__() takes no keyword arguments'
def check(label, action, name):
    assert name not in cache
    try:
        action()
    except TypeError as exc:
        assert type(exc) is TypeError
        assert str(exc) == message
        assert exc.args == (message,)
        assert name not in cache
        print(label, type(exc).__name__, str(exc), exc.args)
    else:
        raise AssertionError('keyword setter must fail: ' + label)
    # 被拒的名字随后仍能合法使用，确认错误路径没有残留登记。
    assert setter(name, subject) is None
    assert cache[name] is subject and importer(name) is subject
check('all-keywords', lambda: setter(key='mc_metadata_all', value=subject), 'mc_metadata_all')
check('keyword-value', lambda: saved[0]('mc_metadata_value', value=subject), 'mc_metadata_value')
check('unknown-keyword', lambda: cache.__setitem__('mc_metadata_unknown', subject, unknown=1), 'mc_metadata_unknown')
assert sys.modules['mc_subject'] is subject and mc_state.runs == 1
print('exact setter errors retain cache/source identity', True)
