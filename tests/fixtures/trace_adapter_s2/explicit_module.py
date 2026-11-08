# 模块本身为真实接收方，不能错误移除模块帧。
try:
    raise ValueError("module")
except ValueError as caught:
    result = caught
