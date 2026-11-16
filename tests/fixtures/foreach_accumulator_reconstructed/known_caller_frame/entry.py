# 本轮全新边界探针，不计常用CP等价通过。
import subject
import sys
def rhs(value):
    try:
        print("frame", sys._getframe(1).f_locals["total"])
    except AttributeError:
        print("frame-unavailable")
    return value
print(subject.fold(5, [3], rhs))
