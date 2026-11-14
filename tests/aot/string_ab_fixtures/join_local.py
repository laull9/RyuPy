# 拆分归因：同样的list构造和join负载放入普通函数局部。
def construct(count, piece):
    parts = []
    for i in range(count):
        parts.append(piece)
    return "".join(parts)


text = construct(100000, "ab")
print(text)
