"""CP1b：from None抑制、同一异常实例的跨域cause/context/suppress刷新。"""
import bridge_fixture

context = ValueError('原始context')
outer = RuntimeError('同一外层实例')


def compiled_suppresses():
    try:
        raise context
    except ValueError:
        raise outer from None


state = bridge_fixture.capture_exception_state(compiled_suppresses)
assert state[0] is outer
assert state[1] is True
assert state[2] is context
assert state[3] is None
replacement_cause = ValueError('替换cause')
replacement_context = TypeError('替换context')
try:
    bridge_fixture.rewrite_exception_state(outer, replacement_cause, replacement_context, False)
except RuntimeError as caught:
    assert caught is outer
    assert caught.__cause__ is replacement_cause
    assert caught.__context__ is replacement_context
    assert caught.__suppress_context__ is False
else:
    raise AssertionError('第一次重抛未传播')
try:
    bridge_fixture.rewrite_exception_state(outer, None, context, True)
except RuntimeError as caught_again:
    assert caught_again is outer
    assert caught_again.__cause__ is None
    assert caught_again.__context__ is context
    assert caught_again.__suppress_context__ is True
else:
    raise AssertionError('第二次重抛未传播')


# 已Foreign的同一实例由编译侧改写，再由callback返回解释域。
compiled_cause = ValueError('编译侧cause')
compiled_context = TypeError('编译侧context')
outer.__cause__ = compiled_cause
outer.__context__ = compiled_context
outer.__suppress_context__ = False
outer.args = ('编译侧新参数',)


def compiled_reraises():
    raise outer


compiled_state = bridge_fixture.capture_exception_state(compiled_reraises)
assert compiled_state[0] is outer
assert compiled_state[1] is False
assert compiled_state[2] is compiled_context
assert compiled_state[3] is compiled_cause
assert compiled_state[4] == ('编译侧新参数',)
# CP原地改写正常返回None，编译raise必须保最新链与args。
return_cause = ValueError('CP正常返回cause')
return_context = TypeError('CP正常返回context')
assert bridge_fixture.mutate_exception_without_raise(
    outer, return_cause, return_context, True, ('CP正常返回新参数',)) is None
try:
    raise outer
except RuntimeError as after_return:
    assert after_return is outer
    assert after_return.__cause__ is return_cause
    assert after_return.__context__ is return_context
    assert after_return.__suppress_context__ is True
    assert after_return.args == ('CP正常返回新参数',)
else:
    raise AssertionError('CP正常返回后compiled重抛失败')
returned_state = bridge_fixture.capture_exception_state(compiled_reraises)
assert returned_state[0] is outer
assert returned_state[1] is True
assert returned_state[2] is return_context
assert returned_state[3] is return_cause
assert returned_state[4] == ('CP正常返回新参数',)
print('CP1b 异常抑制与可变链刷新通过')
