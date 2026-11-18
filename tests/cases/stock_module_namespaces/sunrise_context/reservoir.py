# 同一真实D在多个定义点替换/删除builtins；原生现场与普通名称查找必须读同一捕获。
import builtins
import sys

owner = sys.modules[__name__]
original = builtins.__dict__
base_len = original['len']

def early():
    return (sys._getframe().f_globals is owner.__dict__, sys._getframe().f_builtins is original,
            len('abc'), '__builtins__' not in owner.__dict__)

first = original.copy()

def first_len(value):
    return 10 + base_len(value)

first['len'] = first_len
__builtins__ = first

def middle():
    return (sys._getframe().f_globals is owner.__dict__, sys._getframe().f_builtins is first,
            len('abc'), '__builtins__' not in owner.__dict__)

second = original.copy()

def second_len(value):
    return 20 + base_len(value)

second['len'] = second_len
__builtins__ = second

def late():
    return (sys._getframe().f_globals is owner.__dict__, sys._getframe().f_builtins is second,
            len('abc'), '__builtins__' not in owner.__dict__)

del __builtins__

def after_delete():
    return (sys._getframe().f_globals is owner.__dict__, sys._getframe().f_builtins is original,
            len('abc'), '__builtins__' not in owner.__dict__)

deleted = '__builtins__' not in owner.__dict__
module_frame = sys._getframe().f_builtins is original

# 在first定义点创建重入外层，内层不同捕获返回后必须恢复first。
__builtins__ = first

def reenter(target):
    before = sys._getframe().f_builtins is first
    result = target()
    return before, result, sys._getframe().f_builtins is first

del __builtins__
