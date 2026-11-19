# ryupy: pending
# 首次正文设置None会让首次导入成功返回None，后续导入才阻断。
import sys
import mc_state
import mc_subject as first
assert first is None and sys.modules['mc_subject'] is None
assert mc_state.runs == 1 and mc_state.original.finished
try:
    import mc_subject as second
except ModuleNotFoundError as exc:
    assert str(exc) == 'import of mc_subject halted; None in sys.modules'
else:
    raise AssertionError('second import must fail')
assert mc_state.runs == 1
print('initializer None first return and next blocking', True)
