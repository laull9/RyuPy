# ryupy: pending
# M3.1：推导式作用域、嵌套顺序、重复键与闭包绑定。
value = "outer"
print([(left, right) for left in range(3) for right in range(left) if right % 2 == 0])
print([value * 2 for value in range(3)], value)
print({item % 2: item for item in range(4)})
print(sorted({item % 3 for item in range(7)}))
functions = [lambda: item for item in range(3)]
print([function() for function in functions])
