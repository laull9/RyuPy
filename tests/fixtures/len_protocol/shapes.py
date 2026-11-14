# 完整len绑定不遗漏实参；数值循环与exact容器仍使用原有MIR路径。
def mark(label, value):
    print(label)
    return value
def pair():
    return len(mark('first', []), mark('second', []))
def zero():
    return len()
def single(value):
    return len(value)
def star(value):
    return len(*value)
def keyword(value):
    return len(obj=value)
def double(value):
    return len(value, **{})
def exact_list():
    return len([1, 2])
def exact_text():
    return len('a🙂')
def numeric(n: int) -> int:
    total = 0
    for i in range(n):
        total += i
    return total
