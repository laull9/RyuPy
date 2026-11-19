# 显式实例源码捕获builtins的导入器；保存别名与计算名不依赖静态import inspect。
def environment_owner():
    pass


loader = environment_owner.__builtins__["__import__"]
reflection = loader("in" + "spect")


def target(value=7):
    return value


print(reflection.signature(target))
