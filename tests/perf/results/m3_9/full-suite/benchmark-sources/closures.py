# bench: category=phase3
# bench: desc=闭包与 lambda：工厂函数、共享 cell 写入、lambda 作为 key
def make_adder(n):
    def add(x):
        return x + n
    return add


def counter():
    count = 0

    def step():
        nonlocal count
        count += 1
        return count
    return step


add5 = make_adder(5)
total = 0
for i in range(1000000):
    total = add5(total)
step = counter()
for i in range(1000000):
    step()
pairs = []
for i in range(50000):
    pairs.append((i % 97, i))
pairs.sort(key=lambda p: p[0])
print(total, step(), pairs[0])
