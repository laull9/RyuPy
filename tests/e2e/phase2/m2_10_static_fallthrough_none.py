# 部分返回必须保留隐式None；完整返回函数仍可走静态整数快路。
def conditional(flag: bool) -> int:
    if flag:
        return 7


def caller(flag: bool) -> int:
    return conditional(flag)


def complete(flag: bool) -> int:
    if flag:
        return 7
    else:
        return 9


def loop_partial(n: int) -> int:
    for i in range(n):
        return 7


def loop_break(flag: bool) -> int:
    while True:
        if flag:
            break
        return 7


def loop_else(n: int) -> int:
    for i in range(n):
        if i == 1:
            break
    else:
        return 9


def caught(flag: bool) -> int:
    try:
        if flag:
            raise ValueError("test")
        return 7
    except ValueError:
        pass


def final_override(flag: bool) -> int:
    while True:
        try:
            return 7
        finally:
            if flag:
                break


def final_complete(flag: bool) -> int:
    try:
        if flag:
            return 7
    finally:
        return 9


for flag in (False, True):
    for result in (conditional(flag), caller(flag), loop_break(flag),
                   caught(flag), final_override(flag)):
        print(result, result is None, type(result).__name__)
for n in (0, 1, 3):
    print(loop_partial(n), loop_else(n))

print(complete(False), complete(True), final_complete(False), final_complete(True))
