# 真实源loader的公开path和name修改不能被隐藏来源忽略。
import sys
import importlib.util as util
import subject
loader = subject.__loader__
original_path = loader.path
replacement_spec = util.find_spec('replacement')
loader.path = replacement_spec.origin
clone = util.module_from_spec(subject.__spec__)
loader.exec_module(clone)
assert clone.replacement == 73 and 'runs' not in clone.__dict__
loader.name = 'renamed'
clone.__name__ = 'renamed'
loader.exec_module(clone)
assert clone.replacement == 73
assert sys.modules['subject'] is subject and subject.runs == 1
loader.path = original_path
loader.name = 'subject'
print('修改loader来源路径和名字后遵守公开来源')
