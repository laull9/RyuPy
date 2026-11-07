# 构造浅复制、直接attrib赋值共享；keys/items快照与任意属性值不能字符串化。
import xml.etree.ElementTree as ET

class AtomicString(str):
    pass

marker = []
source = {'first': marker, 'replace': 'old'}
root = ET.Element('root', source, replace='new', final='last')
assert root.attrib is not source
assert root.attrib['first'] is marker
assert list(root.attrib) == ['first', 'replace', 'final']
assert source['replace'] == 'old' and root.get('replace') == 'new'
source['outside'] = 'not copied'
assert 'outside' not in root.attrib
marker.append('shared')
assert root.get('first') is marker
saved_keys = root.keys()
saved_items = root.items()
assert type(saved_keys) is list and type(saved_items) is list
root.set('later', 'added')
assert saved_keys == ['first', 'replace', 'final']
assert len(saved_items) == 3 and saved_items[0][1] is marker
replacement = {'active': marker}
root.attrib = replacement
assert root.attrib is replacement
root.set('from-node', 'visible')
replacement['from-alias'] = 'visible'
assert root.get('from-alias') == 'visible'
fallback = object()
assert root.get('missing', fallback) is fallback and root.get('missing') is None
text = AtomicString('atomic <&>')
tail = object()
tag = object()
root.text = text
root.tail = tail
root.tag = tag
assert root.text is text and isinstance(root.text, AtomicString)
assert root.tail is tail and root.tag is tag
assert ET.Element(tag).tag is tag
child_source = {'a': marker}
child = ET.SubElement(root, 'child', child_source, b='two')
assert child.attrib is not child_source and child.get('a') is marker
assert root[0] is child
print('constructor', list(source), saved_keys, saved_items)
print('attrib-alias', root.attrib is replacement, list(root.attrib.items()))
print('raw-fields', root.text is text, root.tail is tail, root.tag is tag)
print('child', child.tag, list(child.attrib), child.attrib is child_source, child.get('a') is marker)
