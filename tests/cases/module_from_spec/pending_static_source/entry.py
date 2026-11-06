# 未选择实例编译的真实源仍以CP公开协议作为后续目标。
import sys
import importlib.util as util
spec = util.find_spec('static_source')
clone = util.module_from_spec(spec)
spec.loader.exec_module(clone)
assert clone.read() == 41 and clone.read.__module__ == 'static_source'
assert 'static_source' not in sys.modules
# 自定义名字协议发生于来源检查，且执行后函数保留原名字对象。
events = []
class Name:
    def __eq__(self, other):
        events.append(other)
        return other == 'static_source'
name = Name()
clone.__name__ = name
spec.loader.exec_module(clone)
assert events == ['static_source']
assert clone.runs == 2 and clone.read() == 42
assert clone.read.__module__ is name
assert 'static_source' not in sys.modules
print('静态来源公开执行及自定义名字比较协议完整保留')
