# M2.6：用户异常的多级继承、精确匹配与属性。

class BaseError(Exception):
    def describe(self):
        return "base:" + str(self.args)


class ChildError(BaseError):
    pass


try:
    raise ChildError("bad")
except ChildError as error:
    print(type(error).__name__, error.args, error.describe())
    print(isinstance(error, BaseError), isinstance(error, Exception))

try:
    raise ChildError("outer") from BaseError("inner")
except BaseError as error:
    print(type(error).__name__, type(error.__cause__).__name__)
finally:
    print("cleanup")
