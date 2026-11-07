# children按当前索引读取且耗尽后不复活；iter是真实前序游标而非物化列表。
import xml.etree.ElementTree as ET

root = ET.Element('root')
child = ET.SubElement(root, 'branch')
leaf = ET.SubElement(child, 'leaf')
again = ET.SubElement(root, 'branch')
assert [node.tag for node in root.iter()] == ['root', 'branch', 'leaf', 'branch']
assert [node.tag for node in root.iter(None)] == ['root', 'branch', 'leaf', 'branch']
assert [node.tag for node in root.iter('*')] == ['root', 'branch', 'leaf', 'branch']
assert list(root.iter('branch')) == [child, again]
assert list(root.iter('missing')) == []
iterator = root.iter()
assert iter(iterator) is iterator and next(iterator) is root
assert next(iterator) is child and next(iterator) is leaf
assert next(iterator) is again
assert next(iterator, None) is None and next(iterator, None) is None
live = iter(root)
assert iter(live) is live and next(live) is child
added = ET.SubElement(root, 'added')
assert next(live) is again and next(live) is added
assert next(live, None) is None
ET.SubElement(root, 'too-late')
assert next(live, None) is None
fresh = root.iter('leaf')
leaf.tag = 'renamed'
assert list(fresh) == []
print('preorder', [node.tag for node in root.iter()])
print('filtered', [node.tag for node in root.iter('branch')])
print('sticky', next(live, 'stopped'), next(iterator, 'stopped'))
