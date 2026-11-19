# ryupy: pending
# 模块和函数内for range都必须读当前绑定；不能仅按文本名称套用builtin计数循环。
builtin_range = range

def read_range():
    global range
    return range

def delete_range():
    global range
    del range

def collect(stop):
    values = []
    for value in range(stop):
        values.append(value)
    return values

# 模块初始lambda遮蔽：真实结果只有7，不能被静态ForRange替换成0、1。
range = lambda stop: [7]
saved_lambda = range
values = []
for value in range(2):
    values.append(value)
function_values = collect(2)
assert values == [7] and function_values == [7]
assert read_range() is saved_lambda
print('range module shadow', values, function_values)

# 模块del后读才回退builtin，同一函数体也必须看到新的绑定。
del range
values = []
for value in range(2):
    values.append(value)
function_values = collect(2)
assert values == [0, 1] and function_values == [0, 1]
assert read_range() is builtin_range
print('range module deleted', values, function_values)

# 删除后重绑普通函数，调用各发生一次；函数内for不能静态冻结为builtin。
calls = []

def custom_range(stop):
    calls.append(stop)
    return [stop + 10, stop + 20]

range = custom_range
saved_custom = range
values = []
for value in range(2):
    values.append(value)
function_values = collect(3)
assert values == [12, 22] and function_values == [13, 23]
assert calls == [2, 3] and read_range() is saved_custom
print('range function rebound', values, function_values)

# 函数global del修改模块绑定，模块循环和函数循环随后都恢复builtin。
delete_range()
values = []
for value in range(3):
    values.append(value)
function_values = collect(3)
assert values == [0, 1, 2] and function_values == [0, 1, 2]
assert calls == [2, 3] and read_range() is builtin_range
print('range global deleted', values, function_values)
try:
    delete_range()
except NameError as error:
    assert str(error) == "name 'range' is not defined"
    print('range delete builtin', type(error).__name__, str(error))
else:
    assert False
assert read_range() is builtin_range

# 再次以新lambda遮蔽，已经运行过的循环位置和旧函数仍须动态跟随。
range = lambda stop: [stop + 30, stop + 40]
values = []
for value in range(2):
    values.append(value)
function_values = collect(3)
assert values == [32, 42] and function_values == [33, 43]
assert read_range() is range and range is not saved_lambda
print('range lambda rebound', values, function_values)

# 保存的旧引用保持原行为；恢复builtin不修改其对象或自定义函数的副作用历史。
del range
assert read_range() is builtin_range
assert saved_lambda(99) == [7]
assert saved_custom(9) == [19, 29] and calls == [2, 3, 9]
values = []
for value in range(1):
    values.append(value)
assert values == [0]
print('range aliases and builtin restored', values, calls)
