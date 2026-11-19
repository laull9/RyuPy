# ryupy: pending
# CPython的dict原生keys/items视图是live，不伪写成受控快照view的期望。
import sys
import mc_subject
keys = sys.modules.keys()
items = sys.modules.items()
before_keys = list(keys)
before_items = list(items)
sys.modules['mc_live_alias'] = mc_subject
assert 'mc_live_alias' in keys
assert ('mc_live_alias', mc_subject) in items
assert 'mc_live_alias' not in before_keys
assert ('mc_live_alias', mc_subject) not in before_items
del sys.modules['mc_live_alias']
assert 'mc_live_alias' not in keys
assert ('mc_live_alias', mc_subject) not in items
print('dict live views and explicit list snapshots differ', True)
