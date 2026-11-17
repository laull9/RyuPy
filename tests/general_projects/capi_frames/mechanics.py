# 产品native header的独立机械门；Python只作为oracle与测试driver，C API观察由两个独立C扩展执行。
import builtins
import gc
import native_carrier_harness as carrier
import native_scope_probe as scope
import native_callback_probe as callback

a = {'__builtins__': builtins.__dict__, '__file__': '真实模块 A.py', 'marker': object()}
b = {'__builtins__': builtins.__dict__, '__file__': '真实模块 B.py', 'marker': object()}
for namespace in (a, b):
    original = set(namespace)
    facts = carrier.run(namespace, namespace['__file__'], scope.facts)
    assert facts[0] is namespace and facts[1] is namespace
    assert facts[2] == namespace['__file__']
    assert set(namespace) == original
    del facts
    gc.collect()
    assert carrier.stats()[0] == 0
facts = carrier.call_existing(scope.facts)
assert facts[0] is globals() and facts[2] == __file__
del facts
gc.collect()
assert carrier.run(a, a['__file__'], callback.remember)
assert carrier.stats()[0] == 1
try:
    callback.replay()
except RuntimeError as error:
    assert str(error) == 'compiled execution context expired'
else:
    raise AssertionError('escaped thunk must expire')
callback.forget()
gc.collect()
assert carrier.stats()[0] == 0
try:
    carrier.run(a, a['__file__'], scope.fail)
except KeyError as error:
    assert error.args == ('native-scope-probe',)
else:
    raise AssertionError('native exception lost')
assert carrier.stats()[0] == 0
for invalid_globals, invalid_filename in (([], 'bad.py'), (a, 123)):
    try:
        carrier.run(invalid_globals, invalid_filename, scope.facts)
    except TypeError:
        pass
    else:
        raise AssertionError('invalid context accepted')
    assert carrier.stats()[0] == 0
print('native-carrier-mechanics-ok', carrier.stats())
