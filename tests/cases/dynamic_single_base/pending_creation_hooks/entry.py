# 运行期守卫仍需拒绝未实现钩子，直到本完整 CP 行为真正被实现。
from bases import Base, events
class Child(Base):
    pass
print(events)
