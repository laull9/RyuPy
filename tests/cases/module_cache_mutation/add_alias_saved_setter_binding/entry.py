# 完整基本合同：保存的setter及容器别名仍只接受两个位置参数，拒绝kwargs不得写缓存。
import sys
import mc_state
import mc_subject
subject = mc_subject
importer = subject.read.__builtins__['__import__']
cache = sys.modules
cache_alias = cache
setter = cache.__setitem__
saved = [setter]
containers = [cache_alias]
original_read = subject.read
original_dict = subject.__dict__
assert setter('mc_setter_saved_alias', subject) is None
assert cache['mc_setter_saved_alias'] is subject
assert importer('mc_setter_saved_alias') is subject
assert saved[0]('mc_setter_list_alias', subject) is None
assert importer('mc_setter_list_alias') is subject
assert containers[0].__setitem__('mc_setter_container_alias', subject) is None
assert importer('mc_setter_container_alias') is subject
print('saved setter and container aliases positional returns', True)
first = 'mc_setter_all_keywords'
assert first not in cache
try:
    setter(key=first, value=subject)
except TypeError as exc:
    assert type(exc) is TypeError
else:
    raise AssertionError('all-keyword setter must fail')
assert first not in cache and sys.modules is cache_alias
assert saved[0](first, subject) is None
assert cache[first] is subject and importer(first) is subject
print('all keywords reject without cache or marker residue', True)
second = 'mc_setter_value_keyword'
assert second not in cache
try:
    saved[0](second, value=subject)
except TypeError as exc:
    assert type(exc) is TypeError
else:
    raise AssertionError('keyword value setter must fail')
assert second not in cache
assert setter(second, subject) is None
assert cache[second] is subject and importer(second) is subject
print('keyword value rejects without cache or marker residue', True)
third = 'mc_setter_unknown_keyword'
assert third not in cache
try:
    containers[0].__setitem__(third, subject, unknown=1)
except TypeError as exc:
    assert type(exc) is TypeError
else:
    raise AssertionError('unexpected keyword setter must fail')
assert third not in cache
assert saved[0](third, subject) is None
assert cache[third] is subject and importer(third) is subject
print('unknown keyword rejects without cache or marker residue', True)
assert sys.modules['mc_subject'] is subject and subject.__dict__ is original_dict
assert subject.read is original_read and subject.read() == 17
assert mc_state.runs == 1 and sys.modules is cache
print('setter bindings preserve source/cache/default import identity', True)
