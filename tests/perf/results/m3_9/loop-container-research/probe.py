def tuple_iteration(a, b, c):
    total = 0
    for value in (a, b, c):
        total += value
    return total

def invariant(n: int, a: int, b: int) -> int:
    total: int = 0
    for i in range(n):
        total += (a ^ b) & 255
    return total

def list_index(n, values):
    total = 0
    for i in range(n):
        total += values[0]
    return total

print(tuple_iteration(1,2,3), invariant(3, 12, 7), list_index(3, [5,6]))
