# ryupy: pending
# 基类表达式之前结构拒绝MI，不借异常回放旧路径。
class A:
    pass
class B:
    pass

def make():
    class C(A, B):
        value = 1
    return C

print(make().value)
