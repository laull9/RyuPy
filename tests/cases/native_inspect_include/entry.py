# 显式include的本地模块及其inspect传递依赖进入真实编译闭图。
import importlib

extra = importlib.import_module("runtime_extra")
print(extra.describe())
