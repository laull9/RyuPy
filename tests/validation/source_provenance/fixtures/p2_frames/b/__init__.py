# 第二个真实Legacy包，不用同名属性替代代码归属。
import a
from typing import TypedDict

def bridge(function, level, label):
    return function(level, label)

@a.decorate
def decorated():
    return TypedDict('Decorated', {'value': int}).__module__
