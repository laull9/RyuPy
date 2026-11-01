# ryupy: pending
# 原生object默认dunder全集尚未实现；不能把用户目录子集冒称完整dir兼容。
class Example:
    value = 1
print(dir(Example()))
print(dir(Example))
