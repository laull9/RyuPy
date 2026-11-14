# ryupy: pending
# 正文替换后，同一正文再次import与外部首次import都返回替代项。
import sys
import mc_state
import mc_subject as result
import mc_donor
old = mc_state.original
assert result is mc_donor and sys.modules['mc_subject'] is mc_donor
assert old is not result and old.before == 'original' and old.after == 'finished'
assert mc_state.inside is mc_donor and mc_state.exported == 'donor'
assert mc_state.events == ['subject:start', 'donor', 'subject:end']
import mc_subject as again
assert again is mc_donor and len(mc_state.events) == 3
print('initializer replacement wins import return', True)
