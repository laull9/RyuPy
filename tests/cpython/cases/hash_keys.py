"""CP1b：跨域等值str/bytes/大int/tuple必须满足dict/set哈希不变量。"""
import bridge_fixture

compiled_values = ['跨域哈希123456789', bytes([0, 1, 200, 255]),
                   int('123456789012345678901234567890'),
                   ('元组', int('98765432101234567890'))]
python_values = bridge_fixture.hash_values()
for index in range(4):
    compiled = compiled_values[index]
    python = python_values[index]
    assert compiled == python
    assert python == compiled
    assert hash(compiled) == hash(python)
    native_mapping = {compiled: 'first'}
    assert native_mapping[python] == 'first'
    native_mapping[python] = 'second'
    assert len(native_mapping) == 1
    reverse_mapping = {python: 'first'}
    assert reverse_mapping[compiled] == 'first'
    assert len({compiled, python}) == 1
    assert bridge_fixture.key_count(compiled, python) == 'second'
    assert bridge_fixture.key_count(python, compiled) == 'second'
print('CP1b 跨域值哈希、字典与集合不变量通过')
