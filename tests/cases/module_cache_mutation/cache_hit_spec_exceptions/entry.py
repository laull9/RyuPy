# ryupy: pending
# CP正期待：普通属性操作传播异常，而默认导入的初始化检查会处理这些spec协议异常。
import sys
import mc_state
import mc_subject
subject = mc_subject
importer = subject.read.__builtins__['__import__']
original_spec = subject.__spec__
alias = 'mc_exception_spec_alias'
sys.modules[alias] = subject
events = []
class GetterErrorSpec:
    @property
    def _initializing(self):
        events.append('getter:RuntimeError')
        raise RuntimeError('spec getter failed')
getter_error = GetterErrorSpec()
subject.__spec__ = getter_error
try:
    getter_error._initializing
except RuntimeError as exc:
    assert str(exc) == 'spec getter failed'
    events.append('direct caught:RuntimeError')
else:
    raise AssertionError('ordinary getter must propagate')
assert importer(alias) is subject
assert sys.modules[alias] is subject and subject.__spec__ is getter_error
events.append('import returned cached module')
print('getter error trace', events)
class BrokenTruth:
    def __bool__(self):
        events.append('truth:ValueError')
        raise ValueError('spec truth failed')
broken_truth = BrokenTruth()
class TruthErrorSpec:
    @property
    def _initializing(self):
        events.append('getter:truth object')
        return broken_truth
truth_error = TruthErrorSpec()
subject.__spec__ = truth_error
try:
    bool(truth_error._initializing)
except ValueError as exc:
    assert str(exc) == 'spec truth failed'
    events.append('direct caught:ValueError')
else:
    raise AssertionError('ordinary truth conversion must propagate')
assert importer(alias) is subject
assert sys.modules[alias] is subject
events.append('import returned cached module')
print('truth error trace', events)
class MissingSpec:
    @property
    def _initializing(self):
        events.append('getter:AttributeError')
        raise AttributeError('initializing absent')
subject.__spec__ = MissingSpec()
assert importer(alias) is subject
events.append('missing getter returned cached module')
print('missing attribute trace', events)
class BrokenAttributeTruth:
    def __bool__(self):
        events.append('truth:AttributeError')
        raise AttributeError('truth is not missing lookup')
attribute_truth = BrokenAttributeTruth()
class AttributeTruthSpec:
    @property
    def _initializing(self):
        events.append('getter:attribute truth object')
        return attribute_truth
attribute_spec = AttributeTruthSpec()
subject.__spec__ = attribute_spec
try:
    bool(attribute_spec._initializing)
except AttributeError as exc:
    assert str(exc) == 'truth is not missing lookup'
    events.append('direct caught:AttributeError')
else:
    raise AssertionError('ordinary truth AttributeError must propagate')
assert importer(alias) is subject
events.append('import returned cached module')
assert sys.modules[alias] is subject and sys.modules['mc_subject'] is subject
assert mc_state.runs == 1 and subject.read() == 17
print('truth AttributeError trace', events)
subject.__spec__ = original_spec
assert importer(alias) is subject
print('spec observation/errors do not remove cache or reinitialize', True)
