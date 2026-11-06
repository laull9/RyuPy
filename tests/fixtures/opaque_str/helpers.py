# 注解只决定对象槽，不约束实际传入对象。
slot: str = 'initial'

def identity(value: str) -> str:
    exact = 'a' + 'b'
    count = 4 + 5
    copy = value
    return copy

def read() -> str:
    return slot

def branch(left: str, right: str, flag: bool) -> str:
    if flag:
        result = left
    else:
        result = right
    return result

def numeric_loop(unused: str, n: int) -> int:
    total = 0
    for i in range(n):
        total += i
    return total

def declared_int(value: str) -> int:
    return value

# 真实函数读取公开文件槽；不读不可变资源位置。
def read_file():
    return __file__
