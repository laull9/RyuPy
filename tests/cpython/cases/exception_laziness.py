"""CP1b：捕获和桥接异常时不提前执行用户__str__/属性读取。"""
import bridge_fixture

try:
    bridge_fixture.raise_lazy()
except ValueError as error:
    assert len(bridge_fixture.exception_events) == 0
    assert error is bridge_fixture.last_error
    assert type(error) is bridge_fixture.LazyError
    assert str(error) == '延迟异常展示'
    assert bridge_fixture.exception_events == ['str']
else:
    raise AssertionError('真实异常未传播')
print('CP1b 异常延迟展示与身份通过')
