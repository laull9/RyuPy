# ryupy: pending
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
