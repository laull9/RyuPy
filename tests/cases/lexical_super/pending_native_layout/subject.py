# ryupy: pending
# 内建 list 布局不随普通实例零参 super 开放。
class Child(list):
    def append(self, value):
        super().append(value)
