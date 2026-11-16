# 同名定义拥有不同函数体与FnId；冷注册不能按签名或名字配对。
def required(value: int) -> int:
    return value + value
