# ryupy: pending
# 负面编译合同由Rust逐段抽取；此源码同时保留CPython语法oracle。
# case: default
def f(x=1):
    return x
# case: annotation
def f(x: int):
    return x
# case: decorator
@print
def f(x):
    return x
# case: matrix_expression
def f(x):
    return x @ x
# case: closure
def f(x):
    def inner():
        return x
    return inner
# case: async
async def f(x):
    return x
# case: language_constant
def f(x):
    return __debug__
globals()["__debug__"] = False
print(f(None))
