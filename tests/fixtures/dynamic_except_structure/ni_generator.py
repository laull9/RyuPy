# 首切明确不支持：新增动态handler所在函数本身是生成器。
def unsupported(target):
    try:
        yield 1
    except target:
        yield 2
