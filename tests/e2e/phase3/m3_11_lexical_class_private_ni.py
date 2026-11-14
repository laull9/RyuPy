# ryupy: pending
# 头/正文未执行前静态拒绝真正需要私名改写的名称。
def make():
    class C:
        __field = 1
    return C

print(make().__name__)
