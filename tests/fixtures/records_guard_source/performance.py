def aggregate(n):
    users = ['ann', 'bob', 'cyd', 'dee', 'eve', 'fay', 'gus', 'hal']
    counts = {}
    for i in range(n):
        key = (users[i % 8], 'buy')
        counts[key] = counts.get(key, 0) + 1
    return counts


counts = aggregate(200000)
print(len(counts), counts[('ann', 'buy')], counts[('hal', 'buy')])
