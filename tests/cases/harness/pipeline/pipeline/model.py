# 数据模型：单继承与 super 链。
class Record:
    def __init__(self, name, value):
        self.name = name
        self.value = value

    def describe(self):
        return self.name + "=" + str(self.value)

    def scaled(self, factor):
        return self.value * factor


class NamedRecord(Record):
    def __init__(self, name, value, tag):
        super().__init__(name, value)
        self.tag = tag

    def describe(self):
        return self.tag + ":" + super().describe()
