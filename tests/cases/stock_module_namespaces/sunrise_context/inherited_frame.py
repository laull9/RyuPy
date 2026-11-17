# CIL名称读取已保留当前builtins；完整CAPI frame的f_builtins快照另行闭合。
import builtins
import sys

original = builtins.__dict__
replacement = original.copy()

def earlier():
    return sys._getframe().f_builtins is original

__builtins__ = replacement

def later():
    return [sys._getframe().f_builtins is replacement for item in [0]]

print([sys._getframe().f_builtins is original for item in [0]])
print(earlier(), later())
del __builtins__
print([sys._getframe().f_builtins is original for item in [0]])
print(earlier(), later())
