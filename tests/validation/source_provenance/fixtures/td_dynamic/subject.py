# 真实Dynamic消费者；调用时读取实际函数Module，不读取公开binding或当前name。
from typing import TypedDict

def make():
    return TypedDict('Record', {'value': int})
