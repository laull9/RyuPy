"""固定CP元数据删除oracle：验证真实builtin回退；不是RyuPy能力通过或NI预期。"""
import builtins
import sys
import types

if sys.implementation.name != 'cpython' or sys.version_info[:3] != (3, 12, 13):
    raise RuntimeError('oracle需要CPython3.12.13')

for name in ('__spec__', '__loader__', '__cached__', '__path__'):
    module = types.ModuleType('metadata_oracle_subject')
    marker = []
    module.__dict__[name] = marker
    exec('def read():\n    return ' + name + '\n', module.__dict__)
    assert module.read() is marker
    del module.__dict__[name]
    try:
        getattr(module, name)
    except AttributeError:
        pass
    else:
        raise AssertionError('外部属性不应回退builtins')
    if name in ('__spec__', '__loader__'):
        assert module.read() is getattr(builtins, name)
        print(name, 'builtin identity')
    else:
        try:
            module.read()
        except NameError:
            print(name, 'NameError')
        else:
            raise AssertionError('不存在的builtin不应伪造值')
    module.__dict__[name] = marker
    assert module.read() is marker
print('metadata oracle ok')
