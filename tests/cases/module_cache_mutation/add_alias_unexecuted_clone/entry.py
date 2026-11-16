# State0公开对象已在缓存时直接返回，不能因为尚未exec而偷跑正文。
import sys
import importlib.util as util
import mc_state
import mc_subject
original = mc_subject
importer = original.read.__builtins__['__import__']
spec = util.find_spec('mc_subject')
clone = util.module_from_spec(spec)
clone_dict = clone.__dict__
assert mc_state.runs == 1 and 'value' not in clone_dict
assert '__builtins__' not in clone_dict
sys.modules['mc_alias_unexecuted'] = clone
assert importer('mc_alias_unexecuted') is clone and mc_state.runs == 1
assert clone.__dict__ is clone_dict and 'value' not in clone_dict
assert '__builtins__' not in clone_dict and not hasattr(clone, 'read')
assert clone.__spec__ is spec and util.find_spec('mc_alias_unexecuted') is spec
assert clone.__name__ == 'mc_subject' and sys.modules['mc_subject'] is original
clone.external = 'before execution'
assert spec.loader.exec_module(clone) is None
assert mc_state.runs == 2 and clone.__dict__ is clone_dict
assert importer('mc_alias_unexecuted') is clone and clone.external == 'before execution'
assert clone.read.__globals__ is clone_dict and clone.read() == 10
clone.write(77)
assert importer('mc_alias_unexecuted').read() == 77 and original.read() == 10
assert mc_state.runs == 2
print('add unexecuted clone alias never implicitly executes', True)
