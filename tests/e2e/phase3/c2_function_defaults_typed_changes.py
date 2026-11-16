# 类型标注只提供优化线索；改写默认值后的真实Python对象必须进入函数体。
def typed(value: int = 1) -> int:
    return value


typed.__defaults__ = ("changed",)
print(typed())


def keyword(*, value: int = 2) -> int:
    return value


keyword.__kwdefaults__["value"] = [3]
print(keyword())


def originally_required(value: int) -> int:
    return value


alias = originally_required
setattr(alias, "__defaults__", ("added",))
print(originally_required())
