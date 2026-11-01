# bench: category=phase3
# bench: desc=推导式：list / dict / set 推导、嵌套 for 与 if 过滤
total = 0
for _ in range(20):
    squares = [x * x for x in range(20000)]
    evens = [x for x in squares if x % 2 == 0]
    index = {x: i for i, x in enumerate(evens)}
    unique = {x % 1000 for x in squares}
    grid = [(i, j) for i in range(100) for j in range(100) if i != j]
    total += len(evens) + len(index) + len(unique) + len(grid)
print(total)
