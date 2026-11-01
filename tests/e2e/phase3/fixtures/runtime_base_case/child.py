# 相对导入包重导出：与固定 progress 项目的首阻断形状一致。
from . import Base as Parent
from .base import events
events.append('child body')
class Child(Parent):
    def value(self):
        return super().value() + ':child'
