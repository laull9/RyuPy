# 产品native header的独立机械门；Python只作为oracle与测试driver，C API观察由两个独立C扩展执行。
import builtins
import gc
import weakref
import types
import time
import functools
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
    code=facts[3]
    assert carrier.origin(code)==2
    clone=code.replace()
    assert carrier.origin(clone)==0
    observed=next(w for w in weakref.getweakrefs(code) if getattr(w.__callback__,'__name__','')=='_ryupy_code_origin_dead')
    escaped_callback=observed.__callback__
    escaped_callback(observed)
    assert carrier.origin(code)==2
    del code,clone,observed
    del facts
    gc.collect()
    assert carrier.stats()[0] == 0
    assert carrier.origin_stats()==(0,0)
    escaped_callback(None)
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

# 诊断计时与正式性能分开；大常量表不影响frame身份查找复杂度。
def small():
    return carrier.kind()
fat=types.FunctionType(small.__code__.replace(co_consts=small.__code__.co_consts+tuple(range(20000))),globals())
for target in (small,fat):
    assert target()==1
    start=time.perf_counter()
    carrier.run(a,a['__file__'],functools.partial(carrier.repeat,target,50000))
    print('diagnostic-frame-context',len(target.__code__.co_consts),time.perf_counter()-start)
assert carrier.origin_stats()==(0,0)

# 定义点builtins归属独立于D的当前键；真实frame中字段必须保持原捕获对象。
captured = dict(builtins.__dict__)
for current in (dict(builtins.__dict__), None, 42):
    if current is None:
        a.pop('__builtins__', None)
    else:
        a['__builtins__'] = current
    original = dict(a)
    facts = carrier.run(a, a['__file__'], scope.facts, captured)
    assert facts[4].f_builtins is captured
    assert facts[0] is a and facts[1] is a and a == original
    del facts
    gc.collect()
    assert carrier.stats()[0] == 0 and carrier.origin_stats() == (0, 0)
print('native-captured-builtins-ok')

# 用户audit拒绝固定code构造时，已分配capsule/target/builtins仍走同一真实回滚。
import sys
reject_carrier_compile = True
def reject_compile(event, args):
    if event == 'compile' and reject_carrier_compile and args[0] == b'(0).__call__()\n':
        raise LookupError('carrier-construction-audit')
sys.addaudithook(reject_compile)
try:
    carrier.run(b, b['__file__'], scope.facts)
except LookupError as error:
    assert error.args == ('carrier-construction-audit',)
else:
    raise AssertionError('构造拒绝错误被覆盖')
reject_carrier_compile = False
assert carrier.stats()[0] == 0 and carrier.origin_stats() == (0, 0)
print('native-construction-rollback-ok')

# 原code先死亡、复制code仍活：原登记注销，复制常量中的capsule只能保留失效上下文。
facts = carrier.run(b, b['__file__'], scope.facts)
code = facts[3]
clone = code.replace()
observed = next(w for w in weakref.getweakrefs(code) if getattr(w.__callback__, '__name__', '') == '_ryupy_code_origin_dead')
escaped_callback = observed.__callback__
del facts, code
gc.collect()
assert observed() is None
assert carrier.origin_stats() == (1, 0) and carrier.origin(clone) == 0
escaped_callback(observed)
try:
    types.FunctionType(clone, b)()
except RuntimeError as error:
    assert str(error) == 'compiled execution context expired'
else:
    raise AssertionError('复制code逃逸后重获执行权限')
del clone, observed
gc.collect()
assert carrier.stats()[0] == 0 and carrier.origin_stats() == (0, 0)
escaped_callback(None)
print('native-copy-after-original-death-ok')
