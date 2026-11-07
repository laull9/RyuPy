# ryupy: pending
# QName延迟消费非str/text变异及用户比较；构造不能提前字符串化或提前拒绝。
import xml.etree.ElementTree as ET

events = []
class Value:
    def __str__(self):
        events.append('str')
        return 'value'
    def __hash__(self):
        events.append('hash')
        return 17
    def __eq__(self, other):
        events.append(('eq', type(other).__name__))
        return other == 'match' if isinstance(other, str) else False
    def __lt__(self, other):
        events.append(('lt', type(other).__name__))
        return True

value = Value()
name = ET.QName(value)
assert name.text is value and events == []
assert hash(name) == 17
assert name == 'match'
assert name != 'other'
assert name < 99
assert name != ET.QName(Value())
try:
    str(name)
except TypeError as error:
    print('non-str-consumption', type(error).__name__, str(error))
else:
    raise AssertionError('QName __str__ returned a non-str')
assert 'str' not in events
print('delegated', events)

formatted = ET.QName(value, 'local')
assert formatted.text == '{value}local' and events[-1] == 'str'
plain = ET.QName(7)
assert plain.text == 7 and hash(plain) == 7 and plain == 7
print('construction', str(formatted), plain.text, hash(plain))

replacement = []
name.text = replacement
assert name.text is replacement
for label, action in [('str-list', lambda: str(name)), ('hash-list', lambda: hash(name))]:
    try:
        action()
    except TypeError as error:
        print(label, type(error).__name__, str(error))
    else:
        raise AssertionError(label + ' did not fail')
assert name == []
name.text = 'restored'
assert str(name) == 'restored' and hash(name) == hash('restored')
print('restored', str(name))
