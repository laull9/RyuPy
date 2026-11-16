# ryupy: pending
# TreeBuilder是独立真实状态机：start/end返回原节点，data按text/tail相位拼接。
import xml.etree.ElementTree as ET

builder = ET.TreeBuilder()
attributes = {'id': '1'}
root = builder.start('root', attributes)
assert type(root) is ET.Element and root.attrib is attributes
assert builder.data('alpha') is None
builder.data('&')
child = builder.start('child', {})
assert root[0] is child
builder.data('inner')
builder.data('<')
assert builder.end('child') is child
builder.data('tail')
builder.data('>')
second = builder.start('empty', {})
assert builder.end('empty') is second
builder.data('last')
assert builder.end('root') is root
assert builder.close() is root
assert builder.close() is root
assert root.text == 'alpha&' and child.text == 'inner<'
assert child.tail == 'tail>' and second.tail == 'last' and second.text is None
assert root.tail is None and len(root) == 2
attributes['outside'] = 'shared after start'
assert root.attrib == {'id': '1', 'outside': 'shared after start'}
print('tree', root.tag, root.attrib, root.text)
print('children', [(node.tag, node.text, node.tail) for node in root])
print('identity', builder.close() is root, root[0] is child, root[1] is second)

# 默认加速器不套用纯Python的close/end断言：空close为None，未闭close仍返回根。
empty = ET.TreeBuilder()
assert empty.close() is None
try:
    empty.end('missing')
except IndexError as error:
    print('empty-end', type(error).__name__, str(error))
else:
    raise AssertionError('empty end accepted')

unclosed = ET.TreeBuilder()
opened = unclosed.start('open', {})
assert unclosed.close() is opened
assert unclosed.end('different-tag') is opened
assert unclosed.close() is opened
try:
    unclosed.end('again')
except IndexError as error:
    print('extra-end', type(error).__name__, str(error))
else:
    raise AssertionError('extra end accepted')
try:
    unclosed.start('second-root', {})
except ET.ParseError as error:
    print('second-root', type(error).__name__, str(error))
else:
    raise AssertionError('second root accepted')
assert unclosed.close() is opened
print('close-state', empty.close(), unclosed.close() is opened)
