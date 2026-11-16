# 两父共享、重复孩子、插入位置和读切片保持节点身份，没有隐式搬家或克隆。
import xml.etree.ElementTree as ET

root = ET.Element('root')
other = ET.Element('other')
shared = ET.SubElement(root, 'shared')
assert other.append(shared) is None
assert root.append(shared) is None
middle = ET.Element('middle')
assert root.insert(1, middle) is None
first = ET.Element('first')
last = ET.Element('last')
root.insert(-100, first)
root.insert(100, last)
assert [node.tag for node in root] == ['first', 'shared', 'middle', 'shared', 'last']
assert root[1] is shared and root[-2] is shared and other[0] is shared
shared.text = 'visible through every reference'
assert root[1].text == other[0].text
assert {shared: root}[other[0]] is root
snapshot = root[:]
assert type(snapshot) is list and snapshot[1] is shared
snapshot.pop()
assert len(root) == 5 and len(snapshot) == 4
assert [node.tag for node in root[1:4:2]] == ['shared', 'shared']
assert root[::-1][1] is shared
assert root[100:200] == []
root.insert(-1, ET.Element('before-last'))
assert root[-2].tag == 'before-last'
print('children', len(root), [node.tag for node in root])
print('aliases', root[1] is root[3], other[0] is root[1], other[0].text)
print('slice', [node.tag for node in snapshot], root[::-1][2] is shared)
