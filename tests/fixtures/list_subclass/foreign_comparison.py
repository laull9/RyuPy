# 真实list派生与外部对象的eq/ne分派；显式ne拒绝后不得再转eq。
class L(list):
    pass

class Both:
    def __eq__(self, other):
        print('eq')
        return False
    def __ne__(self, other):
        print('ne')
        return False

class EqOnly:
    def __eq__(self, other):
        print('eq-only')
        return False

class RejectNe:
    def __eq__(self, other):
        print('eq-unused')
        return False
    def __ne__(self, other):
        print('ne-NI')
        return NotImplemented

value = L([1])
for foreign in [Both(), EqOnly(), RejectNe()]:
    print(value != foreign)
    print(foreign != value)
    print(value == foreign)
    print(foreign == value)
print(value == [1], value != [1], [1] == value, [1] != value)
