# ryupy: pending
# M2.3：负索引、扩展切片、切片赋值和边界错误。
values = [0, 1, 2, 3, 4, 5]
print(values[-1], values[::-2], values[-100:100])
values[1:3] = [8, 9, 10]
print(values)
values[::2] = [20, 21, 22, 23]
del values[1::2]
print(values)
try:
    values[::2] = [1]
except ValueError:
    print("extended slice length")
try:
    print(values[100])
except IndexError:
    print("index error")
try:
    print(values[::0])
except ValueError:
    print("zero step")
