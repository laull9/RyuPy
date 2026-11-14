# ryupy: pending
# None与未命中严格不同；两个导入语法均阻断且不执行源。
import sys
import mc_state
sys.modules['mc_subject'] = None
assert 'mc_subject' in sys.modules and sys.modules.get('mc_subject') is None
try:
    import mc_subject
except ModuleNotFoundError as exc:
    assert str(exc) == 'import of mc_subject halted; None in sys.modules'
else:
    raise AssertionError('plain import must be blocked')
try:
    from mc_subject import value
except ModuleNotFoundError as exc:
    assert str(exc) == 'import of mc_subject halted; None in sys.modules'
else:
    raise AssertionError('from import must be blocked')
assert mc_state.runs == 0
del sys.modules['mc_subject']
import mc_subject as loaded
assert loaded.value == 17 and mc_state.runs == 1
sys.modules['mc_subject'] = None
assert loaded.value == 17
sys.modules['mc_subject'] = loaded
import mc_subject as restored
assert restored is loaded and mc_state.runs == 1
print('None blocks import and deletion restores loading', True)
