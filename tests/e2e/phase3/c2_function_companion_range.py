# ryupy: pending
# 伴生range在实参后构造一次；原typed完整实参、可变缺省身份、步长与控制流均受保护。
import c2_function_companion_range_helper as helper

print(helper.fold(3, 4), helper.walk(0, 0, 5, 1))
helper.fold.__defaults__ = (5, 4)
print(helper.fold())
shared = ["same"]
helper.fold.__defaults__ = (shared, 0)
print(helper.fold() is shared, helper.fold() is helper.fold())
helper.fold.__defaults__ = (5, True)
print(helper.fold())
helper.walk.__defaults__ = (0, 3, -4, -1)
print(helper.walk())
helper.walk.__defaults__ = (0, 1, 4, 1)
print(helper.walk())
helper.walk.__defaults__ = (0, 1, 4, 0)
try:
    helper.walk()
except ValueError as error:
    print(type(error).__name__, str(error))
order = []
def argument():
    order.append("argument")
    helper.walk.__defaults__ = (0, 1, 4, 1)
    return 10
print(helper.walk(argument()), order)
