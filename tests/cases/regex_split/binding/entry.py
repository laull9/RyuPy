# 动态调用的绑定/转换错误合同：完整消息冻结，副作用次数与异常身份明确断言。
import re

events = []
marker = LookupError('index marker')
class Index:
    def __init__(self, value):
        self.value = value
    def __index__(self):
        events.append('index')
        return self.value
class Boom:
    def __index__(self):
        events.append('boom')
        raise marker
class IntOnly:
    def __int__(self):
        events.append('int')
        return 1


def failure(label, call, kind, expected_events):
    events.clear()
    try:
        call()
    except Exception as error:
        assert type(error).__name__ == kind
        assert events == expected_events
        if kind == 'LookupError':
            assert error is marker
        print(label, type(error).__name__, str(error), events)
    else:
        assert False


split = re.split
pattern = re.compile(',')
saved = pattern.split
failure('module-missing', lambda: split(), 'TypeError', [])
failure('module-string-missing', lambda: split(','), 'TypeError', [])
failure('module-arity', lambda: split(',', 'a,b', 0, 0, 0), 'TypeError', [])
failure('module-unknown', lambda: split(',', 'a,b', unknown=1), 'TypeError', [])
failure('module-duplicate', lambda: split(',', 'a,b', 1, maxsplit=2), 'TypeError', [])
failure('bound-missing', lambda: saved(), 'TypeError', [])
failure('bound-arity', lambda: saved('a,b', 0, 0), 'TypeError', [])
failure('bound-flags', lambda: saved('a,b', flags=0), 'TypeError', [])
failure('bound-pos', lambda: saved('a,b', pos=1), 'TypeError', [])
failure('bound-duplicate', lambda: saved('a,b', 1, maxsplit=2), 'TypeError', [])
failure('binding-before-compile', lambda: split('[', None, Boom(), unknown=1), 'TypeError', [])
failure('compile-before-index', lambda: split('[', None, Boom()), 'error', [])
failure('index-before-subject', lambda: split(',', None, Boom()), 'LookupError', ['boom'])
failure('subject-after-index', lambda: split(',', None, Index(1)), 'TypeError', ['index'])
failure('binding-before-index', lambda: saved(None, Boom(), unknown=1), 'TypeError', [])
failure('negative-subject', lambda: saved(None, -1), 'TypeError', [])
failure('domain-after-index', lambda: saved(b'a,b', Index(1)), 'TypeError', ['index'])
failure('compiled-flags-before-index', lambda: split(pattern, None, Boom(), re.I), 'ValueError', [])
failure('bytes-on-text', lambda: split(b',', 'a,b'), 'TypeError', [])
failure('subject-int', lambda: split(b',', 42), 'TypeError', [])
failure('subject-list', lambda: split(b',', [1, 2]), 'TypeError', [])
failure('pattern-bytearray', lambda: split(bytearray(b','), b'a,b'), 'TypeError', [])
for count in (None, 1.0, IntOnly(), Index('bad'), 2**63, -2**63 - 1):
    expected = ['index'] if isinstance(count, Index) else []
    kind = 'OverflowError' if type(count) is int else 'TypeError'
    failure('maxsplit-' + type(count).__name__, lambda: saved(None, count), kind, expected)
for count in (1, -1, 2**40):
    events.clear()
    result = saved('a,b,c', Index(count))
    assert events == ['index']
    assert result == (['a,b,c'] if count < 0 else ['a', 'b,c'] if count == 1 else ['a', 'b', 'c'])
    print('index-once', count, result, events)
assert split(pattern=',', string='a,b,c', maxsplit=1, flags=0) == ['a', 'b,c']
assert saved(string='a,b,c', maxsplit=1) == ['a', 'b,c']
print('keywords', saved(string='a,b,c', maxsplit=1))

# 先验证缓冲区有效性，再判断文本/二进制域；负限制仍必须验证。
released = memoryview(b'a,b')
released.release()
failure('negative-released', lambda: split(',', released, -1), 'TypeError', [])
# 模块绑定按关键字的出现次序报告未知参数或重复参数。
failure('unknown-before-duplicate', lambda: split(',', 'x', wat=1, string='x'), 'TypeError', [])
failure('duplicate-before-unknown', lambda: split(',', 'x', string='x', wat=1), 'TypeError', [])
