# 跨模块工厂：三个独立genexpr迫使链接器重映射各自普通/批量resume身份。
def positive():
    return (x * 2 for x in range(6))


def negative():
    return (x * -3 for x in range(-2, 5))


def overflowing():
    return (x * 3 for x in range(4000000000000000000, 4000000000000000003))
