# ryupy: pending
# 常规入口从显式实例模块读取真实执行结果，不依赖RyuPy专有Python API。
import subject
assert subject.before == 3 and subject.after == 91
assert subject.current_type == 'changed type'
assert subject.annotated == 'changed type'
assert subject.observed is subject.__dict__['never_declared']
assert subject.observed == ['injected']
assert subject.total == 15 and subject.item == 8
assert subject.__dict__['range'] is subject.range
print('dynamic names and builtin shadowing')
