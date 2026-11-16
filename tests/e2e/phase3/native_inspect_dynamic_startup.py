# 保存动态导入函数并计算模块名；编译期未知名字也要保守保留原生inspect。
import importlib as dispatch

loader = dispatch.import_module
name = "in" + "spect"
reflection = loader(name)


def target(a, b=3):
    return a + b


print(reflection.signature(target))
print(reflection.signature(target).bind(1).arguments)
