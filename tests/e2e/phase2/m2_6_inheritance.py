# M2.6：单继承、super、继承属性与子类判断。
class Base:
    def __init__(self, value):
        self.value = value

    def describe(self):
        return "base"


class Child(Base):
    def __init__(self, value):
        super().__init__(value + 1)

    def describe(self):
        return super().describe() + ":child"


child = Child(4)
print(child.value, child.describe())
print(isinstance(child, Base), issubclass(Child, Base))
print(isinstance(True, int), type(True) is int)

base = Base(2)
print(base.describe())


def replacement(self):
    return "updated"


Base.describe = replacement
print(base.describe(), child.describe())
