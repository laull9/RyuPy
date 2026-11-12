# 实际MI基经普通动态槽进入新准备入口；不伪装成编译期已认证ABC基。
import collections.abc as abc
Base = abc.Sequence

class Derived(Base):
    pass

print('dynamic-mi-created')
