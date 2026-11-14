# 拆分归因：同样的+=负载放入普通函数局部；动态实参保既有holder候选形状。
def accumulate(initial, count, piece):
    text = initial
    for i in range(count):
        text += piece
    return text


text = accumulate("", 100000, "ab")
print(text)
