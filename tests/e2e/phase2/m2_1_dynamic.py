# ryupy: pending
# M2.1：异构值、动态返回与标注不强制限制实参。
def twice(value: int) -> int:
    return value + value


def choose(flag):
    if flag:
        return 12
    return "twelve"


value = 3
print(twice(value), twice("ha"))
value = "changed"
print(value, choose(True), choose(False))
print(True + 2, 2 + 0.5, 0 or "fallback", "left" and 7)
