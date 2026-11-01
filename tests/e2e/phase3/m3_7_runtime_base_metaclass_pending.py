# ryupy: pending
# 非类基对象可能通过其type充当元类；CPython先运行类体，再在最终构造处失败。
# 首切普通PyClass guard不模拟这种元类协议，不能误计为兼容。
class Holder:
    pass
holder = Holder()
holder.base = 7
Existing = 'old'
log = []
try:
    class Existing(holder.base):
        log.append('body')
except TypeError:
    print(Existing, log)
