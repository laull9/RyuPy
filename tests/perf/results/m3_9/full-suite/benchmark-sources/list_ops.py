# bench: category=containers
# bench: desc=列表 append / 下标读写 / pop / 切片 / extend / insert / in / index
items = []
for i in range(300000):
    items.append(i)
total = 0
for i in range(300000):
    total += items[i]
for i in range(300000):
    items[i] = items[i] + 1
for x in items:
    total += x
while len(items) > 150000:
    total += items.pop()
chunk = items[1000:2000]
for i in range(200):
    part = items[i:i + 500]
    total += len(part)
more = []
for i in range(2000):
    more.extend(chunk)
small = []
for i in range(2000):
    small.insert(0, i)
hits = 0
for i in range(300):
    if i in small:
        hits += 1
    hits += small.index(i)
print(total, len(more), hits)
