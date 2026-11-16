# 尚未执行；真实导入模块，来源不是exec生成。
from typing import NamedTuple as Root

def make():
    return Root('FunctionalOrigin', [('x', int)])

__name__ = 'late_class_origin'
class ClassOrigin(Root):
    x: int
