# ryupy: pending
# 整数/连续与扩展切片写删的后续真实合同；不以NI作为CP等价期望。
import xml.etree.ElementTree as ET

root = ET.Element('root')
a = ET.Element('a')
b = ET.Element('b')
c = ET.Element('c')
d = ET.Element('d')
e = ET.Element('e')
x = ET.Element('x')
for child in [a, b, c, d, e]:
    root.append(child)
root[1] = x
root[-1] = a
snapshot = root[:]
assert list(root) == [a, x, c, d, a]
root[1:3] = [b, c, x]
assert list(root) == [a, b, c, x, d, a]
root[::2] = [x, x, x]
assert list(root) == [x, b, x, x, x, a]
print('assigned', [node.tag for node in root], root[0] is root[2])
del root[1]
del root[-1]
del root[1::2]
assert list(root) == [x, x]
assert snapshot == [a, x, c, d, a]
print('deleted', [node.tag for node in root], [node.tag for node in snapshot])

before = root[:]
def wrong_size():
    root[::2] = [a, b]
def wrong_child():
    root[:] = [a, 'bad', b]
def wrong_index():
    root[99] = a

for label, action, expected in [('slice-size', wrong_size, ValueError),
                                 ('slice-child', wrong_child, TypeError),
                                 ('index-range', wrong_index, IndexError)]:
    try:
        action()
    except expected as error:
        print(label, type(error).__name__, str(error))
    else:
        raise AssertionError(label + ' did not fail')
    assert list(root) == before

root[0:1] = []
assert list(root) == [x]
root[1:1] = [a, x]
assert list(root) == [x, a, x]
del root[:]
assert len(root) == 0 and len(snapshot) == 5
print('final', len(root), snapshot[0] is snapshot[-1])
