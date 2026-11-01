# 方法装饰器在原始类命名空间中求值；property更新复制描述符，方法仍按实例绑定。
def traced(function):
    print("decorate", function.__name__)
    def wrapper(self, value):
        return function(self, value) + 1
    return wrapper


class Item:
    offset = 3
    decorator = traced

    @decorator
    def add(self, value=offset):
        return value

    @staticmethod
    def static(value):
        return value + 2

    @classmethod
    def label(cls):
        return cls.__name__

    @property
    def value(self):
        return self._value

    @value.setter
    def value(self, value):
        self._value = value

    @value.deleter
    def value(self):
        del self._value


item = Item()
print(item.add(4), item.static(5), item.label())
item.value = 6
print(item.value)
del item.value
print(hasattr(item, "_value"))


class Child(Item):
    pass


print(Item.label(), Child.label(), Child().label())
