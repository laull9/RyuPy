# 函数定义来源与限定名语义验收。
# 独立完整正例：目标无外层自由名，限定名断言在真实target lambda调用内完成。
iterator = (number for [0][(lambda callback: 0 if callback.__qualname__ == '<genexpr>.<lambda>' else 1)(lambda: 0)], number in [(3, 4)])
assert list(iterator) == [4]
print('generator target lexical lambda', 4)
