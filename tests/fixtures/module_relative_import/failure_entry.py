import importlib.util as util
import failpkg
import pkg.plain
if False:
    import failpkg.subject
    import pkg.failing
spec = util.find_spec('failpkg.subject')
target = util.module_from_spec(spec)
try:
    spec.loader.exec_module(target)
except RuntimeError as error:
    assert str(error) == 'bad init'
else:
    raise AssertionError('missing failure')
assert failpkg.events == ['a', 'bad']
assert 'a' not in target.__dict__ and 'bad' not in target.__dict__
spec.loader.exec_module(target)
assert failpkg.events == ['a', 'bad', 'bad']
assert target.read() is target.a
plain_spec = util.find_spec('pkg.failing')
plain = util.module_from_spec(plain_spec)
try:
    plain_spec.loader.exec_module(plain)
except ImportError:
    pass
else:
    raise AssertionError('missing attribute')
assert plain.value is pkg.plain.value
assert 'absent' not in plain.__dict__
print('fromlist failure and retry phases preserved')
