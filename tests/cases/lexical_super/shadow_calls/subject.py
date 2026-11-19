# 读取被遮蔽 super 仍触发词法 cell，但用户 callable 不能被当作内建。
canonical = super
def user():
    return 'global user'
super = user
class Base:
    def identify(self):
        return self
class Child(Base):
    def parameter(self, super):
        value = super()
        return value, canonical().identify()
    def local(self):
        super = lambda: 'local user'
        value = super()
        return value, canonical().identify()
    def global_value(self):
        global super
        value = super()
        return value, canonical().identify()
    @staticmethod
    def no_arguments(super=lambda: 'no arguments'):
        return super()
