"""裸return经动态返回槽及调用转发后必须仍是Python None。"""
def choose(flag):
    if flag:
        return
    return "value"


def forward(flag):
    return choose(flag)


print(choose(True) is None)
print(forward(True) is None)
print(choose(False))
print(forward(False))
