"""CP1b：对象穿过 Python callable 往返，保持可变别名与模块身份。"""
import bridge_fixture
import bridge_fixture as again
import json

assert bridge_fixture is again
value = json.loads('{"items": [1, 2]}')
assert bridge_fixture.echo(value) is value
items = value['items']
assert bridge_fixture.echo(items) is items
bridge_fixture.append(items, 3)
assert value['items'] == [1, 2, 3]
bridge_fixture.assign(value, 'alias', items)
assert value['alias'] is items
text = ''.join(['编译', '对象', str(987654321)])
data = bytes([0, 1, 2, 200, 255])
large = int('123456789012345678901234567890')
for immutable in (text, data, large):
    assert bridge_fixture.echo(immutable) is immutable
    assert bridge_fixture.same(immutable, immutable)
python_text = bridge_fixture.make_text()
assert bridge_fixture.echo(python_text) is python_text
assert bridge_fixture.same(python_text, python_text)
print('CP1b 身份与可变别名通过')
