# 完整函数frame待实现输入；保留实际观察后再断言，不能用模块D冒充函数locals。
from business_frame_peer import read_frame

observed = read_frame('caller-token')
print('function-frame', observed, flush=True)
assert observed == (True, 17, 'read_frame', 8)
