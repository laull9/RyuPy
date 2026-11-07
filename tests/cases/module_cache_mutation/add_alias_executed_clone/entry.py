# 已执行clone的新别名不能更名、再执行或接管原模块的函数环境。
import sys
import importlib.util as util
import mc_state
import mc_subject
original = mc_subject
importer = original.read.__builtins__['__import__']
spec = util.find_spec('mc_subject')
clone = util.module_from_spec(spec)
spec.loader.exec_module(clone)
assert mc_state.runs == 2 and clone is not original
clone.write(31)
old_function = clone.read
clone_dict = clone.__dict__
sys.modules['mc_alias_executed'] = clone
cached = importer('mc_alias_executed')
assert cached is clone and cached.read is old_function
assert cached.__dict__ is clone_dict and cached.read.__globals__ is clone_dict
assert clone.__name__ == 'mc_subject' and clone.__package__ == ''
assert clone.__spec__ is spec and util.find_spec('mc_alias_executed') is spec
assert clone.read() == 31 and original.read() == 10 and mc_state.runs == 2
clone.items.append('clone')
assert importer('mc_alias_executed').items == ['clone'] and original.items == []
clone.value = 59
assert old_function() == 59 and importer('mc_alias_executed').read() == 59
import mc_subject as source_again
assert source_again is original and sys.modules['mc_subject'] is original
assert original.read() == 10 and mc_state.runs == 2
print('add executed clone alias preserves separate owner and state', True)
