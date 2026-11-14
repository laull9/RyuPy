# 小方案结构源：Static保原ABI，Dynamic传真实函数，普通实参后读同实例默认值。
def numeric_add(left: int, right: int) -> int:
    return left + right
def numeric_loop(count: int) -> int:
    total = 0
    for index in range(count):
        total = numeric_add(total, index)
    return total
def numeric_direct(left: int, right: int) -> int:
    return numeric_add(left, right)
def numeric_recursive(value: int) -> int:
    if value <= 1:
        return 1
    return value * numeric_recursive(value - 1)
def static_output(value: int) -> None:
    print(value)
def dynamic_add(left, right):
    return left + right
def target(value, extra=7):
    return value + extra
def argument():
    target.__module__ = 'changed-in-argument'
    return 3
def direct_with_argument():
    return target(argument())
alias = target
def through_alias(value):
    return alias(value)
def factory(seed):
    def nested(value=1):
        return seed + value
    return nested
def stream(value):
    yield value
    yield value + 1
