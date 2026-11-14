# 拆分归因：原str_concat的list构造和join段，不把append复制成本归到join。
parts = []
for i in range(100000):
    parts.append("ab")
text = "".join(parts)
print(text)
