# 独立窄例：仅目标名字与builtins相位，真实正文没有异常处理。
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
assert 'source_marker' not in clone.__dict__ and 'value' not in clone.__dict__
assert clone.__builtins__ == 7
del clone.__name__
try:
    spec.loader.exec_module(clone)
except AttributeError:
    pass
else:
    raise AssertionError('缺少名字应失败')
assert 'source_marker' not in clone.__dict__ and 'value' not in clone.__dict__
assert clone.__builtins__ == 7
clone.__name__ = None
del clone.__builtins__
assert spec.loader.exec_module(clone) is None
assert clone.__name__ is None and clone.read.__module__ is None
assert clone.source_marker == 'A' and clone.read() == 17
assert clone.__spec__ is spec and clone.__loader__ is spec.loader
spec.name = 'renamed'
renamed = util.module_from_spec(spec)
assert renamed.__name__ == 'renamed' and renamed.__spec__ is spec
try:
    spec.loader.exec_module(renamed)
except ImportError as error:
    assert str(error) == 'loader for subject cannot handle renamed'
    assert error.name == 'renamed'
else:
    raise AssertionError('spec改名不能覆盖loader来源名字')
assert '__builtins__' not in renamed.__dict__ and 'source_marker' not in renamed.__dict__
assert sys.modules['subject'] is subject and subject.read() == 17
assert subject.source_marker == 'A'
print('窄例：不匹配、缺失、None及spec改名的执行相位正确')
