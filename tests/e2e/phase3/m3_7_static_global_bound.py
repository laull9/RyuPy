# 静态整数在读取时检查绑定，不得把 CLR 默认 0 当已赋值。
def read_number() -> int:
    return number

try:
    read_number()
except NameError:
    print('静态整数未绑定')
number = 42
print(read_number())
