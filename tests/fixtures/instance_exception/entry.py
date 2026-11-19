from importlib import util
import lookups
import cleanup

original = ValueError('original')
assert lookups.quiet() == 'quiet'
assert lookups.builtin_target(original) == 'value'
lookups.ValueError = TypeError
assert lookups.builtin_target(original) == 'other'
del lookups.ValueError
assert lookups.builtin_target(original) == 'value'
assert lookups.local_target(ValueError, original) == 'match'
assert lookups.local_target((), original) == 'next'
assert lookups.close(ValueError)(original) == 'free'
assert lookups.cell_target(ValueError, original)
assert lookups.Handler().run(ValueError, original) == 'method'
for target in [(ValueError, 7), ((ValueError,),), ValueError('target')]:
    try:
        lookups.local_target(target, original)
    except TypeError as error:
        assert error.__context__ is original
    else:
        raise AssertionError('invalid target accepted')
for function, expected in [(lookups.missing_target, NameError), (lookups.unbound_target, UnboundLocalError)]:
    try:
        function(original)
    except expected as error:
        assert error.__context__ is original
    else:
        raise AssertionError('lookup did not fail')

assert cleanup.fallback is ValueError
assert 'ValueError' not in cleanup.__dict__
assert cleanup.global_clear(ValueError, original) == 'cleared'
assert 'slot' not in cleanup.__dict__
assert cleanup.untouched == 'keep'
for function, expected in [(cleanup.local_clear, UnboundLocalError), (cleanup.free_clear, UnboundLocalError)]:
    try:
        function(original)
    except expected:
        pass
    else:
        raise AssertionError('as was retained')
reader = cleanup.cell_clear(original)
try:
    reader()
except NameError:
    pass
else:
    raise AssertionError('cell was retained')
try:
    cleanup.bare(original)
except ValueError as error:
    assert error is original
assert cleanup.control(original) == [0, 1]
assert 'slot' not in cleanup.__dict__
assert cleanup.final_override(original) == 'outer'
assert 'slot' not in cleanup.__dict__

spec = util.find_spec('captured')
first = util.module_from_spec(spec)
old = {'Target': ValueError, 'BaseException': BaseException}
first.__dict__['__builtins__'] = old
spec.loader.exec_module(first)
saved = first.run
assert saved(original) == 'caught'
new = {'Target': TypeError, 'BaseException': BaseException}
first.__dict__['__builtins__'] = new
spec.loader.exec_module(first)
assert saved(original) == 'caught'
assert first.run(original) == 'other'
old['Target'] = TypeError
assert saved(original) == 'other'
second = util.module_from_spec(spec)
second.__dict__['__builtins__'] = {'Target': ValueError, 'BaseException': BaseException}
spec.loader.exec_module(second)
assert second.run(original) == 'caught'
assert first.run(original) == 'other'
print('Name/bare LEGB, owner builtins and implicit cleanup preserved')
assert lookups.ambient(original, TypeError('other'))
try:
    lookups.handler_error(original)
except TypeError as error:
    assert error.__context__ is original
else:
    raise AssertionError('handler did not raise')
try:
    lookups.with_wrap(original)
except ValueError as error:
    assert error is original
iterator = lookups.with_generator()
assert next(iterator) == 1
try:
    next(iterator)
except StopIteration:
    pass
else:
    raise AssertionError('generator did not end')
assert lookups.events == ['enter', 'exit', 'enter', 'exit']
