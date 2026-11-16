# ryupy: pending
"""F1唯一应用：callback原buffer晚Join、错误/lease顺序与完整末链；模板/escape留待F2。"""
import re


def expect_error(label, kind, message, call):
    try:
        call()
    except Exception as error:
        assert type(error) is kind and str(error) == message
        print(label, type(error).__name__, str(error))
        return error
    raise AssertionError('missing error: ' + label)


def carrier(kind, content):
    owner = bytes(content) if kind in ('readonly_B', 'readonly_c') else bytearray(content)
    if kind == 'array':
        return owner, owner
    if kind == 'view_slice':
        owner = bytearray(b'!' + content + b'?')
    root = memoryview(owner)
    if kind in ('view_c', 'readonly_c'):
        value = root.cast('c')
        root.release()
    elif kind == 'view_slice':
        value = root[1:-1]
        root.release()
    else:
        value = root
    return value, owner


def clean(value, owner):
    if isinstance(value, memoryview):
        value.release()
    if isinstance(owner, bytearray):
        owner.append(33)


# 原V六结果载体；跨公开route/subject的组合首次应用CP守门。
p = re.compile(b'a')
saved_subn = p.subn
for kind in ('array', 'view_B', 'view_c', 'view_slice', 'readonly_B', 'readonly_c'):
    value, owner = carrier(kind, b'X')
    outputs = []
    for source_kind in ('exact', 'mutable', 'view'):
        data = bytearray(b'zaaT' if source_kind == 'mutable' else b'aa')
        source = b'a' if source_kind == 'exact' else data if source_kind == 'mutable' else memoryview(data)
        seen = []

        def replacement(match):
            assert match.string is source and match.re is p
            seen.append((match.span(), match.group()))
            return value

        if source_kind == 'exact':
            result = p.sub(replacement, source)
            assert result == b'X' and seen == [((0, 1), b'a')]
        elif source_kind == 'mutable':
            result, count = re.subn(p, replacement, source)
            assert result == b'zXXT' and count == 2
            assert seen == [((1, 2), b'a'), ((2, 3), b'a')]
        else:
            result, count = saved_subn(replacement, source)
            assert result == b'XX' and count == 2
            assert seen == [((0, 1), b'a'), ((1, 2), b'a')]
            source.release()
        assert type(result) is bytes and result is not value
        outputs.append(result)
        data.append(33)
        print('F1.carrier-' + kind + '-' + source_kind, result, seen)
    if isinstance(owner, bytearray):
        owner[1 if kind == 'view_slice' else 0] = ord('Y')
    clean(value, owner)
    assert outputs == [b'X', b'zXXT', b'XX']
    print('F1.carrier-clean-' + kind, outputs)

# 旧F增长结果引用，返回时不能Acquire或快照；两次append必须均可成功。
shared = bytearray(b'X')
events = []


def growing(match):
    shared.append(ord('Y'))
    events.append(bytes(shared))
    return shared


assert p.sub(growing, bytearray(b'aa')) == b'XYYXYY'
assert events == [b'XY', b'XYY']
shared.append(33)
print('F1.growing-reference', events, shared)

# 旧52同长修改返回同一对象；最终两个片段均读C，不是首次B的快照。
shared = bytearray(b'X')
events = []


def changing(match):
    events.append(bytes(shared))
    shared[0] = 65 + len(events)
    return shared


assert p.sub(changing, b'aa') == b'CC'
assert events == [b'X', b'B'] and bytes(shared) == b'C'
shared.append(33)
print('F1.same-size-reference', events, shared)

# 原J的source view自身作结果；array组合首次CP守门。source的scanner lease仍只计1。
for kind in ('array', 'view'):
    data = bytearray(b'aa')
    source = data if kind == 'array' else memoryview(data)
    events = []

    def alias_result(match):
        events.append(match.span())
        assert match.string is source
        data[match.start()] = ord('x') if len(events) == 1 else ord('y')
        if kind == 'view':
            try:
                source.release()
            except BufferError as error:
                assert str(error) == 'memoryview has 1 exported buffer'
            else:
                raise AssertionError('source scanner lease lost')
        return source

    assert p.subn(alias_result, source) == (b'xyxy', 2)
    assert events == [(0, 1), (1, 2)]
    if kind == 'view': source.release()
    data.append(33)
    print('F1.source-alias-' + kind, events, data)

# 原F的released结果错误index0与原J prefix/先前坏类型错误顺序。
for name, prefix, first_bad in [('zero', b'', False), ('prefix', b'p', False), ('earlier-str', b'', True)]:
    result_owner = bytearray(b'X')
    result_view = memoryview(result_owner)
    events = []

    def released_result(match):
        events.append(match.span())
        if len(events) == 1:
            return 'bad' if first_bad else result_view
        result_view.release()
        return result_view if first_bad else b'Y'

    message = ('sequence item 0: expected a bytes-like object, str found' if first_bad else
               'sequence item ' + str(len(prefix)) + ': expected a bytes-like object, memoryview found')
    expect_error('F1.released-' + name, TypeError, message, lambda: p.sub(released_result, prefix + b'aa'))
    start = len(prefix)
    assert events == [(start, start + 1), (start + 1, start + 2)]
    result_owner.append(33)
    print('F1.released-clean-' + name, events, result_owner)

# 新组合：已取得good结果lease之后碰到坏view，失败必须撤good、source全部临时lease。
good = bytearray(b'X')
bad_owner = bytearray(b'Y')
bad = memoryview(bad_owner)
source_owner = bytearray(b'aa')
source = memoryview(source_owner)
events = []


def partial_failure(match):
    events.append(match.span())
    if len(events) == 1:
        return good
    bad.release()
    return bad


expect_error('F1.partial-acquire', TypeError, 'sequence item 1: expected a bytes-like object, memoryview found',
             lambda: saved_subn(partial_failure, source))
assert events == [(0, 1), (1, 2)]
source.release()
source_owner.append(33)
good.append(33)
bad_owner.append(33)
print('F1.partial-clean', good, bad_owner, source_owner)

# 同exporter的不同view也须各自撤lease；失败不得留下先前good view的export。
owner = bytearray(b'X')
good_view = memoryview(owner)
bad_view = good_view[:]
events = []


def sibling_failure(match):
    events.append(match.span())
    if len(events) == 1:
        return good_view
    bad_view.release()
    return bad_view


expect_error('F1.sibling-acquire', TypeError, 'sequence item 1: expected a bytes-like object, memoryview found',
             lambda: p.sub(sibling_failure, b'aa'))
assert events == [(0, 1), (1, 2)]
good_view.release()
owner.append(33)
print('F1.sibling-clean', events, owner)

# 总长为0也不能跳过released结果的验证；empty合法结果仅验值/类型/非原buffer身份。
owner = bytearray(b'')
released_empty = memoryview(owner)
released_empty.release()
expect_error('F1.released-empty', TypeError, 'sequence item 0: expected a bytes-like object, memoryview found',
             lambda: p.sub(lambda match: released_empty, b'a'))
owner.append(33)
for kind in ('array', 'view_c', 'readonly_B'):
    value, owner = carrier(kind, b'')
    result, count = p.subn(lambda match: value, b'a')
    assert result == b'' and type(result) is bytes and result is not value and count == 1
    clean(value, owner)
    print('F1.empty-' + kind, result, type(result).__name__, count)

# no-match/负count不调用callback，不读取其潜在released返回值；count1不多调一次。
seen = []


def never_called(match):
    seen.append(match.span())
    return released_empty


unchanged = bytes([122, 122])
assert p.sub(never_called, unchanged) is unchanged
assert p.sub(never_called, b'aa', -1) == b'aa' and seen == []
returned = bytearray(b'X')
assert p.subn(lambda match: returned, b'aa', 1) == (b'Xa', 1)
returned.append(33)
print('F1.count-no-callback', unchanged, seen, returned)

# 后callback新异常仍优先于尚未Join的mutable或普通坏结果，原identity不重包。
for first_kind in ('mutable', 'str'):
    source_owner = bytearray(b'aa')
    source = memoryview(source_owner)
    returned = bytearray(b'X')
    marker = LookupError('later-' + first_kind)
    seen = []

    def later_error(match):
        seen.append(match.span())
        if len(seen) == 1:
            return returned if first_kind == 'mutable' else 'bad'
        raise marker

    try:
        p.sub(later_error, source)
    except LookupError as caught:
        assert caught is marker
    else:
        raise AssertionError('later callback did not win')
    assert seen == [(0, 1), (1, 2)]
    source.release()
    source_owner.append(33)
    returned.append(33)
    print('F1.later-error-' + first_kind, seen, source_owner, returned)

# exact单片身份与旧mixed-str模板的两层错误保原路径，不扩输入模板域。
exact_result = bytes([88, 89, 90])
assert p.sub(lambda match: exact_result, b'a') is exact_result
assert p.sub(exact_result, b'a') is exact_result
assert p.sub(lambda match: None, b'aa') == b''
print('F1.exact-controls', exact_result, p.sub(lambda match: None, b'aa'))
mixed = re.compile(br'(a)?b')
assert mixed.sub(r'\1', unchanged) is unchanged
expect_error('F1.mixed-present', TypeError, 'sequence item 1: expected str instance, bytes found',
             lambda: mixed.sub(r'\1', b'ab'))
expect_error('F1.mixed-missing', TypeError, 'sequence item 0: expected a bytes-like object, str found',
             lambda: mixed.sub(r'\1', b'b'))
assert re.sub('(a)', r'<\1>', 'a a') == '<a> <a>'
print('F1.text-control', re.sub('(a)', r'<\1>', 'a a'))
print('F1.protocol-complete')

# 最后全新链来自最终Join，而非callback抛错；所有callback已返回，finally只验证清理。
trace_pattern = re.compile(b'a')
trace_source_owner = bytearray(b'paa')
trace_source = memoryview(trace_source_owner)
trace_result_owner = bytearray(b'X')
trace_result = memoryview(trace_result_owner)
trace_seen = []


def trace_callback(match):
    trace_seen.append(match.span())
    if len(trace_seen) == 2:
        trace_result.release()
        return b'Y'
    return trace_result


def join_middle():
    return trace_pattern.sub(trace_callback, trace_source)


def join_outer():
    return join_middle()


try:
    raise LookupError('join-cause')
except LookupError as caught:
    saved_cause = caught

try:
    raise RuntimeError('join-ambient') from saved_cause
except RuntimeError:
    print('F1.chain')
    try:
        join_outer()
    finally:
        assert trace_seen == [(1, 2), (2, 3)]
        trace_source.release()
        trace_source_owner.append(33)
        trace_result_owner.append(33)
        print('F1.finally-clean', trace_seen, trace_source_owner, trace_result_owner)
