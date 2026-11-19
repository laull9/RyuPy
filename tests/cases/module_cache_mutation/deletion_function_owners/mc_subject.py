# 函数与默认容器必须按本次模块执行创建。
import mc_state
mc_state.runs += 1
value = mc_state.runs
def read():
    return value
def store(item, bucket=[]):
    bucket.append(item)
    return bucket
