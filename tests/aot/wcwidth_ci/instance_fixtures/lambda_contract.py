# Legacy 验收入口观察真实 loader 的双实例、函数归属与固定参数 binder。
import importlib.util as util
import lambda_subject
spec = util.find_spec('lambda_subject')
left = util.module_from_spec(spec)
right = util.module_from_spec(spec)
left_builtins = lambda_subject.make.__builtins__.copy()
right_builtins = lambda_subject.make.__builtins__.copy()
left.__builtins__ = left_builtins
right.__builtins__ = right_builtins
spec.loader.exec_module(left)
spec.loader.exec_module(right)
old = left.make()
other = right.make()
assert old is not other
assert old.__builtins__ is left_builtins
assert other.__builtins__ is right_builtins
assert old.__globals__ is left.__dict__
assert other.__globals__ is right.__dict__
assert old('alpha') == b'alpha'
assert other('beta') == b'beta'
assert left.zero()() == 7
assert left.positional()('pos') == b'pos'
assert left.keyword()(value='kw') == b'kw'
for function, args, kwargs in [
    (left.positional(), (), {'value': 'bad'}),
    (left.keyword(), ('bad',), {}),
    (left.keyword(), (), {}),
    (left.make(), (), {}),
    (left.zero(), (1,), {}),
]:
    try:
        function(*args, **kwargs)
    except TypeError:
        pass
    else:
        raise AssertionError('binder 应拒绝错误调用')
events = []
def called(x, y, *, z):
    events.append((x, y, z))
    return z
f = left.many()
assert f(called, 1, 2, z=3) == 3
assert events == [(1, 2, 3)]
try:
    f(called, 1, 2)
except TypeError:
    pass
else:
    raise AssertionError('required kwonly 应拒绝缺参')
def fail(x, y, *, z):
    events.append('raise')
    raise ValueError('lambda-call')
try:
    f(fail, 1, 2, z=3)
except ValueError as error:
    assert str(error) == 'lambda-call'
else:
    raise AssertionError('参数调用异常必须传播')
assert events == [(1, 2, 3), 'raise']
old.__module__ = 'not-an-owner'
old_globals = left.__dict__
replacement = left_builtins.copy()
left.__builtins__ = replacement
spec.loader.exec_module(left)
new = left.make()
assert left.__dict__ is old_globals
assert old.__builtins__ is left_builtins
assert new.__builtins__ is replacement
assert other.__builtins__ is right_builtins
assert old is not new
assert old.__globals__ is left.__dict__
assert new.__globals__ is left.__dict__
assert old('old') == b'old' and new('new') == b'new'
assert other.__globals__ is right.__dict__
print('lambda owners binder effects ok')
