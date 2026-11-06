# 顶层实例名字必须先读真实字典；外部类型变更及builtin遮蔽不能走旧静态优化。
import sys
import support
before = len([1, 2, 3])
len = support.alternative
after = len([1, 2, 3])
annotated: int = 10
support.inject(sys.modules[__name__])
observed = never_declared
current_type = annotated
range = support.custom_range
total = 0
for item in range(100):
    total += item
