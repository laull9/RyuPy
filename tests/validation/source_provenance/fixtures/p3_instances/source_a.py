# 显式Instance源A；重新捕获builtins不应改变实际代码或globals归属。
import warnings
from typing import TypedDict
__builtins__ = __builtins__.copy()
source_marker = 'A'
Top = TypedDict('Top', {'value': int})

def emit(which):
    if which == 1:
        warnings.warn('instance-warning', UserWarning)
    else:
        warnings.warn('instance-warning', UserWarning)
    return TypedDict('Record', {'value': int}).__module__

def stream():
    warnings.warn('instance-generator-first', UserWarning)
    yield TypedDict('First', {'value': int}).__module__
    warnings.warn('instance-generator-second', UserWarning)
    yield TypedDict('Second', {'value': int}).__module__
