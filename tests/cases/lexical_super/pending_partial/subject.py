# ryupy: pending
# CP 的 partial(super) 可读取调用帧；S1 不以 TLS 模拟它。
from functools import partial
class Base:
    def identify(self):
        return self
class Child(Base):
    def identify(self):
        return partial(super)().identify()
