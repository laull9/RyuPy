# 最窄真实新别名：只新增一次，不替换、不删除，不碰原登记名。
import sys
import importlib.util as util
import mc_state
import mc_subject
original = mc_subject
importer = original.read.__builtins__['__import__']
alias = 'mc_alias_existing'
assert alias not in sys.modules
keys_before = list(sys.modules.keys())
items_before = list(sys.modules.items())
original_dict = original.__dict__
original_function = original.read
original_spec = original.__spec__
sys.modules[alias] = original
assert sys.modules[alias] is original and sys.modules['mc_subject'] is original
assert importer(alias) is original and importer(alias, None, None, (), 0) is original
assert importer(alias, None, None, [], 0) is original
assert mc_state.runs == 1
assert original.__dict__ is original_dict and original.read is original_function
assert original.read.__globals__ is original_dict
assert original.__name__ == 'mc_subject' and original.__spec__ is original_spec
assert util.find_spec(alias) is original_spec and original_spec.name == 'mc_subject'
current = importer(alias)
current.write(31)
current.items.append('shared')
assert original.read() == 31 and original.items == ['shared']
original.value = 47
assert importer(alias).read() == 47 and mc_state.runs == 1
assert alias not in keys_before and alias in list(sys.modules.keys())
assert not any(key == alias for key, value in items_before)
assert any(key == alias and value is original for key, value in list(sys.modules.items()))
import mc_subject as source_again
assert source_again is original and mc_state.runs == 1
print('add existing alias identity/spec/owner/mutations/snapshots', True)
