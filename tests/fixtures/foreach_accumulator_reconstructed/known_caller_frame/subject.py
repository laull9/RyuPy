# 本轮全新边界探针，不计常用CP等价通过。
def fold(seed, values, rhs):
    total = seed
    for value in values:
        total += rhs(value)
    return total
