# 非Enum类正常属性写及方法调用，不应被新guard改变。
class Ordinary:
    value = 1
    def read(self):
        return self.value
