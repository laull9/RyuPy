# 通用伴生核心的源码输入；原定义均无默认值，静态核心保留整数ABI。
def required(value: int) -> int:
    return value


def sibling(value: int) -> int:
    return value


def forward(value: int) -> int:
    return sibling(value)


def recursive(value: int, count: int) -> int:
    if count <= 0:
        return value
    return recursive(value, count - 1)


def keyword(value: int, *, other: int) -> int:
    return other


def numeric(value: int) -> int:
    return value + 1


def numeric_call(value: int) -> int:
    return numeric(value)


def effect(value: int) -> int:
    print("body-once")
    return value
