# 拆分归因：原str_concat的模块级+=段，保持10万次及每次"ab"。
text = ""
for i in range(100000):
    text += "ab"
print(text)
