"""C1 类体/元类失败保留旧绑定、同一异常与命名空间确定释放。"""
from bridge_fixture.class_construction import Base, FailInitBase, clear_traceback, disposed, fail_body, reset

reset()
Aborted = 'old-body'
try:
    class Aborted(Base):
        fail_body()
except ValueError as error:
    assert str(error) == 'body marker'
    clear_traceback(error)
else:
    raise AssertionError('class body error was lost')
assert Aborted == 'old-body'
assert disposed() == 1

Failed = 'old-meta'
try:
    class Failed(FailInitBase):
        @classmethod
        def on_construct(cls):
            return cls
except ValueError as error:
    assert str(error) == 'meta init marker'
    clear_traceback(error)
else:
    raise AssertionError('metaclass error was lost')
assert Failed == 'old-meta'
assert disposed() == 2
print('native class failure cleanup passed')
