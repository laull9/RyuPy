# 固定源码行是完整帧oracle的一部分；尚未执行。
from typing import NamedTuple

class Ready(NamedTuple):
    x: int

def inner():
    return Ready()

def outer():
    return inner()

outer()
