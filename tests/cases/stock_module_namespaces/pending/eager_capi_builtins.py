# ryupy: pending
# CIL名称读取已保留当前builtins；完整CAPI frame的f_builtins快照另行闭合。
import builtins
import sys

original = builtins.__dict__
replacement = original.copy()
__builtins__ = replacement
print([sys._getframe().f_builtins is original for item in [0]])
del __builtins__
print([sys._getframe().f_builtins is original for item in [0]])
