# 有限标量键以外、view集合、weakref/pickle/deepcopy；全部保CP正预期而非NI golden。
from collections import OrderedDict as OD
import copy
import pickle
import weakref

for key in (('tuple', 1), b'bytes'):
    d = OD([(key, 'value')])
    print('extended_key', type(key).__name__, d[key], next(iter(d)) is key, list(d.items()))
    print('extended_key_move', d.move_to_end(key, False), list(d.items()))
d = OD(a=1, b=2)
e = OD(b=2, c=3)
print('keys_set', sorted(d.keys() & e.keys()), sorted(d.keys() | e.keys()), sorted(d.keys() - e.keys()), sorted(d.keys() ^ e.keys()))
print('items_set', sorted(d.items() & e.items()), sorted(d.items() | e.items()))
print('cross_set_equality', d.keys() == {'a', 'b'}, d.items() == {('a', 1), ('b', 2)})
proxy = d.keys().mapping
print('view_mapping', type(proxy).__name__, list(proxy.items()))
d.move_to_end('a')
print('view_mapping_live', list(proxy.items()))
ref = weakref.ref(d)
print('weakref', ref() is d)
d.tag = []
shallow = copy.copy(d)
print('copy_module', type(shallow) is OD, shallow is not d, list(shallow.items()), shallow.tag is d.tag)
cyclic = OD()
cyclic['self'] = cyclic
cyclic['value'] = []
clone = copy.deepcopy(cyclic)
print('deepcopy_cycle', type(clone) is OD, clone is not cyclic, clone['self'] is clone, clone['value'] is not cyclic['value'])
restored = pickle.loads(pickle.dumps(d, protocol=4))
print('pickle_roundtrip', type(restored) is OD, restored == d, list(restored.items()), restored.tag == d.tag, restored.tag is not d.tag)
print('DONE pending_extended_protocols')
