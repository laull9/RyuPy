# ryupy: pending
# 内层类体回退到外层尚未填充的隐式cell；不能误读同名global或提前填类。
__class__ = 'wrong global'
seen = []

def save(method):
    seen.append(method)
    return method

def make():
    class Outer:
        @save
        def original(self):
            return __class__
        class Inner:
            value = __class__
    return Outer

try:
    make()
except NameError:
    print('outer class cell is empty')
try:
    seen[0](None)
except NameError:
    print('escaped method retains empty original cell')
