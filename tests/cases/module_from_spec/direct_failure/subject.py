# 使用已支持的普通控制模块，不依赖globals()反射。
import control
started = True
value = 17
def read():
    return value
control.escaped.append(read)
if control.fail:
    raise RuntimeError('body failed')
finished = True
