# 三种绑定来源都不能退回入口快照或方法调用 TLS。
import subject
first = subject.Child()
second = subject.Child()
assert first.rebound(second) is second
assert first.own_cell(second) is second
assert first.callee_first(second) is second
assert subject.events == ['select']
assert first.positional_only(second) is second
print('current first argument and callee evaluation order')
