# 域外类创建钩子保持独立待办，不能用“预期 NI”替代 CPython 合同。
events = []
class Base:
    def __init_subclass__(cls):
        events.append(('hook', cls.__name__))
