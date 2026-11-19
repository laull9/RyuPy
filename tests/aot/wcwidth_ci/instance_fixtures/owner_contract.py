# 原样计算模块的外部owner因果合同；业务调用始终通过原wrapper或wcswidth。
import sys
import importlib.util as util
import wcwidth as package
import wcwidth.wcwidth
import wcwidth.table_zero as zero
m = sys.modules['wcwidth.wcwidth']
assert package.wcwidth is m.wcwidth and package.wcswidth is m.wcswidth
assert m.wcswidth.__globals__ is m.__dict__
assert m.wcwidth.__wrapped__.__globals__ is m.__dict__
print('package-original', package.wcwidth('汉'), package.wcswidth('汉字'))
spec = util.find_spec('wcwidth.wcwidth')
left = util.module_from_spec(spec)
right = util.module_from_spec(spec)
spec.loader.exec_module(left)
spec.loader.exec_module(right)
assert left.__dict__ is not right.__dict__
assert left.wcswidth.__globals__ is left.__dict__
assert right.wcswidth.__globals__ is right.__dict__
assert left.wcwidth.__wrapped__.__globals__ is left.__dict__
assert left._wcversion_value.__wrapped__.__globals__ is left.__dict__
assert left._wcmatch_version.__wrapped__.__globals__ is left.__dict__
assert left.wcwidth is not right.wcwidth
assert left._wcversion_value is not right._wcversion_value
assert left._wcmatch_version is not right._wcmatch_version
assert left.ZERO_WIDTH is right.ZERO_WIDTH and left.ZERO_WIDTH is zero.ZERO_WIDTH
print('owners-and-caches', left.wcwidth.cache_parameters(), left._wcversion_value.cache_parameters(), left._wcmatch_version.cache_parameters())

def absent(ucs, table):
    return 0

def present(ucs, table):
    return 1

left._bisearch = absent
right._bisearch = present
print('helper-miss', left.wcwidth('汉'), right.wcwidth('汉'), package.wcwidth('汉'))
print('while-owner', left.wcswidth('汉字'), right.wcswidth('汉字'), package.wcswidth('汉字'))
assert left.wcwidth('汉') == 1 and right.wcwidth('汉') == 0
old = left.wcwidth
old_body = old.__wrapped__
old_width = left.wcswidth
globals_alias = left.__dict__
spec.loader.exec_module(left)
assert left.__dict__ is globals_alias
assert old_body.__globals__ is globals_alias
assert left.wcwidth is not old and left.wcswidth is not old_width
print('old-hit', old('汉'))
old.cache_clear()
print('old-miss-after-reexec', old('汉'), old_width('汉字'), left.wcwidth('汉'), right.wcwidth('汉'))
assert old('汉') == 2 and old_width('汉字') == 4
assert sys.modules['wcwidth.wcwidth'] is m and package.wcwidth is m.wcwidth
# from导入保原对象；属性重绑不追随，内部突变经cache miss可见。
original_table = zero.ZERO_WIDTH
original_ranges = original_table['15.1.0']
zero.ZERO_WIDTH = {}
assert left.ZERO_WIDTH is original_table and right.ZERO_WIDTH is original_table
original_table['15.1.0'] = ((ord('汉'), ord('汉')),)
left.wcwidth.cache_clear()
print('shared-table-mutation', left.wcwidth('汉', '15.1.0'))
assert left.wcwidth('汉', '15.1.0') == 0
original_table['15.1.0'] = original_ranges
zero.ZERO_WIDTH = original_table
# 定义时builtins捕获：改目标dict键不改变旧函数captured builtins。
def ascii_ord(value):
    return 65

def control_ord(value):
    return 128

captured = m.wcswidth.__builtins__.copy()
captured['ord'] = ascii_ord
left.__builtins__ = captured
spec.loader.exec_module(left)
old = left.wcwidth
assert old.__wrapped__.__builtins__ is captured
replacement = captured.copy()
replacement['ord'] = control_ord
left.__builtins__ = replacement
old.cache_clear()
print('captured-before-reexec', old('汉'))
assert old('汉') == 1
spec.loader.exec_module(left)
old.cache_clear()
print('captured-after-reexec', old('汉'), left.wcwidth('汉'))
assert old('汉') == 1 and left.wcwidth('汉') == -1
assert old.__wrapped__.__builtins__ is captured
assert left.wcwidth.__wrapped__.__builtins__ is replacement
print('owner-contract-complete')
