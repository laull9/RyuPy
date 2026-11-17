# 失败模块体先发布无捕获普通函数，再由真实 importlib 回滚缓存。
import control

offset = 40


def read(x):
    return x + offset


control.escaped.append(read)
raise RuntimeError("stock 导入失败")
