# 使用现有 dict 字段、int 和真实 __module__ 属性，不使用类语法或限定符。
from typing import TypedDict
import sys

print("T:data-cached", "pkg.data" in sys.modules)
print("T:before-create")
Record = TypedDict("Record", {"value": int})
print("T:module", Record.__module__)
print("T:after-create")
