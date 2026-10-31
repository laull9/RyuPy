# M2.1：动态值 + 控制流的组合——循环计数、条件分支、多返回类型与字符串拼接。
def label(value):
    if value > 2:
        return "big"
    return value


total = 0
for i in range(4):
    total = total + i
print(total, label(5), label(1))

flag = True
print(not flag, 3 // 2, 7 % -3, 2 ** 10, -total, abs(-7))

count = 0
while count < 3:
    count += 1
print(count, count == 3, count != 2)

text = "a"
text = text + "b"
print(text, 1 if flag else 2)
