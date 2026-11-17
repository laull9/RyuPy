# ryupy: pending
# 薄函数候选；保留公开函数直到正常退出，等待总控生命周期门。
def f(x):
    return x

value = [1, 2]
print(f(value) is value)
print(globals()["f"] is f)
