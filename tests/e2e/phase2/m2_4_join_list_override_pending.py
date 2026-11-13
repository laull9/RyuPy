# ryupy: pending
# 既有list子类__iter__覆盖准入缺口；不是精确list分配优化新增支持。
class Parts(list):
    def __iter__(self):
        yield 'override'
print(','.join(Parts(['ignored'])))
