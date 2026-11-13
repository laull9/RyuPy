# Path退化分支保持身份与文件，公开生成器上下文属性及绑定方法走真实协议。
from importlib.resources import as_file
from pathlib import Path


def error(call):
    try:
        call()
    except Exception as exc:
        print(type(exc).__name__, str(exc))
    else:
        raise AssertionError('应当失败')


p = Path('resource.txt')
p.write_text('原始资源', encoding='utf-8')
for path in [p, Path('.'), Path('missing-parent/missing.txt')]:
    cm = as_file(path)
    assert cm.args == (path,) and cm.kwds == {}
    assert cm.func.__name__ == '_'
    assert isinstance(cm.__doc__, str)
    generator = cm.gen
    enter, leave = cm.__enter__, cm.__exit__
    assert enter.__self__ is cm and leave.__self__ is cm
    assert enter == cm.__enter__ and leave == cm.__exit__ and enter != leave
    assert hash(enter) == hash(cm.__enter__)
    assert {enter: 'bound'}[cm.__enter__] == 'bound'
    assert enter.__func__ is cm.__enter__.__func__
    assert enter.__func__.__name__ == '__enter__'
    assert enter.__qualname__ == '_GeneratorContextManager.__enter__'
    assert enter.__module__ == 'contextlib'
    assert enter() is path and cm.gen is generator
    assert not hasattr(cm, 'args') and not hasattr(cm, 'kwds') and not hasattr(cm, 'func')
    error(enter)
    assert leave(typ=None, value=None, traceback=None) is False
    assert leave(None, None, None) is False
    error(enter)
assert p.read_text(encoding='utf-8') == '原始资源'
assert not Path('missing-parent').exists()

for exc in [ValueError('body'), StopIteration('body'), RuntimeError('body'), KeyboardInterrupt('body')]:
    try:
        with as_file(p) as actual:
            assert actual is p
            raise exc
    except BaseException as caught:
        assert caught is exc
    else:
        raise AssertionError('不能吞掉异常')
assert p.read_text(encoding='utf-8') == '原始资源'

for call in [lambda: as_file(), lambda: as_file(path=p), lambda: as_file(p, p),
             lambda: as_file(p, path=p), lambda: as_file(p, other=1),
             lambda: as_file(42), lambda: as_file('resource.txt'), lambda: as_file(None),
             lambda: as_file(42, 43), lambda: as_file(42, path=43), lambda: as_file(42, other=1),
             lambda: as_file(p, p, other=1), lambda: as_file(42, 43, other=1)]:
    error(call)
cm = as_file(p)
for call in [lambda: cm.__enter__(1), lambda: cm.__enter__(x=1),
             lambda: cm.__exit__(), lambda: cm.__exit__(None, None, None, None),
             lambda: cm.__exit__(unknown=1), lambda: cm.__enter__.__func__(None),
             lambda: cm.__exit__.__func__(None, None, None, None),
             lambda: cm._recreate_cm.__func__(None), lambda: cm.__enter__(1, x=1),
             lambda: cm.__exit__(None, None, None, None, unknown=1),
             lambda: cm.__exit__(None, None, None, None, typ=None)]:
    error(call)
assert cm.__enter__() is p
assert cm.__exit__(None, None, None) is False

# 正常生成器协议可从gen观察，耗尽之后enter按contextlib报错。
cm = as_file(p)
assert next(cm.gen) is p
error(cm.__enter__)
cm = as_file(p)
error(lambda: cm.__exit__(None, None, None))
error(cm.__enter__)
# _recreate_cm与ContextDecorator每次创建新的上下文，复用调用不耗尽模板。
cm = as_file(p)
assert cm._recreate_cm.__self__ is cm and cm.__call__.__self__ is cm
clone = cm._recreate_cm()
with clone as actual:
    assert actual is p
@cm
def decorated(value):
    return value
assert decorated(7) == 7 and decorated(8) == 8
assert cm.args == (p,)
with cm as actual:
    assert actual is p
error(cm._recreate_cm)
# 公开字典不是快照；修改args影响重建，gen重绑影响进入的真实产出。
cm = as_file(p)
assert cm.__dict__['args'] is cm.args
cm.args = (Path('replacement'),)
with cm._recreate_cm() as actual:
    assert actual == Path('replacement')
replacement = as_file(Path('other'))
cm.gen = replacement.gen
with cm as actual:
    assert actual == Path('other')
cm = as_file(p)
del cm.kwds
error(cm.__enter__)
assert not hasattr(cm, 'args') and hasattr(cm, 'func')
p.unlink()
print('resources-as-file-path: ok')
