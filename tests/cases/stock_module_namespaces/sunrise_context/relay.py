# 显式Python函数A调用编译B，B原生调用必须切换现场；返回A后仍恢复其真实frame。
import builtins
import sys
import reservoir

foreign_builtins = builtins.__dict__.copy()
space = builtins.__dict__.copy()
space.clear()
space['__builtins__'] = foreign_builtins
space['sys'] = sys
space['expected_builtins'] = foreign_builtins
space['expected_globals'] = space
source = """
def drive(target):
    before = (sys._getframe().f_globals is expected_globals,
              sys._getframe().f_builtins is expected_builtins)
    value = target()
    after = (sys._getframe().f_globals is expected_globals,
             sys._getframe().f_builtins is expected_builtins)
    return before, value, after
"""
builtins.exec(source, space)
drive = space['drive']

def direct():
    return reservoir.early(), reservoir.middle(), reservoir.late(), reservoir.after_delete()

def through_python():
    return drive(reservoir.middle), drive(reservoir.late), drive(reservoir.after_delete)

def nested():
    return reservoir.reenter(reservoir.late), drive(lambda: reservoir.reenter(reservoir.late))
