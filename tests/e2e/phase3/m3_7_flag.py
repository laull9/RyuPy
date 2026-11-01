# 真Flag不是int：独立类型身份、组合缓存、别名、严格位域与现有魔术协议。
from enum import Enum, Flag

class Access(Flag):
    NONE = 0
    READ = 1
    WRITE = 2
    EXECUTE = 4
    RW = READ | WRITE
    READ_ALIAS = READ

print(issubclass(Flag, Enum), issubclass(Access, Flag), isinstance(Access.READ, int))
print(str(Access), type(Access).__name__)
print(list(Access), len(Access), list(Access.__members__))
print(Access.READ_ALIAS is Access.READ, Access['READ_ALIAS'] is Access.READ)
combo = Access.READ | Access.EXECUTE
print(combo, repr(combo), combo.name, combo.value)
print(combo is Access(5), combo is (Access.EXECUTE | Access.READ))
print(Access(Access.READ) is Access.READ)
print(Access.RW is (Access.READ | Access.WRITE))
print(Access.READ & Access.WRITE, Access.RW ^ Access.WRITE, ~Access.READ)
print(~~Access.READ is Access.READ)
print(bool(Access.NONE), bool(combo), len(combo), list(combo))
print(Access.NONE in combo, Access.READ in combo, combo in Access.READ)
print(3 in Access, 8 in Access, Access.READ in Access)
print(Access.READ == 1, hash(combo) == hash(Access(5)))
for number in [-1, -2, -8, 0, 7]:
    print(number, Access(number))
for number in [8, -9]:
    try:
        Access(number)
    except ValueError:
        print('严格位域拒绝', number)

class Other(Flag):
    READ = 1
for mode in ['left_int', 'right_int', 'other', 'member_contains', 'int_conversion']:
    try:
        if mode == 'left_int':
            Access.READ | 1
        elif mode == 'right_int':
            1 | Access.READ
        elif mode == 'other':
            Access.READ | Other.READ
        elif mode == 'member_contains':
            Other.READ in combo
        else:
            int(Access.READ)
    except TypeError:
        print('Flag不混同int或其它类', mode)
print(Access.READ == Other.READ)
try:
    combo.value = 7
except AttributeError:
    print('成员只读')
try:
    Access.READ = Access.WRITE
except AttributeError:
    print('类成员只读')

# 位定义顺序决定组合显示/迭代顺序；不是始终按数值排序。
class Ordered(Flag):
    SECOND = 2
    FIRST = 1
    FOURTH = 4
print(Ordered(3), list(Ordered(3)), list(Ordered))

# 中间缺位仍严格拒绝，~仅补已声明单bit，不把缺位当有效标志。
class Sparse(Flag):
    A = 1
    B = 8
print(~Sparse.A, ~Sparse(0), Sparse(-8))
for number in [2, 15, -1]:
    try:
        Sparse(number)
    except ValueError:
        print('缺位拒绝', number)
print(Sparse(0), repr(Sparse(0)), Sparse(0).name)

# 只有zero成员的类与完全空类不能用相同的“无单bit”判定。
class ZeroOnly(Flag):
    ZERO = 0
class Empty(Flag):
    pass
print(list(ZeroOnly), len(ZeroOnly), ZeroOnly(0), ~ZeroOnly.ZERO)
try:
    Empty(0)
except TypeError:
    print('空Flag不能构造成员')
