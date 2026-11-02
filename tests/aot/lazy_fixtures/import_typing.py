# 非目标模块对照：只核真实公开值及重复导入身份。
import typing
import typing as repeated
from typing import Any

print(typing is repeated)
print(Any is typing.Any, Any is repeated.Any)
print(typing.TYPE_CHECKING)
