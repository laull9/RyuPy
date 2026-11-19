# ryupy: pending
# 通用数值/序列变换，参数与全局不按类型标注收窄，溢出和运算异常保留Python语义。
factor = 3
offset = 7
divisor = -4
mask = 5

def total(x):
    scaled = x * factor
    return scaled + offset

def floor(x):
    return x // divisor

def remainder(x):
    return x % divisor

def divide(x):
    return x / divisor

def power(x):
    return x ** -1

def bits(x):
    return ((x << 3) | mask) ^ ((x >> 1) & 3)

def negate(x):
    return -x - +x

def invert(x):
    return ~x

def truth(x):
    return not x

def large(x):
    return x + 1267650600228229401496703205376

for value in [0, -7, 9223372036854775807, 1.25]:
    print(total(value), floor(value), remainder(value), divide(value))
print(power(2), power(-2), bits(31), bits(-31), negate(12), invert(12))
print(truth(None), truth([]), truth([1]), large(9223372036854775807))
factor = 2
offset = "!"
print(total("ab"))
offset = [3]
source = [1, 2]
print(total(source), source)
divisor = 0
for operation in [floor, remainder, divide, power]:
    try:
        operation(0)
    except ZeroDivisionError as error:
        print(type(error).__name__, error.args, str(error))
