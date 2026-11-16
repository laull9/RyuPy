# ryupy: pending
# 默认加速clear替换attrib而非清空旧别名；remove仅删首个相同节点引用。
import xml.etree.ElementTree as ET

root = ET.Element('root', {'retain': 'old'})
shared = ET.SubElement(root, 'shared')
middle = ET.SubElement(root, 'middle')
root.append(shared)
other = ET.Element('other')
other.append(shared)
assert root.remove(shared) is None
assert [node.tag for node in root] == ['middle', 'shared']
assert root[1] is shared and other[0] is shared
missing = ET.Element('shared')
try:
    root.remove(missing)
except ValueError as error:
    print('missing', type(error).__name__, str(error))
else:
    raise AssertionError('structurally equal nodes are not interchangeable')
assert len(root) == 2
old_attributes = root.attrib
root.text = 'text'
root.tail = 'tail'
shared.text = 'shared content'
assert root.clear() is None
assert root.tag == 'root' and root.text is None and root.tail is None
assert len(root) == 0 and root.attrib == {}
assert root.attrib is not old_attributes and old_attributes == {'retain': 'old'}
old_attributes['after'] = 'outside'
assert root.attrib == {}
assert other[0] is shared and shared.text == 'shared content'
print('clear', root.tag, root.text, root.tail, len(root), root.attrib)
print('old-attrib', old_attributes, root.attrib is old_attributes)
print('other-parent', len(other), other[0].text)
