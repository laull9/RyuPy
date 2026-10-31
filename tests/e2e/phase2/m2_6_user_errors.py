# M2.6：用户异常继承、参数和匹配。
class InputError(ValueError):
    pass


try:
    raise InputError("bad", 7)
except ValueError as error:
    print(type(error).__name__, error.args)
    print(isinstance(error, Exception))
finally:
    print("custom cleanup")
