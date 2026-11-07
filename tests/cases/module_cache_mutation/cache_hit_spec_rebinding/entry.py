# ryupy: pending
# CP正期待：新别名建立后，cache-hit仍观察当前spec的初始化协议及其副作用。
import sys
import mc_state
import mc_subject
subject = mc_subject
importer = subject.read.__builtins__['__import__']
original_spec = subject.__spec__
alias = 'mc_observed_spec_alias'
sys.modules[alias] = subject
events = []
class Flag:
    def __init__(self, label, answer):
        self.label = label
        self.answer = answer
    def __bool__(self):
        events.append('truth:' + self.label)
        return self.answer
false_flag = Flag('false', False)
true_flag = Flag('true', True)
class NextSpec:
    @property
    def _initializing(self):
        events.append('next:_initializing')
        return true_flag
next_spec = NextSpec()
class FirstSpec:
    @property
    def _initializing(self):
        events.append('first:_initializing')
        subject.__spec__ = next_spec
        return false_flag
first_spec = FirstSpec()
subject.__spec__ = first_spec
assert importer(alias) is subject
assert subject.__spec__ is next_spec
assert 'first:_initializing' in events and 'truth:false' in events
print('first cache-hit trace', events)
assert importer(alias, None, None, [], 0) is subject
assert 'next:_initializing' in events and 'truth:true' in events
print('second cache-hit trace', events)
assert importer(alias, None, None, (), 0) is subject
print('third cache-hit trace', events)
assert sys.modules[alias] is subject and sys.modules['mc_subject'] is subject
assert subject.read() == 17 and mc_state.runs == 1
subject.__spec__ = original_spec
before_restore_import = list(events)
assert importer(alias) is subject and events == before_restore_import
assert mc_state.runs == 1
print('cache hit preserves live spec observations and module identity', True)
