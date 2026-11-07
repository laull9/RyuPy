# ryupy: pending
# 首次导入前写入、已有槽替换、旧局部保留与正文不执行。
import sys
import mc_state
import mc_donor
assert 'mc_target' not in sys.modules
sys.modules['mc_target'] = mc_donor
import mc_target
assert mc_target is mc_donor and mc_state.events == ['donor']
assert sys.modules.get('mc_target') is mc_donor
saved = mc_target
import mc_other
sys.modules['mc_target'] = mc_other
import mc_target as replaced
from mc_target import value
assert replaced is mc_other and value == 'other'
assert saved is mc_donor and saved.value == 'donor'
assert mc_state.events == ['donor', 'other']
sys.modules['mc_target'] = saved
import mc_target as restored
assert restored is saved and mc_state.events == ['donor', 'other']
print('assignment/replacement/restore identity', True)
