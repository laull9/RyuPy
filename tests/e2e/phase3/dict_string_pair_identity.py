# ryupy: pending
# 字符串对聚合探针未来MIR接入的观察边界；同次CPython即时生成预期，不保存golden。
def aggregate():
    counts = {}
    for user, action in [("ann", "buy"), ("bob", "view"), ("ann", "buy")]:
        key = (user, action)
        counts[key] = counts.get(key, 0) + 1
        # 具名key可以被locals观察，禁止将其无条件改成永不物化的探针。
        print(locals()["key"] is key, key)
    print(list(counts.items()))
    old = next(iter(counts))
    counts[("ann", "buy")] = 9
    print(next(iter(counts)) is old)
    del counts[("ann", "buy")]
    counts[("ann", "buy")] = 10
    print(list(counts.items()))


class Collision:
    def __hash__(self):
        return hash(("ann", "buy"))

    def __eq__(self, other):
        print("eq", type(other) is tuple, other is observed)
        return other == ("ann", "buy")


aggregate()
stored = Collision()
counts = {stored: 42}
observed = ("ann", "buy")
print(counts.get(observed, 0))
counts[observed] = 43
print(next(iter(counts)) is stored, counts[observed])
