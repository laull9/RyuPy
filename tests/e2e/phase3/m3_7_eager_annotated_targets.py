# ryupy: pending
# 非 future 的模块目标注解要实际执行，不能复用字符串化路径静默忽略。
log = []
class Holder:
    pass
holder = Holder()
items = [0]
def base():
    log.append('base')
    return holder
def sequence():
    log.append('sequence')
    return items
def index():
    log.append('index')
    return 0
def rhs():
    log.append('rhs')
    return 7
def annotation():
    log.append('annotation')
    return int
base().value: annotation() = rhs()
sequence()[index()]: annotation() = rhs()
base().missing: annotation()
sequence()[index()]: annotation()
print(log)
print(holder.value, items, hasattr(holder, 'missing'), __annotations__)
try:
    holder.value: MissingAnnotation = 9
except NameError:
    print('先存储后注解', holder.value)
