# 待跑合同输入；未执行，不能作为已验 CP 事实。
import state
import dependency
state.dependency = dependency
state.attempts += 1
print('F:child', state.attempts)
if state.attempts == 1:
    raise state.failure
value = 29
