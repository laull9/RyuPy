def replace(function):
    def wrapper():
        return "wrapped"
    return wrapper


@replace
def function():
    return "original"


class Replacement:
    pass


def replace_class(cls):
    return Replacement


@replace_class
class Public:
    pass
