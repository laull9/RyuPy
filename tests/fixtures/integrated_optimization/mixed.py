# 同一源码同时包含records实体键累加与生成器sum；组合门必须核两类实际发射。
def aggregate(n):
    users = ['ann', 'bob']
    counts = {}
    for i in range(n):
        key = (users[i % 2], 'buy')
        counts[key] = counts.get(key, 0) + 1
    return counts


counts = aggregate(40)
print(len(counts), counts[('ann', 'buy')], counts[('bob', 'buy')])
print(sum(x * 2 for x in range(100)))
