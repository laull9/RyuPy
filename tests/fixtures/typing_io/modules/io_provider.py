"""I4 辅助模块：真实导入、稳定 from 别名与普通派生，不伪造模块对象。"""
import typing
from typing import BinaryIO as Base

Saved = Base
class Stream(Base):
    def read(self, n=-1):
        return ('provider', n)

class Replacement:
    def read(self, n=-1):
        return ('replacement', n)


class BinaryIO:
    pass


def current_base():
    return Base


def native_base():
    return typing.BinaryIO


# 在真实模块执行期间快照当前global __name__，定义完恢复原值。
initial_name = __name__
try:
    __name__ = 'provider-before-definition'
    class RenamedStream(Base):
        pass
finally:
    __name__ = initial_name
