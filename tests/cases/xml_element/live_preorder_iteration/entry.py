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

# 物化入口与 children 游标一致；快照只复制外层，子树不被递归摊平。
def collect(factory, node):
    return factory(node)


def capture(*nodes):
    return nodes


empty = ET.Element('empty')
assert list(empty) == [] and tuple(empty) == ()
empty_cursor = iter(empty)
assert list(empty_cursor) == []
a = ET.SubElement(empty, 'a')
assert list(empty_cursor) == [] and tuple(empty) == (a,)
b = ET.Element('b')
parent = ET.Element('parent')
parent.append(a)
parent.append(b)
parent.append(a)
other = ET.Element('other')
other.append(a)
grandchild = ET.SubElement(a, 'grandchild')
list_alias = list
snapshot = list_alias(parent)
frozen = collect(tuple, parent)
for items in [snapshot, frozen, list(iter(parent)), [node for node in parent]]:
    assert len(items) == 3
    assert items[0] is a and items[1] is b and items[2] is a
assert snapshot is not list(parent) and frozen is not tuple(parent)
assert list(other)[0] is a and list(list(parent)[0])[0] is grandchild
assert tuple(tuple(parent)[0])[0] is grandchild
assert capture(*parent) == (a, b, a)
replacement = [None]
replacement[:] = parent
assert replacement[0] is a and replacement[1] is b and replacement[2] is a
late = ET.SubElement(parent, 'late')
assert len(snapshot) == 3 and len(frozen) == 3 and list(parent)[3] is late
snapshot.append(grandchild)
assert len(parent) == 4 and parent[3] is late

# 插入在游标之前会重见原节点；之后插入和首次耗尽前追加可见。
active = ET.Element('active')
active.append(a)
active.append(b)
first = iter(active)
second = iter(active)
assert next(first) is a
active.insert(0, grandchild)
assert next(first) is a
active.insert(2, late)
assert next(first) is late
active.append(a)
assert tuple(first) == (b, a)
active.append(b)
assert list(first) == []
assert list(second) == [grandchild, a, late, b, a, b]
assert list(active) == [grandchild, a, late, b, a, b]

# 消费后的比较错误不能被物化适配吞掉；其它 native 对象不获得迭代能力。
try:
    sorted(parent)
except TypeError:
    pass
else:
    assert False
try:
    list(ET.QName('plain'))
except TypeError:
    pass
else:
    assert False
