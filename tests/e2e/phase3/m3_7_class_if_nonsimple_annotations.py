# 括号名字标注仍求表达式，但不是字段登记；有初值时先赋值再求标注。
from dataclasses import dataclass, fields

events = []


def record(label, value):
    events.append(label)
    return value


class Conditional:
    before = __annotations__
    if True:
        (absent): record('annotation only', int)
        (assigned): record('annotation after assignment', assigned) = record('value', 9)
    if False:
        (never): record('wrong annotation', str)
    after = __annotations__


print(events)
print(Conditional.before is Conditional.after, Conditional.__annotations__)
print(hasattr(Conditional, 'absent'), Conditional.assigned, hasattr(Conditional, 'never'))
Conditional = dataclass(Conditional)
print(len(fields(Conditional)), Conditional().assigned)

# 非条件路径共用同一类体降级，不能只修 if 特判。
class Straight:
    (value): record('straight annotation', int) = 3


print(Straight.__annotations__, Straight.value)
Straight = dataclass(Straight)
print(len(fields(Straight)), events)
