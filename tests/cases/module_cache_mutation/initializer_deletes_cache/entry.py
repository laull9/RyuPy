# ryupy: pending
# 正文成功但删自身条目时，导入收尾取缓存应抛KeyError。
import sys
import mc_state
for attempt in (1, 2):
    try:
        import mc_subject
    except KeyError as exc:
        assert exc.args == ('mc_subject',)
    else:
        raise AssertionError('deleted successful import must fail')
    assert 'mc_subject' not in sys.modules
    assert mc_state.runs == attempt and mc_state.saved[-1].finished
assert mc_state.saved[0] is not mc_state.saved[1]
assert sys.modules['mc_state'] is mc_state
print('initializer deletion raises and retries freshly', True)
