"""产品有限边界：真实hook拒绝不是CP成功；核原副作用及失败发布。"""
import sys
import importlib.util
import base
mode = sys.argv[1]
assert mode in ('init-subclass', 'set-name')
spec = importlib.util.find_spec('subject')
a = importlib.util.module_from_spec(spec)
a.mode = mode
sentinel = object()
a.Parser = sentinel
if mode == 'init-subclass':
    base.Base.__init_subclass__ = classmethod(base.hook)
try:
    spec.loader.exec_module(a)
except NotImplementedError as error:
    assert a.Parser is sentinel
    assert base.events == ['base', 'body', 'field', 'default']
    expected = 'class cell definitions with inherited __init_subclass__ hooks are not implemented'
    if mode == 'set-name':
        expected = 'class cell definitions with __set_name__ hooks are not implemented'
    assert str(error) == expected
    print(mode, base.events, 'NI old-binding')
else:
    raise AssertionError('unsupported hook was silently accepted')
