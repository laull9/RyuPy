# 类定义日志用于验证导入与继承顺序，不使用伪模块。
events = ['base loaded']
class Base:
    label = 'base'
    def value(self):
        return self.label

class Other:
    label = 'other'
    def value(self):
        return self.label
