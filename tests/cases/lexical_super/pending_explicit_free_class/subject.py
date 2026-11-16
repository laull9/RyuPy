# ryupy: pending
# 普通外层显式局部形成真实 free cell，但不属于 S1 定义类来源。
alias = super
def wrong_free_class():
    __class__ = 42
    def call(value):
        __class__
        return alias()
    return call
def shadow_free_class():
    __class__ = 42
    def call(value, super):
        __class__
        return super()
    return call
