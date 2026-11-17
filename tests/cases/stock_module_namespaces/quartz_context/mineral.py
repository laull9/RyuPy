# 不同命名与数据布局：工厂内创建函数、同D重绑、删除后旧捕获仍由CAPI可见。
import builtins
import sys

namespace = sys.modules[__name__].__dict__
standard = builtins.__dict__
alternate = standard.copy()
alternate['abs'] = lambda value: 70 + standard['abs'](value)

def create(expected):
    def examine(value):
        return (sys._getframe().f_globals is namespace,
                sys._getframe().f_builtins is expected, abs(value), '__builtins__' not in namespace,
                standard['type'](sys._getframe().f_builtins) is standard['dict'])
    return examine

old = create(standard)
__builtins__ = alternate
fresh = create(alternate)
del __builtins__
retained = create(standard)

# 字面量字典沿Runtime已有映射投影；捕获桥不能只接受foreign PythonObject字典。
literal = {'abs': alternate['abs']}
__builtins__ = literal
from_literal = create(literal)
del __builtins__

__builtins__ = alternate

def bridge(callback):
    before = sys._getframe().f_builtins is alternate
    result = callback()
    return before, result, sys._getframe().f_builtins is alternate

del __builtins__
