# ryupy: pending
"""CP1b：handled异常状态双向同步、裸raise、嵌套重入与正常/错误路径恢复。"""
import bridge_fixture

outer = ValueError('编译外层handler')
inner = TypeError('编译内层handler')
try:
    raise outer
except ValueError:
    assert bridge_fixture.handled_exception() is outer
    try:
        bridge_fixture.reraise_handled_exception()
    except ValueError as reraised:
        assert reraised is outer
    else:
        raise AssertionError('解释域裸raise未使用编译handler')
    try:
        raise inner
    except TypeError:
        assert bridge_fixture.handled_exception() is inner
    assert bridge_fixture.handled_exception() is outer
assert bridge_fixture.handled_exception() is None

python_handler = TypeError('解释域handler')


def compiled_bare_raise():
    raise


try:
    bridge_fixture.call_in_python_handler(python_handler, compiled_bare_raise)
except TypeError as from_python_handler:
    assert from_python_handler is python_handler
else:
    raise AssertionError('编译回调裸raise未使用解释域handler')
assert bridge_fixture.handled_exception() is None


def compiled_nested_handler():
    assert bridge_fixture.handled_exception() is python_handler
    local = ValueError('重入内层handler')
    try:
        raise local
    except ValueError:
        assert bridge_fixture.handled_exception() is local
    assert bridge_fixture.handled_exception() is python_handler
    return '嵌套恢复'


assert bridge_fixture.call_in_python_handler(python_handler, compiled_nested_handler) == '嵌套恢复'
assert bridge_fixture.handled_exception() is None
callback_error = RuntimeError('回调错误路径')


def compiled_failing_callback():
    assert bridge_fixture.handled_exception() is python_handler
    raise callback_error


try:
    bridge_fixture.call_in_python_handler(python_handler, compiled_failing_callback)
except RuntimeError as from_callback:
    assert from_callback is callback_error
    assert from_callback.__context__ is python_handler
else:
    raise AssertionError('回调错误路径未传播')
assert bridge_fixture.handled_exception() is None
# 已Foreign的当前handler被CP原地改写正常返回，后续scope入口不能覆盖新链/args。
updated_cause = TypeError('handler内新cause')
updated_context = RuntimeError('handler内新context')
try:
    raise outer
except ValueError as active:
    assert bridge_fixture.handled_exception() is active
    assert bridge_fixture.mutate_exception_without_raise(
        active, updated_cause, updated_context, True, ('handler内新参数',)) is None
    assert bridge_fixture.handled_exception() is active
    details = bridge_fixture.read_handled_exception_details(active)
    assert details[0] is active
    assert details[1] is updated_cause
    assert details[2] == ('handler内新参数',)
    assert active.__cause__ is updated_cause
    assert active.args == ('handler内新参数',)
    try:
        raise
    except ValueError as bare_again:
        assert bare_again is active
        assert bare_again.__cause__ is updated_cause
        assert bare_again.args == ('handler内新参数',)
    assert bridge_fixture.handled_exception() is active
assert bridge_fixture.handled_exception() is None
print('CP1b handled状态、裸raise和嵌套恢复通过')
