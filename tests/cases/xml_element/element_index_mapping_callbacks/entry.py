# ryupy: pending
# 默认C允许字典派生/用户key/自定义index，保实际副作用与错误；本切仍独立pending。
import xml.etree.ElementTree as ET

events = []
class Attributes(dict):
    def keys(self):
        events.append('keys')
        return super().keys()
    def __getitem__(self, key):
        events.append(('getitem', key))
        return super().__getitem__(key)

source = Attributes({'a': 'value'})
root = ET.Element('root', source)
assert root.attrib is not source and type(root.attrib) is dict
assert root.attrib == {'a': 'value'}
print('dict-subclass-copy', events, root.attrib)
events.clear()
root.attrib = source
assert root.attrib is source
root.set('b', 'from-node')
assert source['b'] == 'from-node'
print('dict-subclass-alias', root.attrib is source, events)

key_events = []
class Key:
    def __hash__(self):
        key_events.append('hash')
        return 5
    def __eq__(self, other):
        key_events.append('eq')
        return self is other

key = Key()
keyed_source = {key: 'old'}
key_events.clear()
keyed = ET.Element('keyed', keyed_source)
assert keyed.keys()[0] is key
print('key-copy-events', key_events)
assert keyed.get(key) == 'old'
keyed.set(key, 'new')
assert keyed.get(key) == 'new' and keyed_source[key] == 'old'
print('key-consumption-events', key_events)

child = ET.SubElement(root, 'child')
late = ET.Element('late')
index_events = []
class Index:
    def __index__(self):
        index_events.append(('index', len(root)))
        root.append(late)
        return -1

assert root[Index()] is late
assert index_events == [('index', 1)]
assert root[0] is child and root[1] is late
class BadIndex:
    def __index__(self):
        index_events.append(('bad', len(root)))
        return 'not-an-int'

try:
    root[BadIndex()]
except TypeError as error:
    print('bad-index', type(error).__name__, str(error))
else:
    raise AssertionError('invalid index return accepted')
assert len(root) == 2
print('index-events', index_events, [node.tag for node in root])
