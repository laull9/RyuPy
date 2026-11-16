# 真实双模块循环：B在A完成之前读取已发布状态。
value = 11
import cycle_b
assert cycle_b.seen == 11
