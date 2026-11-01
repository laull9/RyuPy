# bench: category=strings
# bench: desc=字符串 += 累加 10 万次，以及 join 构造对照
text = ""
for i in range(100000):
    text += "ab"
parts = []
for i in range(100000):
    parts.append("ab")
joined = "".join(parts)
print(len(text), len(joined), text == joined)
