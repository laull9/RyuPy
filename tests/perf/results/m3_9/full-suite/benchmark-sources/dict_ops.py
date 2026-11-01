# bench: category=containers
# bench: desc=字典：整数 / 字符串键读写、get、in、items 迭代、del
counts = {}
for i in range(200000):
    key = i % 1000
    counts[key] = counts.get(key, 0) + 1
words = ["alpha", "beta", "gamma", "delta", "epsilon", "zeta", "eta", "theta"]
freq = {}
for i in range(300000):
    w = words[i % 8]
    if w in freq:
        freq[w] += 1
    else:
        freq[w] = 1
total = 0
for _ in range(100):
    for k, v in counts.items():
        total += k + v
for _ in range(50):
    for k in freq:
        total += freq[k]
table = {}
for i in range(100000):
    table[i] = i * 2
for i in range(0, 100000, 2):
    del table[i]
print(len(counts), freq["beta"], total, len(table))
