# X真实ModuleMain入口读目标环境；编译source绝不能由目标State.Code=Y反推。
import warnings
from typing import TypedDict

source_marker = 'X'
__builtins__ = __builtins__.copy()
__name__ = 'body-current-name'
Top = TypedDict('EntryRecord', {'value': int})

def emit():
    warnings.warn('xy-warning', UserWarning)
    return TypedDict('FunctionRecord', {'value': int}).__module__

class NativeMarker:
    def __bool__(self):
        return int("not-an-integer")
