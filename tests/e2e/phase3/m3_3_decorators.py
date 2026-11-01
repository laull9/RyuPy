# M3.3：装饰器求值 / 应用顺序与替换 callable。
def decorate(label):
    print("evaluate", label)

    def apply(function):
        print("apply", label)

        def wrapper(*args, **kwargs):
            return label + function(*args, **kwargs)

        return wrapper

    return apply


@decorate("outer:")
@decorate("inner:")
def message(value):
    return value


print(message(value="ok"))


def label_class(cls):
    cls.label = "decorated"
    return cls


@label_class
class Item:
    pass


print(Item.label)

# 可调用对象既可以作为装饰器，也可以作为装饰结果。
class CallableWrapper:
    def __init__(self, function):
        self.function = function

    def __call__(self, value):
        return self.function(value) + 1


@CallableWrapper
def number(value):
    return value * 2


print(number(4))


class DecoratorObject:
    def __call__(self, function):
        return CallableWrapper(function)


decorator_object = DecoratorObject()


@decorator_object
def another(value):
    return value + 2


print(another(5))
