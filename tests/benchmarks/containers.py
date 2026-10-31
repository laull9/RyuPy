# bench: category=containers
# bench: desc=20 万 append + 10 万字典读写 + 5 万次字符串拼接（M2.11 起的对照）
items = []
for i in range(200000):
    items.append(i)

lookup = {}
for i in range(100000):
    lookup[i] = i
total = 0
for i in range(100000):
    total += lookup[i]

text = "a"
for i in range(50000):
    text = text + "b"

print(len(items), total, len(text))
