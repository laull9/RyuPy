# 非类型基值需要动态元类协议：CP 会先执行类体，再由 int 元类构造失败。
# 本切普通类准备入口仍明确拒绝此域；不能把它改成“预期 NI”来算通过。
old = object()
Target = old
InvalidBase = 42
events = []
try:
    class Target(InvalidBase):
        events.append('body-ran')
except TypeError:
    print('dynamic metaclass failed', Target is old, events)
