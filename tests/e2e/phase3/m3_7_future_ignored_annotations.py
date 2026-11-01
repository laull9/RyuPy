# future 注解限制只属于当前注解作用域，不能误拒 lambda/推导式内部的合法表达式。
from __future__ import annotations
class Holder:
    pass
holder = Holder()
def update():
    holder.first: (lambda: (yield 1)) = 7
    holder.second: (lambda: (captured := int)) = 8
    holder.third: [(captured := int) for _ in []] = 9
    holder.fourth: ((captured := int) for _ in []) = 10
    return '普通函数'
print(update())
print(holder.first, holder.second, holder.third, holder.fourth)
