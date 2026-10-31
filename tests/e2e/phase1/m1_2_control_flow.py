# ryupy: pending
# M1.2：if/elif/else、while、break/continue、短路求值。
def classify(n: int) -> int:
    if n < 0:
        return -1
    elif n == 0:
        return 0
    else:
        return 1


def collatz_steps(n: int) -> int:
    steps = 0
    while n != 1:
        if n % 2 == 0:
            n = n // 2
        else:
            n = 3 * n + 1
        steps += 1
    return steps


def first_multiple(k: int, limit: int) -> int:
    i = 1
    found = -1
    while True:
        if i > limit:
            break
        if i % k != 0:
            i += 1
            continue
        found = i
        break
    return found


print(classify(-5), classify(0), classify(7))
print(collatz_steps(27))
print(first_multiple(7, 100))
print(first_multiple(7, 3))
x = 5
print(x > 0 and x < 10)
print(x < 0 or x == 5)
