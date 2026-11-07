# 本轮全新边界探针，不计常用CP等价通过。
import subject
class Index:
    def __index__(self):
        print("index")
        return 3
try:
    print(subject.fold(0, 0, Index(), 1))
except TypeError:
    print("TypeError")
