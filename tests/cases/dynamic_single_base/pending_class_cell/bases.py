# 跨模块基类是实际运行时对象，不是编译器传播的 ClassId。
class Root:
    def chain(self):
        return ['Root']

class Left(Root):
    def chain(self):
        return ['Left'] + super().chain()

class Right(Root):
    def chain(self):
        return ['Right'] + super().chain()

class Diamond(Left, Right):
    pass

class Other:
    def chain(self):
        return ['Other']
