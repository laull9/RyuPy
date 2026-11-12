# 真实源码导出的普通类及已有内建布局别名，不含MI定义。
class Plain:
    def __init__(self, value):
        self.value = value

    def read(self):
        return self.value

ListBase = list
DictBase = dict
StrBase = str
