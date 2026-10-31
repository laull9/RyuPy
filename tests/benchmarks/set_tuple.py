# bench: category=containers
# bench: desc=集合 add / in / 运算，元组构造、解包与作为字典键
seen = set()
for i in range(300000):
    seen.add(i % 5000)
hits = 0
for i in range(300000):
    if i in seen:
        hits += 1
a = set()
b = set()
for i in range(2000):
    a.add(i)
    b.add(i * 2)
for _ in range(50):
    hits += len(a & b) + len(a | b) + len(a - b)
grid = {}
for x in range(300):
    for y in range(300):
        grid[(x, y)] = x + y
total = 0
for x in range(300):
    for y in range(300):
        total += grid[(x, y)]
for i in range(300000):
    p = (i, i + 1, i + 2)
    q, r, s = p
    total += q + r + s
print(len(seen), hits, total)
