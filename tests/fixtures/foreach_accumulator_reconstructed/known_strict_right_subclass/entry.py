# 本轮全新边界探针，不计常用CP等价通过。
import subject
class Left:
    def __add__(self, other):
        print("left-add")
        return 10
class Right(Left):
    def __radd__(self, other):
        print("right-radd")
        return 20
print(subject.fold(Left(), [Right()]))
