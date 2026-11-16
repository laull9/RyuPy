# 真正Y代码：默认Execute(target)仍执行Y，且X/Y同target必须共享warning桶。
import warnings
from typing import TypedDict
source_marker = 'Y'

def emit_y():
    warnings.warn('xy-warning', UserWarning)
    return TypedDict('YRecord', {'value': int}).__module__
