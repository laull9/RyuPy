# 当前目标名字检查先于builtins，None名字仍执行来源代码。
import sys
import importlib.util as util
import subject
spec = subject.__spec__
clone = util.module_from_spec(spec)
clone.__name__ = 'wrong'
clone.__builtins__ = 7
try:
    spec.loader.exec_module(clone)
except ImportError as error:
    assert str(error) == 'loader for subject cannot handle wrong'
    assert error.name == 'wrong'
else:
    raise AssertionError('名字不匹配应失败')
assert 'runs' not in clone.__dict__ and clone.__builtins__ == 7
del clone.__name__
try:
    spec.loader.exec_module(clone)
except AttributeError:
    pass
else:
    raise AssertionError('缺少名字应失败')
assert 'runs' not in clone.__dict__ and clone.__builtins__ == 7
clone.__name__ = None
del clone.__builtins__
spec.loader.exec_module(clone)
assert clone.__name__ is None and clone.read.__module__ is None
assert clone.read() == 41 and clone.__spec__ is spec
spec.name = 'renamed'
renamed = util.module_from_spec(spec)
assert renamed.__name__ == 'renamed'
try:
    spec.loader.exec_module(renamed)
except ImportError as error:
    assert error.name == 'renamed'
else:
    raise AssertionError('spec改名不能覆盖loader来源名字')
assert '__builtins__' not in renamed.__dict__
assert sys.modules['subject'] is subject and subject.runs == 1
print('不匹配、缺失、None及spec改名的执行顺序正确')
