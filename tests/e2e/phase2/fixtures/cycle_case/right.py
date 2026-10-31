# 读取尚未执行完毕的 left；不能重新初始化 left。
print("right init")
from . import left

seen = left.phase
