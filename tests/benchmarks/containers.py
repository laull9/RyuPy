# 基准：容器与字符串的动态方法 / 下标路径。
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
