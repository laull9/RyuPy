"""CP1b：异常实例、继承匹配、cause/context 与 Python traceback 原样往返。"""
import bridge_fixture

marker = ValueError('原始实例')


def compiled_raises():
    raise marker


try:
    bridge_fixture.chain(compiled_raises)
except RuntimeError as outer:
    assert outer.__cause__ is marker
    assert outer.__context__ is marker
    assert outer.__suppress_context__
    assert marker.__traceback__ is not None
    assert outer.__traceback__ is not None
    try:
        raise outer.__cause__
    except Exception as original:
        assert original is marker
        assert isinstance(original, ValueError)
        assert str(original) == '原始实例'
else:
    raise AssertionError('异常链未跨桥传播')
try:
    bridge_fixture.raise_user_error()
except ValueError as user_error:
    assert isinstance(user_error, bridge_fixture.UserError)
    assert str(user_error) == '用户子类'
else:
    raise AssertionError('用户异常子类继承匹配失败')
try:
    bridge_fixture.raise_misleading_error()
except ValueError:
    raise AssertionError('错误地按__name__匹配同名异常')
except TypeError as misleading:
    assert type(misleading) is bridge_fixture.MisleadingValueError
    assert str(misleading) == '同名不同继承'
else:
    raise AssertionError('同名异常身份负例未抛错')
print('CP1b 异常身份、继承与链通过')
