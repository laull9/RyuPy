# 固定 CP 执行全部断言；RyuPy 当前在 builtins 的真实导入先失败，不冒称运行时 eval 已验收。
import subject

assert subject.direct_value == 11
assert subject.compiled_value == 12
assert subject.namespace['value'] == 9
assert subject.aliased_value == 13
assert subject.reflected_value == 14
assert subject.local_result == 30
print('real builtin module, aliases, reflected calls and local frame environment')
