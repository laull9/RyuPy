# ryupy: pending
# 默认加速extend先耗尽生成器，再校验并附加有效前缀；迭代异常时不附加已yield值。
import xml.etree.ElementTree as ET

root = ET.Element('root')
first = ET.Element('first')
last = ET.Element('last')
events = []

def source():
    events.append(('yield-first', len(root)))
    yield first
    events.append(('yield-invalid', len(root)))
    yield 'bad'
    events.append(('yield-last', len(root)))
    yield last
    events.append(('finished', len(root)))

try:
    root.extend(source())
except TypeError as error:
    print('type-error', type(error).__name__, str(error))
else:
    raise AssertionError('invalid child was accepted')
assert events == [('yield-first', 0), ('yield-invalid', 0), ('yield-last', 0), ('finished', 0)]
assert len(root) == 1 and root[0] is first
print('eager-events', events)
print('valid-prefix', [node.tag for node in root])

raised = ET.Element('raised')
kept = ET.SubElement(raised, 'kept')
raise_events = []

def broken():
    raise_events.append(('yield', len(raised)))
    yield first
    raise_events.append(('raise', len(raised)))
    raise RuntimeError('source stopped')

try:
    raised.extend(broken())
except RuntimeError as error:
    print('source-error', type(error).__name__, str(error))
else:
    raise AssertionError('source error was hidden')
assert len(raised) == 1 and raised[0] is kept
assert raise_events == [('yield', 1), ('raise', 1)]
assert root.extend((last, first)) is None
assert list(root) == [first, last, first]
print('source-events', raise_events, [node.tag for node in raised])
print('success', [node.tag for node in root], root[0] is root[-1])
