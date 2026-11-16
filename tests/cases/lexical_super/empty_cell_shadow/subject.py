# 方法装饰阶段 cell 尚空；只有实际 canonical 调用读取它。
import helpers
class Base:
    def identify(self):
        return self
class Child(Base):
    @helpers.inspect
    def choose(self, super):
        return super()
    @helpers.inspect_no_arguments
    def no_arguments():
        return super()
