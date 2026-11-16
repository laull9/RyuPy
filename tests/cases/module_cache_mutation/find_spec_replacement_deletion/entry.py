# ryupy: pending
# find_spec返回当前缓存对象的spec；删除后只发现源，不执行正文。
import sys
import importlib.util as util
import mc_state
import mc_subject
import mc_donor
original = mc_subject
source_spec = original.__spec__
donor_spec = mc_donor.__spec__
sys.modules['mc_subject'] = mc_donor
assert util.find_spec('mc_subject') is donor_spec
assert donor_spec.name == 'mc_donor'
del sys.modules['mc_subject']
discovered = util.find_spec('mc_subject')
assert discovered.name == 'mc_subject' and discovered is not source_spec
assert 'mc_subject' not in sys.modules and mc_state.runs == 1
sys.modules['mc_subject'] = None
assert util.find_spec('mc_subject') is None and mc_state.runs == 1
del sys.modules['mc_subject']
import mc_subject as fresh
assert fresh is not original and fresh.serial == 2
assert util.find_spec('mc_subject') is fresh.__spec__
print('find_spec follows cache then discovers without execution', True)
