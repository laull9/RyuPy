# ryupy: pending
# 完整CP正例：序列解包目标中的lambda在genexpr作用域，记录真实目标求值副作用。
observed = []
box = [0]
def capture(function):
    observed.append(function)
    return 0

iterator = (number for box[capture(lambda: 'target')], number in [(3, 4)])
assert observed == [] and box == [0]
assert list(iterator) == [4]
assert box == [3] and len(observed) == 1
assert observed[0].__qualname__ == '<genexpr>.<lambda>'
assert observed[0]() == 'target'
print(observed[0].__qualname__, observed[0](), box)
