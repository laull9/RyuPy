# 警告来源：仅显式注册空子包，不调用资源函数。
print("W:entry-before")
import pkg
print("W:entry-after")
