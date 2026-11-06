# ryupy: pending
"""mutable sub的count/模板/subject顺序、late join、原异常与完整清理链；候选未执行。"""
import re


RELEASED = "expected string or bytes-like object, got 'memoryview'"


def expect_error(label, kind, message, call):
    try:
        call()
    except Exception as error:
        assert type(error) is kind and str(error) == message
        print(label, type(error).__name__, str(error))
        return error
    raise AssertionError('expected error: ' + label)


p = re.compile(b'a')
for name in ('bytearray', 'view'):
    data = bytearray(b'aa')
    source = memoryview(data) if name == 'view' else data
    kept = []
    marker = RuntimeError('callback-' + name)

    def failing(match):
        kept.append(match)
        assert match.string is source and match.span() == (0, 1)
        raise marker

    try:
        p.subn(failing, source)
    except RuntimeError as caught:
        assert caught is marker and len(kept) == 1
    else:
        raise AssertionError('callback identity lost')
    assert kept[0].group() == b'a' and kept[0].endpos == 2
    if name == 'view': source.release()
    data.append(33)
    print('S3.callback-identity-' + name, kept[0].span(), data)
    if name == 'view':
        expect_error('S3.retained-released', TypeError, RELEASED, kept[0].group)
    else:
        assert kept[0].group() == b'a'

# G原无效模板合同：准备阶段失败不能留下本次lease。
data = bytearray(b'aa')
view = memoryview(data)
expect_error('S3.invalid-template', re.error, 'bad escape \\x at position 0',
             lambda: p.sub(br'\x', view))
view.release()
data.append(33)
print('S3.invalid-template-clean', data)

events = []


class Count:
    def __init__(self, action, result):
        self.action = action
        self.result = result

    def __index__(self):
        events.append('count')
        self.action()
        return self.result


def idle():
    pass


# 新count-release组合首次CP守门：负count也必须取得/验证source。
for count in (-1, 0):
    data = bytearray(b'aa')
    view = memoryview(data)
    events = []
    expect_error('S3.count-release-' + str(count), TypeError, RELEASED,
                 lambda: p.sub(b'X', view, Count(view.release, count)))
    assert events == ['count']
    data.append(33)
    print('S3.count-release-clean-' + str(count), events, data)
data = bytearray(b'aa')
view = memoryview(data)
events = []
expect_error('S3.template-before-subject', re.error, 'bad escape \\x at position 0',
             lambda: p.sub(br'\x', view, Count(view.release, -1)))
assert events == ['count']
data.append(33)
print('S3.template-before-clean', events, data)
data = bytearray(b'aa')
view = memoryview(data)
events = []
expect_error('S3.count-before-template', TypeError, '__index__ returned non-int (type str)',
             lambda: p.sub(br'\x', view, Count(idle, 'bad')))
assert events == ['count']
view.release()
data.append(33)
print('S3.count-before-clean', events, data)

# count转换的resize发生在本次acquire之前；N与Match.endpos按修改后的长度冻结。
data = bytearray(b'a')
seen = []
events = []


def grow():
    data.extend(b'a')


def changed_length(match):
    seen.append((match.group(), match.span(), match.pos, match.endpos, match.string is data))
    return b'X'


assert p.subn(changed_length, data, Count(grow, 1)) == (b'Xa', 1)
assert events == ['count'] and seen == [(b'a', (0, 1), 0, 2, True)]
data.append(33)
print('S3.count-resize', events, seen, data)

# 普通错误返回仅在所有必要callback后join；有前缀时错误item编号为1，不提前拒绝。
for name, returned, typename in [('str', 'bad', 'str'), ('int', 3, 'int')]:
    data = bytearray(b'zaa')
    view = memoryview(data)
    seen = []

    def wrong_result(match):
        seen.append(match.span())
        return returned

    expect_error('S3.late-result-' + name, TypeError,
                 'sequence item 1: expected a bytes-like object, ' + typename + ' found',
                 lambda: p.sub(wrong_result, view))
    assert seen == [(1, 2), (2, 3)]
    view.release()
    data.append(33)
    print('S3.late-result-clean-' + name, seen, data)

data = bytearray(b'aa')
view = memoryview(data)
seen = []
marker = LookupError('later callback marker')


def later_error(match):
    seen.append(match.span())
    if len(seen) == 1:
        return 'wrong-domain'
    raise marker


try:
    p.sub(later_error, view)
except LookupError as caught:
    assert caught is marker and seen == [(0, 1), (1, 2)]
else:
    raise AssertionError('later callback must win over join validation')
view.release()
data.append(33)
print('S3.later-error', seen, data)
print('S3.protocol-complete', True)

# 终尾每异常只抛一次、ambient在模块建立；finally必须在原错误传播时证实lease已清。
trace_data = bytearray(b'a')
trace_view = memoryview(trace_data)
trace_pattern = re.compile(b'a')
trace_kept = []


def callback_leaf(match):
    trace_kept.append(match)
    raise ValueError('sub-callback-body')


def callback_middle(match):
    return callback_leaf(match)


def sub_middle():
    return trace_pattern.sub(callback_middle, trace_view)


def sub_outer():
    return sub_middle()


try:
    raise LookupError('sub-cause')
except LookupError as caught:
    saved_cause = caught

print('S3.chain', 'next exception is intentional')
try:
    raise RuntimeError('sub-ambient') from saved_cause
except RuntimeError:
    try:
        sub_outer()
    finally:
        trace_view.release()
        trace_data.append(33)
        assert len(trace_kept) == 1 and trace_kept[0].string is trace_view
        print('S3.finally-clean', trace_data, trace_kept[0].span(), trace_kept[0].endpos)
