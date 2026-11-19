"""CP与产品共同正向/普通异常合同；hook CP输出不作为产品正向黄金。"""
import sys
import importlib.util
import base
mode = sys.argv[1]
spec = importlib.util.find_spec('subject')
a = importlib.util.module_from_spec(spec)
a.mode = mode
sentinel = object()
a.Parser = sentinel
if mode == 'init-subclass':
    base.Base.__init_subclass__ = classmethod(base.hook)
try:
    spec.loader.exec_module(a)
except ValueError as error:
    assert a.Parser is sentinel
    print(mode, str(error), base.events, 'old-binding')
else:
    if mode == 'copies':
        b = importlib.util.module_from_spec(spec)
        b.mode = mode
        spec.loader.exec_module(b)
        old = a.Parser().read
        a.value = 20
        b.value = 30
        assert old() == (20, 7, 5)
        assert b.Parser().read() == (30, 7, 5)
        assert a.Parser.read.__globals__ is a.__dict__
        assert b.Parser.read.__globals__ is b.__dict__
        spec.loader.exec_module(a)
        a.value = 40
        assert old() == (40, 7, 5)
        assert a.Parser.read is not old.__func__
        assert b.Parser().read() == (30, 7, 5)
        print('copies old-method reexec owner OK')
    elif mode == 'builtins':
        old = a.Parser().builtin
        a.__dict__['__builtins__'] = {'len': lambda x: 99, '__name__': 'subject'}
        assert old() == 0
        assert a.Parser().read() == (10, 7, 5)
        print('definition-builtins default OK')
    else:
        assert a.Parser is not sentinel
        print(mode, base.events, 'published')
