# 导入 right 前先暴露 phase，供循环另一端观察。
print("left init")
phase = "starting"
from . import right

phase = "ready"
