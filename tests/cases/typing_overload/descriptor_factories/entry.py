# 完整公开语义已通过固定CP与B3差分；保原源码行号。
# 完整应用：classmethod/staticmethod两种堆叠顺序，registry存原参数而非unwrap结果。
from typing import overload, get_overloads, clear_overloads

clear_overloads()
saved = []
def retain(value):
    saved.append(value)
    return value

class Factory:
    @overload
    @retain
    @classmethod
    def make(cls, value: int):
        return "cm stub"

    @classmethod
    @overload
    @retain
    def make(cls, value: str):
        return "function stub"

    @classmethod
    def make(cls, value):
        return cls.__name__ + ":" + str(value)

    @overload
    @retain
    @staticmethod
    def parse(value: int):
        return "sm stub"

    @staticmethod
    @overload
    @retain
    def parse(value: str):
        return "plain stub"

    @staticmethod
    def parse(value):
        return "parse:" + str(value)

print(Factory.make(7), Factory().make("ok"), Factory.parse(9), Factory().parse("yes"))
cm = get_overloads(Factory.make)
sm = get_overloads(Factory.parse)
print([type(value).__name__ for value in cm], [type(value).__name__ for value in sm])
print(cm[0] is saved[0], cm[1] is saved[1], sm[0] is saved[2], sm[1] is saved[3])
print(cm[0].__func__.__qualname__, sm[0].__func__.__qualname__)
print(cm[0].__func__(Factory, 1), cm[1](Factory, "x"), sm[0](1), sm[1]("x"))
print(get_overloads(cm[0])[0] is cm[0], get_overloads(sm[0])[0] is sm[0])
print(get_overloads(Factory().make)[1] is cm[1])
print(clear_overloads(), get_overloads(Factory.make), get_overloads(Factory.parse))
